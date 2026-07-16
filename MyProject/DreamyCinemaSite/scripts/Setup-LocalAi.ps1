[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI'),
    [switch]$SkipWhisperModel,
    [switch]$SkipQwenModel
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$InstallRoot = [IO.Path]::GetFullPath($InstallRoot)
if ($InstallRoot.TrimEnd('\') -eq [IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\')) {
    throw 'InstallRoot cannot be a drive root.'
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$downloads = Join-Path $InstallRoot 'downloads'
$runtimeRoot = Join-Path $InstallRoot 'runtime'
$whisperRoot = Join-Path $runtimeRoot 'faster-whisper'
$cudaRoot = Join-Path $whisperRoot 'cuda12'
$venvRoot = Join-Path $whisperRoot '.venv'
$llamaRoot = Join-Path $runtimeRoot 'llama.cpp'
$modelsRoot = Join-Path $InstallRoot 'models'
$whisperModel = Join-Path $modelsRoot 'faster-whisper-large-v3'
$qwenModel = Join-Path $modelsRoot 'Qwen3-8B-Q4_K_M.gguf'

New-Item -ItemType Directory -Force -Path $downloads,$runtimeRoot,$whisperRoot,$cudaRoot,$llamaRoot,$modelsRoot | Out-Null

function Invoke-Download([string]$Uri, [string]$Destination, [long]$ExpectedBytes) {
    $resolvedDestination = [IO.Path]::GetFullPath($Destination)
    $resolvedRoot = $InstallRoot.TrimEnd('\') + '\'
    if (-not $resolvedDestination.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Download destination escapes InstallRoot: $resolvedDestination"
    }
    if ((Test-Path -LiteralPath $Destination) -and (Get-Item -LiteralPath $Destination).Length -eq $ExpectedBytes) {
        Write-Host "Using existing download: $Destination"
        return
    }
    Write-Host "Downloading/resuming $Uri"
    & curl.exe --location --fail --retry 10 --retry-all-errors --retry-delay 2 `
        --speed-limit 102400 --speed-time 30 --continue-at - --output $Destination $Uri
    if ($LASTEXITCODE -ne 0) { throw "Download failed: $Uri" }
    if ((Get-Item -LiteralPath $Destination).Length -ne $ExpectedBytes) {
        throw "Download size mismatch: $Destination"
    }
}

function Expand-ZipFresh([string]$Archive, [string]$Destination) {
    $resolvedDestination = [IO.Path]::GetFullPath($Destination)
    $resolvedRoot = $InstallRoot.TrimEnd('\') + '\'
    if (-not $resolvedDestination.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Archive destination escapes InstallRoot: $resolvedDestination"
    }
    if (Test-Path -LiteralPath $Destination) {
        Get-ChildItem -LiteralPath $Destination -Force | Remove-Item -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Expand-Archive -LiteralPath $Archive -DestinationPath $Destination -Force
}

$python = (Get-Command python -ErrorAction Stop).Source
if (-not (Test-Path -LiteralPath (Join-Path $venvRoot 'Scripts\python.exe'))) {
    & $python -m venv $venvRoot
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the faster-whisper virtual environment.' }
}
$venvPython = Join-Path $venvRoot 'Scripts\python.exe'
& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install --requirement (Join-Path $repoRoot 'tools\ai\requirements-faster-whisper.txt')
if ($LASTEXITCODE -ne 0) { throw 'Installing faster-whisper Python packages failed.' }

# faster-whisper's own Windows instructions link this maintained CUDA 12/cuDNN 9 archive.
$cudaArchive = Join-Path $downloads 'cuBLAS.and.cuDNN_CUDA12_win_v3.7z'
Invoke-Download 'https://github.com/Purfview/whisper-standalone-win/releases/download/libs/cuBLAS.and.cuDNN_CUDA12_win_v3.7z' $cudaArchive 849141159
if (-not (Get-ChildItem -LiteralPath $cudaRoot -Filter 'cudnn*.dll' -Recurse -ErrorAction SilentlyContinue)) {
    $sevenZip = Get-Command 7z.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
    if (-not $sevenZip -and (Test-Path -LiteralPath 'C:\Program Files\7-Zip\7z.exe')) {
        $sevenZip = 'C:\Program Files\7-Zip\7z.exe'
    }
    if (-not $sevenZip) {
        throw '7-Zip is required to extract the maintained CUDA runtime archive (winget install 7zip.7zip).'
    }
    & $sevenZip x -y "-o$cudaRoot" $cudaArchive
    if ($LASTEXITCODE -ne 0) { throw 'Extracting the CUDA 12/cuDNN 9 runtime failed.' }
}

$headers = @{ 'User-Agent' = 'DreamyCinema-LocalAi-Setup' }
$release = $null
$llamaAsset = $null
$cudartAsset = $null
$recentReleases = Invoke-RestMethod -Headers $headers -Uri 'https://api.github.com/repos/ggml-org/llama.cpp/releases?per_page=10'
foreach ($candidate in $recentReleases) {
    $candidateLlama = $candidate.assets | Where-Object name -Like 'llama-*-bin-win-cuda-12.4-x64.zip' | Select-Object -First 1
    $candidateCudart = $candidate.assets | Where-Object name -EQ 'cudart-llama-bin-win-cuda-12.4-x64.zip' | Select-Object -First 1
    if ($null -ne $candidateLlama -and $null -ne $candidateCudart) {
        $release = $candidate
        $llamaAsset = $candidateLlama
        $cudartAsset = $candidateCudart
        break
    }
}
if ($null -eq $release) {
    throw 'No recent complete official llama.cpp release contains both Windows CUDA 12.4 assets.'
}
$llamaArchive = Join-Path $downloads $llamaAsset.name
$cudartArchive = Join-Path $downloads $cudartAsset.name
Invoke-Download $llamaAsset.browser_download_url $llamaArchive $llamaAsset.size
Invoke-Download $cudartAsset.browser_download_url $cudartArchive $cudartAsset.size
$llamaVersionRoot = Join-Path $llamaRoot $release.tag_name
if (-not (Get-ChildItem -LiteralPath $llamaVersionRoot -Filter 'llama-server.exe' -Recurse -ErrorAction SilentlyContinue)) {
    Expand-ZipFresh $llamaArchive $llamaVersionRoot
    $cudartTemp = Join-Path $downloads ("cudart-" + $release.tag_name)
    Expand-ZipFresh $cudartArchive $cudartTemp
    $server = Get-ChildItem -LiteralPath $llamaVersionRoot -Filter 'llama-server.exe' -Recurse | Select-Object -First 1
    if ($null -eq $server) { throw 'llama-server.exe was not found after extraction.' }
    Get-ChildItem -LiteralPath $cudartTemp -Filter '*.dll' -Recurse | Copy-Item -Destination $server.DirectoryName -Force
}

if (-not $SkipWhisperModel -and -not (Test-Path -LiteralPath (Join-Path $whisperModel 'model.bin'))) {
    Write-Host 'Downloading the official Systran faster-whisper-large-v3 model...'
    $env:HF_HOME = Join-Path $InstallRoot 'huggingface-cache'
    & $venvPython -c "from huggingface_hub import snapshot_download; import sys; snapshot_download('Systran/faster-whisper-large-v3', local_dir=sys.argv[1])" $whisperModel
    if ($LASTEXITCODE -ne 0) { throw 'Downloading faster-whisper-large-v3 failed.' }
}

if (-not $SkipQwenModel -and -not (Test-Path -LiteralPath $qwenModel)) {
    $modelUri = 'https://huggingface.co/Qwen/Qwen3-8B-GGUF/resolve/main/Qwen3-8B-Q4_K_M.gguf?download=true'
    Invoke-Download $modelUri $qwenModel 5027783488
}

if (Test-Path -LiteralPath $qwenModel) {
    $metadata = Invoke-RestMethod -Headers $headers -Uri 'https://huggingface.co/api/models/Qwen/Qwen3-8B-GGUF?blobs=true'
    $modelMetadata = $metadata.siblings | Where-Object rfilename -EQ 'Qwen3-8B-Q4_K_M.gguf' | Select-Object -First 1
    if ($null -ne $modelMetadata.lfs.sha256) {
        $actual = (Get-FileHash -LiteralPath $qwenModel -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $modelMetadata.lfs.sha256.ToLowerInvariant()) {
            throw 'Qwen3-8B Q4_K_M SHA256 verification failed.'
        }
    }
}

$manifest = [ordered]@{
    installedAt = (Get-Date).ToString('o')
    fasterWhisper = '1.2.1'
    ctranslate2 = '4.8.1'
    cudaRuntimeArchive = (Get-FileHash -LiteralPath $cudaArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    llamaCpp = $release.tag_name
    llamaArchive = $llamaAsset.name
    qwenModel = if (Test-Path -LiteralPath $qwenModel) { (Get-FileHash -LiteralPath $qwenModel -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $InstallRoot 'versions.json') -Encoding UTF8

Write-Host "Local AI runtime is ready under $InstallRoot"
Write-Host "Copy appsettings.Local.example.json to appsettings.Local.json, then run scripts\Start-LocalAi.ps1."
