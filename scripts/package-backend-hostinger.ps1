param(
    [string]$OutputRoot = "artifacts\backend-hostinger",
    [switch]$SourceOnly
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$backendRoot = Join-Path $root "backend"
$stagingRoot = Join-Path $root $OutputRoot
$packageRoot = Join-Path $stagingRoot "backend"
$publicRoot = Join-Path $stagingRoot "public_html"
$hostingerPublic = Join-Path $root "deploy\hostinger\public"

if (-not $SourceOnly -and -not (Test-Path (Join-Path $backendRoot "vendor"))) {
    throw "backend\\vendor was not found. Run Composer install for backend before packaging for Hostinger."
}

New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null
if (Test-Path $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
New-Item -ItemType Directory -Force -Path $publicRoot | Out-Null

$excludeNames = @(
    "Dockerfile",
    "docker"
)

Get-ChildItem -Path $backendRoot -Force |
    Where-Object { $excludeNames -notcontains $_.Name } |
    ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $packageRoot -Recurse -Force
    }

Copy-Item -LiteralPath (Join-Path $backendRoot "config\local.php.example") -Destination (Join-Path $packageRoot "config\local.php") -Force
Copy-Item -LiteralPath (Join-Path $hostingerPublic ".htaccess") -Destination (Join-Path $publicRoot ".htaccess") -Force
Copy-Item -LiteralPath (Join-Path $hostingerPublic "index.php") -Destination (Join-Path $publicRoot "index.php") -Force

$zipPath = Join-Path $stagingRoot "backend-hostinger.zip"
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

Write-Host "Packaged Hostinger backend to $zipPath"
if ($SourceOnly) {
    Write-Host "Created a source-only package. Run Composer install on the host or rebuild with vendor included before production use."
}
