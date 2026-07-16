[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$whisperRoot = Join-Path $InstallRoot 'runtime\faster-whisper'
$python = Join-Path $whisperRoot '.venv\Scripts\python.exe'
$model = Join-Path $InstallRoot 'models\faster-whisper-large-v3'
$server = Join-Path $repoRoot 'tools\ai\faster_whisper_server.py'
if (-not (Test-Path -LiteralPath $python)) { throw 'Run Setup-LocalAi.ps1 first (missing faster-whisper virtual environment).' }
if (-not (Test-Path -LiteralPath (Join-Path $model 'model.bin'))) { throw 'Run Setup-LocalAi.ps1 first (missing large-v3 model).' }

$dllDirectories = Get-ChildItem -LiteralPath (Join-Path $whisperRoot 'cuda12') -Filter '*.dll' -Recurse |
    Select-Object -ExpandProperty DirectoryName -Unique
if (-not $dllDirectories) { throw 'CUDA 12/cuDNN 9 runtime DLLs are missing.' }
$env:PATH = (($dllDirectories -join ';') + ';' + $env:PATH)
$env:DREAMY_CUDA_DLL_DIRS = ($dllDirectories -join ';')
$env:DREAMY_WHISPER_MODEL = $model
$env:DREAMY_WHISPER_DEVICE = 'cuda'
$env:DREAMY_WHISPER_COMPUTE_TYPE = 'int8_float16'
$env:DREAMY_WHISPER_IDLE_UNLOAD_SECONDS = '60'
$env:DREAMY_WHISPER_MAX_UPLOAD_MB = '64'
$env:DREAMY_LLAMA_PROPS_URL = 'http://127.0.0.1:8080/props'
$env:HF_HOME = Join-Path $InstallRoot 'huggingface-cache'

& $python $server
exit $LASTEXITCODE
