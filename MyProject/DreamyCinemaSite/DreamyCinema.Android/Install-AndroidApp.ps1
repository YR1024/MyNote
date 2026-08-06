[CmdletBinding()]
param(
    [string]$ApkPath = (Join-Path $PSScriptRoot "app\build\outputs\apk\debug\app-debug.apk")
)

$ErrorActionPreference = "Stop"
$sdkRoot = $env:ANDROID_HOME
if (-not $sdkRoot) {
    $sdkRoot = $env:ANDROID_SDK_ROOT
}
if (-not $sdkRoot) {
    $sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}

$adb = Join-Path $sdkRoot "platform-tools\adb.exe"
if (-not (Test-Path -LiteralPath $adb)) {
    throw "未找到 adb：$adb"
}
if (-not (Test-Path -LiteralPath $ApkPath)) {
    throw "未找到 APK：$ApkPath。请先执行 .\Build-Android.ps1"
}

$devices = & $adb devices |
    Select-String -Pattern "^\S+\s+device$"
if (-not $devices) {
    throw "没有已授权的 Android 设备。请在手机开启 USB 调试、连接电脑并确认授权。"
}

& $adb install -r $ApkPath
if ($LASTEXITCODE -ne 0) {
    throw "APK 安装失败。"
}
