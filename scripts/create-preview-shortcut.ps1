param([switch]$Remove)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$desktop = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktop "HARDWARE STORE Preview.lnk"

if ($Remove) {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
        Write-Host "Removed: $shortcutPath"
    }
    else {
        Write-Host "Shortcut does not exist: $shortcutPath"
    }
    exit 0
}

$pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
if (-not $pwsh) {
    throw "PowerShell 7 (pwsh) was not found."
}
$launcher = Join-Path $PSScriptRoot "open-preview.ps1"
if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Launcher script was not found: $launcher"
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $pwsh.Source
$shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$launcher`""
$shortcut.WorkingDirectory = $root
$shortcut.Description = "Open HARDWARE STORE Local Preview"
$shortcut.IconLocation = "$env:SystemRoot\System32\imageres.dll,68"
$shortcut.Save()

Write-Host "Created: $shortcutPath"
Write-Host "Remove later with: .\scripts\create-preview-shortcut.ps1 -Remove"
