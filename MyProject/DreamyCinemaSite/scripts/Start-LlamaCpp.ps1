[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI'),
    [int]$GpuLayers = 99,
    [int]$ContextSize = 8192
)

$ErrorActionPreference = 'Stop'
$llamaRoot = Join-Path $InstallRoot 'runtime\llama.cpp'
$server = Get-ChildItem -LiteralPath $llamaRoot -Filter 'llama-server.exe' -Recurse -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
$model = Join-Path $InstallRoot 'models\Qwen3-8B-Q4_K_M.gguf'
if ($null -eq $server) { throw 'Run Setup-LocalAi.ps1 first (missing official llama-server.exe).' }
if (-not (Test-Path -LiteralPath $model)) { throw 'Run Setup-LocalAi.ps1 first (missing Qwen3-8B Q4_K_M).' }

& $server.FullName `
    --host 127.0.0.1 `
    --port 8080 `
    --model $model `
    --alias Qwen3-8B-Q4_K_M `
    --ctx-size $ContextSize `
    --n-gpu-layers $GpuLayers `
    --parallel 1 `
    --jinja `
    --reasoning off `
    --reasoning-budget 0 `
    --flash-attn on `
    --no-context-shift `
    --sleep-idle-seconds 5
exit $LASTEXITCODE
