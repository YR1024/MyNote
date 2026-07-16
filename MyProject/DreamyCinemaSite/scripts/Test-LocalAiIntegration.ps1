[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DreamyCinema\AI'),
    [int]$Port = 5251,
    [int]$VideoSeconds = 0,
    [int]$SpeechChunkSeconds = 30,
    [switch]$KeepTestData
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $env:TEMP 'DreamyCinema-phase31-real-integration'
$resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
$resolvedTempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
if (-not $resolvedTestRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The isolated test root must stay under the Windows temporary directory.'
}
if (Test-Path -LiteralPath $resolvedTestRoot) {
    Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
}

$videoRoot = Join-Path $resolvedTestRoot 'Videos'
$dataRoot = Join-Path $resolvedTestRoot 'Data'
$buildRoot = Join-Path $resolvedTestRoot 'build'
New-Item -ItemType Directory -Force -Path $videoRoot,$dataRoot,$buildRoot | Out-Null
$server = $null
$success = $false
$baseUrl = "http://127.0.0.1:$Port"
$password = 'Dreamy-Local-Ai-Test-2026'

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Format-SrtTime([int]$Milliseconds) {
    $time = [TimeSpan]::FromMilliseconds($Milliseconds)
    return '{0:00}:{1:00}:{2:00},{3:000}' -f [int]$time.TotalHours,$time.Minutes,$time.Seconds,$time.Milliseconds
}

function Start-TestServer {
    $stdout = Join-Path $resolvedTestRoot 'server.out.log'
    $stderr = Join-Path $resolvedTestRoot 'server.err.log'
    return Start-Process -FilePath 'dotnet' -WindowStyle Hidden -PassThru -WorkingDirectory $repoRoot `
        -ArgumentList @((Join-Path $buildRoot 'DreamyCinemaSite.dll'),'--urls',$baseUrl) `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
}

function Stop-TestServer {
    if ($null -ne $script:server -and (Get-Process -Id $script:server.Id -ErrorAction SilentlyContinue)) {
        Stop-Process -Id $script:server.Id -Force
        Wait-Process -Id $script:server.Id -Timeout 10 -ErrorAction SilentlyContinue
    }
    $script:server = $null
}

function Wait-Server {
    $deadline = (Get-Date).AddSeconds(45)
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri "$baseUrl/api/auth/session" -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        } catch { Start-Sleep -Milliseconds 500 }
    } while ((Get-Date) -lt $deadline)
    throw 'The isolated DreamyCinema server did not start.'
}

function Open-AuthenticatedSession([bool]$Setup) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $state = Invoke-RestMethod -Uri "$baseUrl/api/auth/session" -WebSession $session
    $headers = @{ 'X-CSRF-TOKEN' = $state.requestToken }
    $body = @{ password = $password } | ConvertTo-Json
    if ($Setup) {
        Invoke-RestMethod -Method Post -Uri "$baseUrl/api/auth/setup" -WebSession $session -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    } else {
        Invoke-RestMethod -Method Post -Uri "$baseUrl/api/auth/login" -WebSession $session -Headers $headers -ContentType 'application/json' -Body $body | Out-Null
    }
    $state = Invoke-RestMethod -Uri "$baseUrl/api/auth/session" -WebSession $session
    return [pscustomobject]@{ Session = $session; Csrf = $state.requestToken }
}

function Invoke-WriteApi([string]$Method, [string]$Path, $Auth, $Body = $null) {
    $parameters = @{
        Method = $Method
        Uri = "$baseUrl$Path"
        WebSession = $Auth.Session
        Headers = @{ 'X-CSRF-TOKEN' = $Auth.Csrf }
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = ($Body | ConvertTo-Json -Depth 8)
    }
    return Invoke-RestMethod @parameters
}

function Get-Api([string]$Path, $Auth) {
    return Invoke-RestMethod -Uri "$baseUrl$Path" -WebSession $Auth.Session
}

function Wait-Job([string]$JobId, $Auth, [string[]]$TerminalStatuses, [int]$TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $job = Get-Api "/api/jobs/$JobId" $Auth
        if ($TerminalStatuses -contains $job.status) { return $job }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for job $JobId."
}

try {
    $ffmpeg = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Filter ffmpeg.exe -Recurse -ErrorAction Stop | Select-Object -First 1 -ExpandProperty FullName
    $ffprobe = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Filter ffprobe.exe -Recurse -ErrorAction Stop | Select-Object -First 1 -ExpandProperty FullName

    $texts = @(
        'What the fuck are you doing?',
        'He shot the guard and blood was everywhere.',
        'This sex scene is not censored.',
        'Alice, get the hell out of here.',
        'DreamyCinema keeps the original tone.',
        'Do not soften the threat.',
        'The killer hid the bloody knife.',
        'She called him a damn liar.',
        'They discussed violence without euphemisms.',
        'The dialogue must stay faithful.',
        'Names and terminology remain consistent.',
        'This is the final subtitle cue.'
    )
    $srt = New-Object Text.StringBuilder
    for ($index = 0; $index -lt $texts.Count; $index++) {
        $start = 500 + ($index * 2500)
        $end = $start + 2000
        [void]$srt.AppendLine(($index + 1).ToString())
        [void]$srt.AppendLine("$(Format-SrtTime $start) --> $(Format-SrtTime $end)")
        [void]$srt.AppendLine($texts[$index])
        [void]$srt.AppendLine()
    }
    $subtitlePath = Join-Path $videoRoot 'AI-REAL-001.en.srt'
    [IO.File]::WriteAllText($subtitlePath, $srt.ToString(), [Text.UTF8Encoding]::new($false))

    $speechPath = Join-Path $resolvedTestRoot 'speech.wav'
    Add-Type -AssemblyName System.Speech
    $synth = [System.Speech.Synthesis.SpeechSynthesizer]::new()
    try {
        $synth.Rate = -1
        $synth.SetOutputToWaveFile($speechPath)
        $synth.Speak(($texts -join ' '))
    } finally { $synth.Dispose() }
    $videoPath = Join-Path $videoRoot 'AI-REAL-001.mp4'
    if ($VideoSeconds -gt 0) {
        & $ffmpeg -hide_banner -loglevel error -y -f lavfi -i 'color=c=black:s=640x360' -stream_loop -1 -i $speechPath -t $VideoSeconds -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac $videoPath
    } else {
        & $ffmpeg -hide_banner -loglevel error -y -f lavfi -i 'color=c=black:s=640x360' -i $speechPath -shortest -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac $videoPath
    }
    if ($LASTEXITCODE -ne 0) { throw 'Creating the isolated test video failed.' }

    dotnet build (Join-Path $repoRoot 'DreamyCinemaSite.csproj') -p:SkipFrontendBuild=true -o $buildRoot
    if ($LASTEXITCODE -ne 0) { throw 'Building the isolated test server failed.' }

    $env:VideoStorage__RootPath = $videoRoot
    $env:Database__Path = Join-Path $dataRoot 'test.db'
    $env:Security__CredentialPath = Join-Path $dataRoot 'credentials.json'
    $env:MediaTools__FfmpegPath = $ffmpeg
    $env:MediaTools__FfprobePath = $ffprobe
    $env:AiSubtitles__Enabled = 'true'
    $env:AiSubtitles__TranslationChunkSize = '5'
    $env:AiSubtitles__SpeechChunkSeconds = $SpeechChunkSeconds.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:AiSubtitles__Speech__Provider = 'FasterWhisper'
    $env:AiSubtitles__Speech__BaseUrl = 'http://127.0.0.1:8001/v1'
    $env:AiSubtitles__Speech__Model = 'large-v3'
    $env:AiSubtitles__Translation__Provider = 'LlamaCpp'
    $env:AiSubtitles__Translation__BaseUrl = 'http://127.0.0.1:8080/v1'
    $env:AiSubtitles__Translation__Model = 'Qwen3-8B-Q4_K_M'

    $server = Start-TestServer
    Wait-Server
    $auth = Open-AuthenticatedSession $true
    $sync = Invoke-WriteApi 'Post' '/api/videos/sync' $auth
    $syncJob = Wait-Job $sync.id $auth @('Completed','Failed') 120
    Assert-Condition ($syncJob.status -eq 'Completed') "Sync failed: $($syncJob.error)"
    $page = Get-Api '/api/videos?page=1&pageSize=20' $auth
    Assert-Condition ($page.total -eq 1) 'The isolated sync did not import exactly one video.'
    $video = $page.items[0]
    $sourceTrack = $video.subtitles | Where-Object kind -EQ 'Original' | Select-Object -First 1
    Assert-Condition ($sourceTrack.cueCount -eq 12) 'The source subtitle track does not contain 12 cues.'
    Assert-Condition ($sourceTrack.revisionStage -eq 'SourceOriginal') 'Imported source subtitles have the wrong revision boundary.'

    $translationWatch = [Diagnostics.Stopwatch]::StartNew()
    $translation = Invoke-WriteApi 'Post' "/api/videos/$($video.id)/subtitles/$($sourceTrack.id)/translate" $auth
    $translationInterrupted = $false
    $deadline = (Get-Date).AddMinutes(3)
    do {
        $translationState = Get-Api "/api/jobs/$($translation.id)" $auth
        if ($translationState.status -eq 'Failed') { throw "Translation failed before restart: $($translationState.error)" }
        if ($translationState.status -eq 'Completed') { break }
        if ($translationState.currentItem -match '1\s*/\s*\d+') {
            Stop-TestServer
            $translationInterrupted = $true
            break
        }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    Assert-Condition $translationInterrupted 'Translation completed before the restart checkpoint could be exercised.'

    $server = Start-TestServer
    Wait-Server
    $auth = Open-AuthenticatedSession $false
    $translationJob = Wait-Job $translation.id $auth @('Completed','Failed') 300
    $translationWatch.Stop()
    Assert-Condition ($translationJob.status -eq 'Completed') "Translation resume failed: $($translationJob.error)"
    Assert-Condition ($translationJob.attemptCount -ge 2) 'Restarted translation did not increment AttemptCount.'

    $tracks = Get-Api "/api/videos/$($video.id)/subtitles" $auth
    $translatedTrack = $tracks | Where-Object source -EQ 'AiTranslation' | Select-Object -First 1
    Assert-Condition ($translatedTrack.cueCount -eq 12) 'Translated Cue count differs from the source.'
    Assert-Condition ($translatedTrack.revisionStage -eq 'ChineseDraft') 'AI translation was not stored as a separate Chinese draft.'
    $sourceVtt = Invoke-RestMethod -Uri "$baseUrl$($sourceTrack.vttUrl)" -WebSession $auth.Session
    $translatedVtt = Invoke-RestMethod -Uri "$baseUrl$($translatedTrack.vttUrl)" -WebSession $auth.Session
    $sourceTimings = [regex]::Matches($sourceVtt, '(?m)^\d{2}:\d{2}:\d{2}\.\d{3} --> \d{2}:\d{2}:\d{2}\.\d{3}$') | ForEach-Object Value
    $translatedTimings = [regex]::Matches($translatedVtt, '(?m)^\d{2}:\d{2}:\d{2}\.\d{3} --> \d{2}:\d{2}:\d{2}\.\d{3}$') | ForEach-Object Value
    Assert-Condition (($sourceTimings -join '|') -eq ($translatedTimings -join '|')) 'Translation changed the subtitle timeline.'
    $explicitTerms = @(
        (-join @([char]20182, [char]22920)),
        (-join @([char]22920, [char]30340)),
        ([char]25805).ToString(),
        ([char]28378).ToString()
    )
    Assert-Condition (($explicitTerms | Where-Object { $translatedVtt.Contains($_) }).Count -gt 0) 'The explicit source phrase appears to have been weakened or omitted.'

    $recognition = Invoke-WriteApi 'Post' "/api/videos/$($video.id)/subtitles/transcribe" $auth @{ language = 'en' }
    $runningRecognition = Wait-Job $recognition.id $auth @('Running','Completed','Failed') 60
    Assert-Condition ($runningRecognition.status -eq 'Running') 'Recognition completed before cancellation could be exercised.'
    Invoke-WriteApi 'Post' "/api/jobs/$($recognition.id)/cancel" $auth | Out-Null
    $cancelled = Wait-Job $recognition.id $auth @('Cancelled','Completed','Failed') 60
    Assert-Condition ($cancelled.status -eq 'Cancelled') 'Running recognition did not cancel.'
    $recognitionWatch = [Diagnostics.Stopwatch]::StartNew()
    Invoke-WriteApi 'Post' "/api/jobs/$($recognition.id)/retry" $auth | Out-Null
    $recognized = Wait-Job $recognition.id $auth @('Completed','Failed') ([Math]::Max(300, $VideoSeconds * 2))
    $recognitionWatch.Stop()
    Assert-Condition ($recognized.status -eq 'Completed') "Recognition retry failed: $($recognized.error)"
    Assert-Condition ($recognized.attemptCount -ge 2) 'Recognition retry did not increment AttemptCount.'
    $tracks = Get-Api "/api/videos/$($video.id)/subtitles" $auth
    $speechTrack = $tracks | Where-Object source -EQ 'SpeechRecognition' | Select-Object -First 1
    Assert-Condition ($speechTrack.cueCount -gt 0) 'Speech recognition did not create cues.'
    Assert-Condition ($speechTrack.revisionStage -eq 'RawRecognition') 'Speech output was not stored as a separate raw recognition track.'
    $speechVtt = Invoke-RestMethod -Uri "$baseUrl$($speechTrack.vttUrl)" -WebSession $auth.Session
    Assert-Condition ($speechVtt -match 'What the fuck') 'The raw recognition track omitted the explicit phrase.'

    $success = $true
    [pscustomobject]@{
        translationJob = $translationJob.id
        translationAttempts = $translationJob.attemptCount
        sourceCues = $sourceTrack.cueCount
        translatedCues = $translatedTrack.cueCount
        timelineIdentical = $true
        recognitionJob = $recognized.id
        recognitionAttempts = $recognized.attemptCount
        recognizedCues = $speechTrack.cueCount
        explicitLanguagePreserved = $true
        videoSeconds = if ($VideoSeconds -gt 0) { $VideoSeconds } else { [Math]::Round($video.durationSeconds, 1) }
        speechChunkSeconds = $SpeechChunkSeconds
        translationElapsedSeconds = [Math]::Round($translationWatch.Elapsed.TotalSeconds, 1)
        recognitionRetryElapsedSeconds = [Math]::Round($recognitionWatch.Elapsed.TotalSeconds, 1)
    } | ConvertTo-Json
}
finally {
    Stop-TestServer
    if (-not $success) {
        Get-Content -LiteralPath (Join-Path $resolvedTestRoot 'server.err.log') -Tail 160 -ErrorAction SilentlyContinue
    }
    if (($success -and -not $KeepTestData) -and (Test-Path -LiteralPath $resolvedTestRoot)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
