[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI')
)

$ErrorActionPreference = 'Stop'
$stateRoot = Join-Path $InstallRoot 'state'
$logRoot = Join-Path $InstallRoot 'logs'
New-Item -ItemType Directory -Force -Path $stateRoot,$logRoot | Out-Null

function Start-LocalService([string]$Name, [string]$ScriptPath) {
    $pidPath = Join-Path $stateRoot "$Name.pid"
    if (Test-Path -LiteralPath $pidPath) {
        $existingPid = [int](Get-Content -LiteralPath $pidPath -Raw)
        if (Get-Process -Id $existingPid -ErrorAction SilentlyContinue) {
            Write-Host "$Name is already running (PID $existingPid)."
            return
        }
    }
    $stdout = Join-Path $logRoot "$Name.out.log"
    $stderr = Join-Path $logRoot "$Name.err.log"
    $process = Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -PassThru `
        -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File',$ScriptPath,'-InstallRoot',$InstallRoot) `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Set-Content -LiteralPath $pidPath -Value $process.Id -Encoding ASCII
    Write-Host "Started $Name (PID $($process.Id)); logs: $logRoot"
}

Start-LocalService 'llama-cpp' (Join-Path $PSScriptRoot 'Start-LlamaCpp.ps1')
Start-LocalService 'faster-whisper' (Join-Path $PSScriptRoot 'Start-FasterWhisper.ps1')

$deadline = (Get-Date).AddMinutes(5)
foreach ($uri in @('http://127.0.0.1:8080/health','http://127.0.0.1:8001/health')) {
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri $uri -TimeoutSec 3
            if ($response.StatusCode -eq 200) { break }
        } catch { Start-Sleep -Seconds 2 }
        if ((Get-Date) -ge $deadline) { throw "Service did not become healthy: $uri" }
    } while ($true)
}

Write-Host 'Local AI services are healthy on loopback ports 8001 and 8080.'
Write-Host 'llama.cpp sleeps after 5 idle seconds; faster-whisper waits for that state before allocating the GPU.'
