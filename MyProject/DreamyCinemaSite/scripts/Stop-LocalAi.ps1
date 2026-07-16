[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI')
)

$ErrorActionPreference = 'Stop'
$stateRoot = Join-Path $InstallRoot 'state'

function Get-DescendantProcesses([int]$ParentId) {
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ParentId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        Get-DescendantProcesses -ParentId $child.ProcessId
        $child
    }
}

foreach ($name in @('faster-whisper','llama-cpp')) {
    $pidPath = Join-Path $stateRoot "$name.pid"
    if (-not (Test-Path -LiteralPath $pidPath)) { continue }
    $servicePid = [int](Get-Content -LiteralPath $pidPath -Raw)
    $process = Get-CimInstance Win32_Process -Filter "ProcessId = $servicePid" -ErrorAction SilentlyContinue
    if ($null -ne $process -and $process.CommandLine -like "*$InstallRoot*") {
        $descendants = @(Get-DescendantProcesses -ParentId $servicePid)
        foreach ($child in $descendants) {
            if ($child.CommandLine -like "*$InstallRoot*" -or $child.ExecutablePath -like "$InstallRoot*") {
                Stop-Process -Id $child.ProcessId -Force -ErrorAction SilentlyContinue
            }
        }
        Stop-Process -Id $servicePid -Force
        Write-Host "Stopped $name (PID $servicePid)."
    }
    Remove-Item -LiteralPath $pidPath -Force
}
