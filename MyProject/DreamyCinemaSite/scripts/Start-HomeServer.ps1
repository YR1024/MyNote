[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Start-LocalAi.ps1') -InstallRoot $InstallRoot
if (Get-NetTCPConnection -State Listen -LocalPort 5210 -ErrorAction SilentlyContinue) {
    throw 'Port 5210 is already in use; DreamyCinema may already be running.'
}
Push-Location $repoRoot
try {
    & dotnet run --urls 'http://0.0.0.0:5210'
} finally {
    Pop-Location
}
