param(
    [ValidateSet("Start", "Stop", "Status")]
    [string]$Action = "Start"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $root ".env.preview.local"
$composeFile = Join-Path $root "compose.preview.yaml"
$project = "hardware-store-preview"

function Resolve-DockerCli {
    $command = Get-Command docker -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\DockerDesktop\resources\bin\docker.exe"),
        (Join-Path $env:ProgramFiles "Docker\Docker\resources\bin\docker.exe"),
        (Join-Path ${env:ProgramW6432} "Docker\Docker\resources\bin\docker.exe")
    ) | Select-Object -Unique

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }
    throw "Docker CLI is not available. Start Docker Desktop or add its CLI to PATH."
}

if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Preview compose file was not found: $composeFile"
}

$dockerCli = Resolve-DockerCli

function Read-PreviewEnvironment {
    if (-not (Test-Path -LiteralPath $envFile)) {
        throw "Missing .env.preview.local. Copy .env.preview.example and replace all placeholders locally."
    }
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $envFile) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) { continue }
        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) { continue }
        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$key] = $value
    }
    return $values
}

function Invoke-PreviewCompose([string[]]$Arguments) {
    & $dockerCli compose --project-name $project --env-file $envFile -f $composeFile @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForUrl([string]$Url, [int]$TimeoutSeconds = 120) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) { return }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for $Url"
}

function Initialize-Preview {
    $values = Read-PreviewEnvironment
    $required = @(
        "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD", "APP_DB_PASSWORD",
        "PREVIEW_OWNER_EMAIL", "PREVIEW_OWNER_USERNAME", "PREVIEW_OWNER_FULL_NAME",
        "PREVIEW_OWNER_PASSWORD", "PREVIEW_BOOTSTRAP_KEY"
    )
    foreach ($key in $required) {
        if (-not $values[$key] -or $values[$key].StartsWith("replace-")) {
            throw "Set a local value for $key in .env.preview.local."
        }
    }

    Invoke-PreviewCompose @("up", "-d", "--build", "postgres")
    $apiPort = if ($values["PREVIEW_API_PORT"]) { $values["PREVIEW_API_PORT"] } else { "8085" }
    $webPort = if ($values["PREVIEW_WEB_PORT"]) { $values["PREVIEW_WEB_PORT"] } else { "3005" }
    Invoke-PreviewCompose @("up", "-d", "--build", "api", "web")
    Wait-ForUrl "http://127.0.0.1:$apiPort/health/ready"
    Wait-ForUrl "http://127.0.0.1:$webPort/api/health"

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $csrf = Invoke-RestMethod -Method Get -Uri "http://127.0.0.1:$apiPort/api/v1/identity/antiforgery" -WebSession $session
    $initialized = Invoke-RestMethod `
        -Method Post `
        -Uri "http://127.0.0.1:$apiPort/api/v1/development/preview/initialize" `
        -WebSession $session `
        -Headers @{
            "X-CSRF-TOKEN" = $csrf.requestToken
            "X-Preview-Bootstrap-Key" = $values["PREVIEW_BOOTSTRAP_KEY"]
        } `
        -ContentType "application/json" `
        -Body "{}"

    Write-Host ""
    Write-Host "HARDWARE STORE preview is ready."
    Write-Host "URL:      http://127.0.0.1:$webPort"
    Write-Host "Products: http://127.0.0.1:$webPort/products"
    Write-Host "Admin:    http://127.0.0.1:$webPort/admin"
    Write-Host "Owner:    $($initialized.ownerUsername)"
    Write-Host "Password: the value stored only in .env.preview.local under PREVIEW_OWNER_PASSWORD"
    Write-Host "Catalog:  $($initialized.categories) categories, $($initialized.brands) brands, $($initialized.products) products"
    Write-Host "Stop:     .\scripts\preview.ps1 -Action Stop"
}

switch ($Action) {
    "Start" {
        Initialize-Preview
    }
    "Stop" {
        Invoke-PreviewCompose @("down")
        Write-Host "Preview containers stopped. Preview volumes were preserved."
    }
    "Status" {
        Invoke-PreviewCompose @("ps")
    }
}
