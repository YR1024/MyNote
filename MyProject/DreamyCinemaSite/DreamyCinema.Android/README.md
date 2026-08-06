# Dreamy Cinema Android

Dreamy Cinema 的原生 Android 播放客户端。它不保存独立片库，也不运行 AI
模型；所有视频、封面、标签、字幕和登录状态都来自现有 ASP.NET Core 服务。

## 已实现

- 配置家中服务器的 HTTP/HTTPS 地址并使用单管理员密码登录；
- 使用 Android Keystore 加密持久化服务端 Cookie，执行现有 CSRF 握手；
- 搜索、排序、分类标签多选交集筛选、分页追加和登录态封面加载；
- 点击视频卡片中的单个标签直接加入交集筛选，卡片其他区域进入播放页；
- 查看番号、标题、简介、标签、日期、大小、时长、分辨率和编码提示；
- 使用 Media3/ExoPlayer 播放原始视频或已生成的硬字幕版；
- 选择服务端 WebVTT 字幕，并在同一个原生 `PlayerView` 中横屏/全屏显示；
- 在管理中心启动/取消/重试同步，管理标签分类和标签，恢复或永久删除回收站视频；
- 新建、导入、下载、恢复和删除数据库备份，导入/下载使用 Android 文档选择器流式传输；
- 在播放页启动 AI 语音识别和中文翻译，查看、取消或重试后台任务；
- 选择字幕轨道和烧录样式生成硬字幕版，并安全删除硬字幕版或经番号确认删除原视频；
- 按系统状态栏、刘海区域和底部导航栏 Insets 布局；
- 登录失效后返回登录页，不在 URL 或日志中保存管理员密码。

视频元数据/封面编辑、批量整理、逐条字幕校对、账户改密和安全审计仍使用网页端。
Android 不运行 AI 模型或 FFmpeg；它只向家中 ASP.NET Core 服务提交并监视任务。

## 构建

需要 Microsoft OpenJDK 17 和 Android SDK API 36：

```powershell
winget install --id Microsoft.OpenJDK.17 --exact
cd DreamyCinema.Android
.\Build-Android.ps1
```

脚本会自动生成不提交 Git 的 `local.properties`，执行 JVM 单元测试、Debug APK
构建和 Android Lint。输出文件：

```text
app/build/outputs/apk/debug/app-debug.apk
```

也可以直接使用 Gradle Wrapper：

```powershell
.\gradlew.bat testDebugUnitTest assembleDebug lintDebug
```

## 安装

通过 USB 调试安装：

```powershell
.\Install-AndroidApp.ps1
```

也可以把 `app-debug.apk` 发送到手机后手动安装。Debug APK 使用 Android 调试
签名，只适合当前自用测试；正式发布前应配置独立 Release 签名和版本升级策略。

## 连接

1. 家中电脑启动 `dotnet run --urls http://0.0.0.0:5210`。
2. 手机和电脑连接同一可信局域网。
3. App 中填写电脑的实际局域网地址，例如 `http://192.168.1.50:5210`。
4. 输入网页端已经设置好的管理员密码。

`0.0.0.0` 是服务监听地址，不能填入 App。当前服务使用 HTTP，因此 App 为可配置
局域网地址明确启用了明文流量；不要通过路由器把 5210 映射到公网。

## 验证

当前自动验证覆盖 Kotlin/资源编译、JVM 单元测试、Android Lint、APK 打包和清单
检查。最终还需在小米手机上验证：

1. 标题栏完全位于通知栏下方，底部内容不被系统导航栏遮挡；
2. 点击卡片标签只筛选且使用交集，点击封面/正文才进入播放；
3. 管理中心的同步、标签、回收站和备份流程均能完成并刷新；
4. AI 与硬字幕任务在 App 切到后台再返回后仍显示服务器上的真实状态；
5. 原始/硬字幕切换、拖动进度、软字幕横屏/全屏和系统返回键正常。
