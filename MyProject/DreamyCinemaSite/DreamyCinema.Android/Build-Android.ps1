[CmdletBinding()]
param(
    [switch]$Clean,
    [switch]$SkipLint
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot

$javaHome = $env:JAVA_HOME
if (-not $javaHome) {
    $javaHome = [Environment]::GetEnvironmentVariable("JAVA_HOME", "Machine")
}
if (-not $javaHome -or -not (Test-Path -LiteralPath (Join-Path $javaHome "bin\java.exe"))) {
    $javaHome = Get-ChildItem -LiteralPath "C:\Program Files\Microsoft" -Directory -ErrorAction SilentlyContinue |
        Where-Object Name -Like "jdk-17*" |
        Sort-Object Name -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $javaHome -or -not (Test-Path -LiteralPath (Join-Path $javaHome "bin\java.exe"))) {
    throw "未找到 JDK 17。请执行：winget install --id Microsoft.OpenJDK.17 --exact"
}

$sdkRoot = $env:ANDROID_HOME
if (-not $sdkRoot) {
    $sdkRoot = $env:ANDROID_SDK_ROOT
}
if (-not $sdkRoot) {
    $sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}
if (-not (Test-Path -LiteralPath (Join-Path $sdkRoot "platforms\android-36"))) {
    throw "未找到 Android SDK API 36：$sdkRoot"
}

$escapedSdk = $sdkRoot.Replace("\", "/").Replace(":", "\:")
Set-Content -LiteralPath (Join-Path $projectRoot "local.properties") `
    -Value "sdk.dir=$escapedSdk" `
    -Encoding utf8NoBOM

$env:JAVA_HOME = $javaHome
$env:Path = "$(Join-Path $javaHome "bin");$env:Path"

$localGradle = Join-Path $projectRoot "..\.build\android-tools\gradle\gradle-9.1.0\bin\gradle.bat"
$gradle = if (Test-Path -LiteralPath $localGradle) {
    (Resolve-Path -LiteralPath $localGradle).Path
} else {
    Join-Path $projectRoot "gradlew.bat"
}

Push-Location $projectRoot
try {
    if ($Clean) {
        & $gradle --no-daemon --console=plain clean
        if ($LASTEXITCODE -ne 0) { throw "Android clean 失败。" }
    }

    & $gradle --no-daemon --console=plain testDebugUnitTest assembleDebug
    if ($LASTEXITCODE -ne 0) { throw "Android 测试或 APK 构建失败。" }

    if (-not $SkipLint) {
        & $gradle --no-daemon --console=plain lintDebug
        if ($LASTEXITCODE -ne 0) { throw "Android Lint 失败。" }
    }
} finally {
    Pop-Location
}

$apk = Join-Path $projectRoot "app\build\outputs\apk\debug\app-debug.apk"
Write-Host "Android Debug APK: $apk"
