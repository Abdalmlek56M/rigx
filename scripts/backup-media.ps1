param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,
    [switch]$Verify
)

$ErrorActionPreference = "Stop"
$sourcePath = (Resolve-Path $Source).Path
$backupRootPath = [System.IO.Path]::GetFullPath($BackupRoot)
if (-not (Test-Path $backupRootPath)) { New-Item -ItemType Directory -Path $backupRootPath -Force | Out-Null }

if ($backupRootPath.StartsWith($sourcePath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "BackupRoot must be outside Source; refusing to copy the media tree into itself."
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$target = Join-Path $backupRootPath "media-$stamp"
New-Item -ItemType Directory -Path $target -Force | Out-Null

function Write-Manifest([string]$root, [string]$manifestPath) {
    $lines = Get-ChildItem -Path $root -Recurse -File |
        Where-Object { $_.Name -notlike "*.sha256" } |
        Sort-Object FullName |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
            $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$hash  $relative"
        }
    Set-Content -Path $manifestPath -Value $lines -Encoding UTF8
    return $lines
}

$sourceManifest = Write-Manifest -root $sourcePath -manifestPath (Join-Path $target "source-manifest.sha256")

Copy-Item -Path (Join-Path $sourcePath "*") -Destination $target -Recurse -Force
$copiedManifest = Write-Manifest -root $target -manifestPath (Join-Path $target "backup-manifest.sha256")
$manifestDiff = Compare-Object $sourceManifest $copiedManifest
if ($manifestDiff) {
    throw "Backup verification failed: copied files differ from the source manifest."
}

Write-Output "BACKUP_OK $target"
Write-Output ("FILES " + $copiedManifest.Count)

if ($Verify) {
    $restorePath = Join-Path $backupRootPath "restore-$stamp"
    New-Item -ItemType Directory -Path $restorePath -Force | Out-Null
    Copy-Item -Path (Join-Path $target "*") -Destination $restorePath -Recurse -Force
    $restoredManifest = Write-Manifest -root $restorePath -manifestPath (Join-Path $restorePath "restore-manifest.sha256")
    $restoreDiff = Compare-Object $copiedManifest $restoredManifest
    if ($restoreDiff) {
        throw "Restore verification failed: restored files differ from the backup manifest."
    }
    Write-Output "RESTORE_VERIFIED $restorePath"
    Write-Output ("RESTORED_FILES " + $restoredManifest.Count)
}
