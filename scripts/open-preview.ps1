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

function Read-PreviewEnvironment {
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $envFile) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) { continue }
        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) { continue }
        $values[$trimmed.Substring(0, $separator).Trim()] = $trimmed.Substring($separator + 1).Trim()
    }
    return $values
}

function Test-PreviewHealth([int]$WebPort, [int]$ApiPort) {
    try {
        $web = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$WebPort/api/health" -TimeoutSec 4
        $api = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:$ApiPort/health/ready" -TimeoutSec 4
        return $web.StatusCode -eq 200 -and $api.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Show-SafeLogs([string]$DockerCli, [string[]]$SensitiveValues) {
    $logs = & $DockerCli compose --project-name $project --env-file $envFile -f $composeFile logs --no-color --tail 80 2>&1 | Out-String
    foreach ($value in $SensitiveValues | Where-Object { $_ -and $_.Length -ge 4 } | Sort-Object -Descending) {
        $logs = $logs.Replace($value, "***")
    }
    $logs = [regex]::Replace($logs, '(?im)(password|secret|token|bootstrap_key)\s*[=:]\s*[^\s;]+', '$1=***')
    Write-Host "Recent preview logs (sanitized):"
    Write-Host $logs
}

try {
    if (-not (Test-Path -LiteralPath $envFile)) {
        throw "Missing .env.preview.local. Copy .env.preview.example and set local values."
    }
    $dockerCli = Resolve-DockerCli

    $values = Read-PreviewEnvironment
    $webPort = if ($values["PREVIEW_WEB_PORT"]) { [int]$values["PREVIEW_WEB_PORT"] } else { 3005 }
    $apiPort = if ($values["PREVIEW_API_PORT"]) { [int]$values["PREVIEW_API_PORT"] } else { 8085 }
    $url = "http://127.0.0.1:$webPort"

    if (-not (Test-PreviewHealth $webPort $apiPort)) {
        $answer = Read-Host "Local Preview is not ready. Start it now? [y/N]"
        if ($answer -notmatch '^(?i)y(es)?$') {
            Write-Host "Preview was not started."
            exit 0
        }
        & (Join-Path $PSScriptRoot "preview.ps1") -Action Start
        if (-not (Test-PreviewHealth $webPort $apiPort)) {
            throw "Preview did not become healthy after startup."
        }
    }

    Start-Process $url
}
catch {
    Write-Host "Preview launcher failed: $($_.Exception.Message)" -ForegroundColor Red
    if (Test-Path -LiteralPath $envFile) {
        try {
            $values = Read-PreviewEnvironment
            Show-SafeLogs $dockerCli @(
                $values["POSTGRES_PASSWORD"],
                $values["APP_DB_PASSWORD"],
                $values["PREVIEW_OWNER_PASSWORD"],
                $values["PREVIEW_BOOTSTRAP_KEY"]
            )
        }
        catch {
            Write-Host "Logs could not be collected safely."
        }
    }
    Read-Host "Press Enter to close"
    exit 1
}
