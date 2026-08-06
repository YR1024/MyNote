# DreamyCinemaSite

面向手机浏览器的本地视频片库。

## 技术栈

- 前端：Vue 3、TypeScript、Vite、Vue Router、Pinia。
- 后端：ASP.NET Core、EF Core、SQLite。
- Android：Kotlin、Material 3 Views、Media3/ExoPlayer，复用同一 ASP.NET Core API 和片库数据。
- 部署：前端源码位于 `DreamyCinema.Web`，生产构建输出到 `wwwroot`，由 ASP.NET Core 通过同一地址提供页面和 API。
- 播放：封面和视频共用一个媒体区域；点击中央按钮在列表内播放，“播放页”按钮进入 `/videos/{id}/play` 独立播放页。
- 媒体处理：同步时使用 ffprobe 提取时长、分辨率、编码和内嵌字幕轨道，并使用 FFmpeg 自动截取视频中间帧作为封面。
- 字幕：外挂或内嵌文本字幕会解析为数据库 Cue，并按需输出 WebVTT；播放器支持浏览器原生、Vue 画面内自绘和画面下方兼容三种显示方式，触屏设备默认使用不会被原生视频层覆盖的画面下方模式。
- 硬字幕：可按单个视频和字幕轨道创建 H.264 硬字幕衍生版，在原始视频与硬字幕版之间切换，并在明确确认后删除原片释放空间。
- AI 字幕：识别和翻译通过可替换 Provider 接口进入持久化后台任务；已接入本机 faster-whisper large-v3 与 llama.cpp/Qwen3-8B，仓库默认配置仍保持关闭。
- 字幕校对：移动端原文/中文对照、视频定位、搜索、逐条修正、质量警告、选区重译、双语轨道和 UTF-8 SRT/WebVTT 导出。
- 后台任务：同步请求写入 SQLite 任务队列，由单个后台 Worker 执行；页面可查看进度、取消、重试，并在刷新或服务重启后恢复。
- 批量整理：手机端可多选当前已加载视频，统一增删标签、确认清晰度建议、重新分析媒体信息、重新生成封面或移入回收站。
- 数据备份与恢复：服务运行期间创建 SQLite 一致性快照；ZIP 可下载、重新导入和恢复，恢复失败会自动回滚到操作前快照。
- 账户与安全：管理员可在手机端修改密码并使所有旧登录立即失效；最近管理操作写入独立的限长审计日志，不记录密码、Cookie、CSRF 或请求正文。

## FFmpeg

自动媒体信息和封面依赖 FFmpeg。Windows 可通过 winget 安装：

```powershell
winget install --id Gyan.FFmpeg --exact
```

程序会依次使用 `MediaTools` 中配置的路径、WinGet 命令链接和系统 `PATH`。FFmpeg 暂时不可用时，视频仍会正常导入，并在同步结果中显示媒体处理警告。

## 本地 AI 字幕

模型、CUDA 运行库和 Python 虚拟环境安装在 `%LOCALAPPDATA%\DreamyCinema\AI`，不会进入 Git。首次部署执行：

```powershell
.\scripts\Setup-LocalAi.ps1
Copy-Item .\appsettings.Local.example.json .\appsettings.Local.json
```

`appsettings.Local.json` 已被 `.gitignore` 排除。真实配置使用 `FasterWhisper` 与 `LlamaCpp` Provider；地址、模型、超时、音频分块、字幕分块、人物名和词汇表都从配置读取。不要把 API 密钥写入 `appsettings.json`。

翻译默认每批 12 条、温度为 0，并在结构校验后执行一次原文—译文语义审查。错译、漏译或幻觉批次会自动重试，失败批次不会作为下一批上下文。播放器已有中文轨道时会显示“重新翻译为中文”，新结果另存为重译稿，不覆盖旧稿。

启动模型服务：

```powershell
.\scripts\Start-LocalAi.ps1
```

完整启动网站与模型服务：

```powershell
.\scripts\Start-HomeServer.ps1
```

网站监听 `0.0.0.0:5210`；faster-whisper 只监听 `127.0.0.1:8001`，llama.cpp 只监听 `127.0.0.1:8080`。手机只能访问网站，不能访问模型接口。后台 Worker 串行执行识别与翻译，llama 空闲时卸载模型，Whisper 最后一块完成后释放模型，避免两者争用 8 GB 显存。

当前固定运行组合为 faster-whisper 1.2.1、CTranslate2 4.8.1、CUDA 12 cuBLAS/cuDNN 9 运行库、llama.cpp `b10015`、Qwen3-8B Q4_K_M。没有安装完整 CUDA Toolkit。重复部署、健康检查、停止和隔离真实 Provider 测试见 `scripts` 目录及 `docs/ai-subtitle-architecture.md`。

## 字幕校对

视频生成中文轨道后，播放页顶部会显示“校对”。校对页支持：

- 选择原文和中文轨道，按 Cue 时间轴对照并点击时间定位播放；
- 搜索原文或中文，筛选质量警告和人工修改；
- 修正原文时自动创建 `SourceCorrected`，不覆盖外挂字幕或 Whisper 原始识别稿；
- 修改中文时自动创建 `FinalPolished`，不覆盖 AI `ChineseDraft`；
- 选择未人工修改的 Cue 局部重译，其余 Cue 和人工修改保持不变；
- 检查空译文、时间轴错位、字幕过长和阅读速度过快；
- 生成原文/中文双语轨道，导出 UTF-8 SRT 或 WebVTT；
- 在当前设备记住最近选择的字幕语言，不记录播放进度。

## 硬字幕手机版

播放页的“硬字幕手机版”按所选字幕轨道创建持久化后台任务。生成前可在“烧录样式”弹窗中设置字体、字号、字体颜色与不透明度、描边颜色与宽度、底部间距；弹窗使用当前封面预览，实际渲染仍以 FFmpeg/libass 为准。所选样式会固化到任务及生成版本，重试和再次查看不会受之后的全局配置变化影响。

FFmpeg 离线把文字烧入视频像素，不会在播放时实时转码；生成期间可以离开页面，任务支持进度、取消、失败重试和服务重启恢复。完成文件位于：

```text
Videos/derived/hard-subtitles/{videoId}/*.mp4
```

原片存在时可在播放页切换“原始视频”和“硬字幕版”，两者都使用 HTTP Range。修改已烧录的字幕轨道会把硬字幕版标记为过期；过期版本仍可播放，但必须重新生成后才能删除原片。删除原片需要输入该视频的完整番号，完成后不能再更换字幕或重新生成，现有硬字幕版也不能删除。

默认家庭服务器配置使用 RTX 3060 Ti 的 `h264_nvenc`：

```json
"HardSubtitle": {
  "VideoEncoder": "h264_nvenc",
  "Preset": "p6",
  "CpuPreset": "fast",
  "Quality": 20,
  "AudioBitrateKbps": 192,
  "FontName": "Microsoft YaHei",
  "FontSize": 18,
  "TextColor": "#FFFFFF",
  "TextOpacity": 92,
  "OutlineColor": "#000000",
  "Outline": 2,
  "MarginV": 24,
  "TimeoutHours": 24
}
```

这些样式字段是弹窗的默认值；字号允许 `12-48`，字体不透明度允许 `20-100`，描边宽度允许 `0-6`，底部间距允许 `0-160`，颜色使用 `#RRGGBB`。配置为 `h264_nvenc` 时会优先使用 NVIDIA；若 FFmpeg 无法加载 CUDA/NVENC 驱动或没有可用设备，同一任务会自动改用 `libx264` 和 `CpuPreset`。也可以把 `VideoEncoder` 直接设为 `libx264`，始终使用 CPU。硬字幕会重新编码视频，输出大小和画质取决于编码器与 `Quality`；上线前应先用有代表性的短片比较速度、画质和体积。

## 运行

```powershell
dotnet run --urls http://0.0.0.0:5210
```

`dotnet build` 和 `dotnet run` 会自动执行 Vue 类型检查及生产构建。首次构建时如尚未安装前端依赖，会自动运行 `npm install`。

默认监听本机所有网络接口的 5210 端口。服务器电脑访问：

```text
http://127.0.0.1:5210/
```

同一局域网内的手机或其他电脑使用服务器的局域网 IP，例如：

```text
http://192.168.28.50:5210/
```

`0.0.0.0` 只是服务监听地址，不是浏览器访问地址。电脑的局域网 IP 可能变化，可运行 `ipconfig` 查看当前 IPv4 地址。

Windows 防火墙需要允许专用网络的 TCP 5210 入站连接。本机当前规则 `DreamyCinemaSite TCP 5210` 仅允许本地子网访问。手机应和服务器连接同一局域网，且不能使用开启客户端隔离的访客 Wi-Fi。

## Android 播放客户端

原生 Android 客户端位于 `DreamyCinema.Android`。它通过现有 Cookie/CSRF 认证连接同一服务端，支持视频库搜索、排序、卡片标签交集筛选、分页、封面，以及原始/硬字幕源和 WebVTT 字幕播放。Media3 把视频与软字幕放在同一个原生 `PlayerView` 中，避免小米浏览器全屏时网页 DOM 字幕被系统视频层排除。

Android 的管理中心可执行视频同步、标签分类与标签管理、回收站维护、缺失记录清理以及数据库备份的新建、导入、下载、恢复和删除。播放页可启动 AI 语音识别/中文翻译任务，查看、取消和重试任务，设置并生成硬字幕版本，删除硬字幕版，或在输入番号确认后删除原视频。

构建并生成可安装的 Debug APK：

```powershell
cd DreamyCinema.Android
.\Build-Android.ps1
```

连接已开启 USB 调试的手机后安装：

```powershell
.\Install-AndroidApp.ps1
```

App 中填写服务器电脑的实际局域网地址，不能填写 `0.0.0.0`。Android 与浏览器共用同一服务端、数据库、媒体目录和后台任务，不会在手机上复制片库或运行 AI 模型。完整说明见 `DreamyCinema.Android/README.md`。

## 前端开发

后端保持运行后，在另一个终端启动 Vite：

```powershell
cd DreamyCinema.Web
npm install
npm run dev
```

开发页面位于 `http://127.0.0.1:5173/`，Vite 会把 `/api` 请求代理到 `http://127.0.0.1:5210`。生产和手机访问仍使用 5210。

移动端端到端测试使用隔离数据目录，不操作真实片库：

```powershell
cd DreamyCinema.Web
npm run test:e2e
```

## 首次登录

网站默认需要管理员登录。第一次启动后，在服务器电脑上打开：

```text
http://127.0.0.1:5210/
```

页面会要求创建管理员密码，至少 10 个字符。首次设置仅允许从本机回环地址完成，手机不能抢先设置。密码不会明文保存；随机盐和 PBKDF2-SHA256 哈希写入：

```text
Data/admin-credentials.json
```

设置完成后，手机使用同一密码登录。登录 Cookie 有效期为 12 小时。所有 API 默认要求登录，所有写请求还需要 CSRF token；连续错误登录会被限流。

片库页面的“账户”入口会显示本次登录到期时间、密码更新时间和当前访问边界。修改密码需要再次输入当前密码；成功后凭据安全标记会轮换，本机与其他设备上的全部旧 Cookie 都会立即失效并要求使用新密码重新登录。安全审计默认写入 `Data/admin-audit.jsonl`，达到 2 MiB 后轮转为一个 `.previous` 文件。

局域网 HTTP 适合开发测试，但密码传输没有 TLS 保护。不要在路由器上把 5210 端口映射到公网；长期使用应通过 HTTPS 或可信 VPN 访问。

## 添加视频

把 `.mp4` 文件放到项目的 `Videos` 目录，然后在网页左侧点击“同步视频”。

外挂字幕可与视频一起放入 `Videos` 顶层。文件名使用“视频番号或原文件名 + 可选语言 + 字幕扩展名”，例如：

```text
ABC-123.mp4
ABC-123.en.srt
ABC-123.zh-CN.ass
```

支持 `.srt`、`.vtt`、`.ass` 和 `.ssa`，文本编码支持 UTF-8 与 GB18030。同步后原字幕移入 `Videos/subtitles/{videoId}`；视频中的可转换文本字幕轨道也会由 FFmpeg 提取。图片型字幕轨道暂不能转换，会在同步结果中显示警告。

同步会做这几件事：

```text
Videos/*.mp4
  -> Videos/originals/yyyy/MM/{videoId}.mp4
  -> 提取时长、分辨率、视频编码
  -> Videos/covers/yyyy/MM/{videoId}-auto.jpg
  -> Videos/subtitles/{videoId}/*
  -> 写入 SQLite 数据库
  -> 刷新网页视频列表
```

当前版本只做本地磁盘视频：

- `GET /api/videos?page=1&pageSize=20`：分页返回可播放视频，包含 `items`、`total`、`page`、`pageSize` 和 `hasMore`；每页最多 50 条。
- `GET /api/videos/{id}`：返回单个视频及其标签，供编辑界面读取。
- `GET /api/videos/{id}/subtitles`：返回该视频的字幕轨道。
- `GET /api/subtitles/{id}/vtt`：根据数据库 Cue 输出浏览器可播放的 WebVTT。
- `GET /api/ai-subtitles/status`：返回不含密钥的 AI Provider 可用状态。
- `POST /api/videos/{id}/subtitles/transcribe`：创建语音识别任务。
- `POST /api/videos/{id}/subtitles/{trackId}/translate`：把指定原文轨道翻译成中文，并保持 Cue 时间轴不变。
- `GET /api/videos/{id}/subtitle-review`：返回原文/中文轨道和逐条质量警告。
- `PUT /api/videos/{id}/subtitle-review/source-cue`：复制并修正一条原文 Cue。
- `PUT /api/videos/{id}/subtitle-review/cue`：复制并保存一条中文人工校对 Cue。
- `POST /api/videos/{id}/subtitle-review/retranslate`：只重新翻译选中的未人工修改 Cue。
- `POST /api/videos/{id}/subtitle-review/bilingual`：生成或刷新双语字幕轨道。
- `GET /api/subtitles/{id}/export?format=srt|vtt`：导出 UTF-8 字幕文件。
- `GET /api/hard-subtitle/options`：返回硬字幕弹窗默认样式。
- `POST /api/videos/{id}/hard-subtitle`：按指定字幕轨道和样式创建或返回当前硬字幕后台任务。
- `DELETE /api/videos/{id}/hard-subtitle`：在原片仍存在时删除硬字幕衍生版。
- `POST /api/videos/{id}/original/delete`：校验最新硬字幕版和完整番号后永久删除原片。
- `GET /api/videos?tagIds=1,2`：按多选标签筛选视频；视频必须包含全部已选标签。
- `GET /api/videos?q=abc&sort=number-asc`：搜索番号、标题、简介或原文件名，并按指定方式排序。
- `GET /api/tag-categories`：返回标签类别和标签列表。
- `POST /api/tag-categories`：新增标签分类。
- `PUT /api/tag-categories/{id}`：重命名标签分类。
- `DELETE /api/tag-categories/{id}`：删除分类及其中的标签关联，不删除视频。
- `POST /api/tag-categories/{id}/tags`：在分类中新增标签。
- `PUT /api/tags/{id}`：重命名标签。
- `DELETE /api/tags/{id}`：删除标签及视频标签关联，不删除视频。
- `PUT /api/videos/{id}`：更新番号、标题、简介和多选标签；番号在片库中唯一。
- `POST /api/videos/batch/tags`：为明确选择的最多 200 个可用视频批量添加或移除标签。
- `POST /api/videos/batch/resolution-suggestions`：按视频短边返回 4K、1080P、720P 标签建议，不修改数据。
- `POST /api/videos/batch/resolution-tags`：重新计算并确认应用清晰度建议，只替换“清晰度”分类中的标签。
- `POST /api/videos/batch/media-jobs`：创建 `Reanalyze` 或 `RegenerateCover` 后台任务。
- `POST /api/videos/batch/trash`：整批预检文件后移动到回收站；任一步失败会回滚数据库并尝试恢复已移动文件。
- `GET /api/videos/{id}/cover`：读取视频封面。
- `POST /api/videos/{id}/cover`：上传 JPEG、PNG 或 WebP 封面，最大 8 MB。
- `DELETE /api/videos/{id}/cover`：移除视频封面。
- `GET /api/videos/maintenance`：返回回收站和缺失文件记录，并重新检查缺失状态。
- `POST /api/videos/{id}/trash`：将视频文件移动到本地回收站。
- `POST /api/videos/{id}/restore`：把回收站视频恢复到原管理目录。
- `DELETE /api/videos/{id}`：永久删除回收站或缺失记录；可用视频必须先移入回收站。
- `POST /api/videos/sync`：创建或返回当前同步任务，响应为 `202 Accepted`，媒体处理不再占用该 HTTP 请求。
- `GET /api/jobs`：返回最近 50 个后台任务。
- `GET /api/jobs/{id}`：返回任务阶段、进度、当前文件、错误、执行次数和时间。
- `POST /api/jobs/{id}/cancel`：取消等待中或运行中的任务；AI HTTP 请求及媒体分析/封面/音频 FFmpeg 进程会收到协作式取消信号。
- `POST /api/jobs/{id}/retry`：重新排队失败或已取消的任务。
- `GET /api/videos/{id}/stream?source=original|hard-subtitle`：返回指定播放版本并启用 HTTP Range；省略来源时优先原片，原片不存在时回退到硬字幕版。
- `GET /api/backups`：列出服务器上的备份包、大小、SHA-256、内容计数和校验状态。
- `POST /api/backups`：创建并校验新的时间戳 ZIP 备份。
- `POST /api/backups/import`：以 `multipart/form-data` 的 `file` 字段导入并完整校验 ZIP 备份。
- `POST /api/backups/{id}/restore`：创建恢复前快照后恢复数据库、封面、字幕和便携配置。
- `GET /api/backups/{id}/download`：下载指定备份包。
- `DELETE /api/backups/{id}`：删除指定备份包，不影响当前片库。
- `POST /api/auth/change-password`：验证当前密码后修改管理员密码，并使全部旧登录失效。
- `GET /api/security/audit?take=50`：读取最近的管理操作结果；最多返回 100 条，不包含请求正文或认证秘密。

## 数据备份

网页顶部的“数据备份”入口可以创建、下载和删除备份。默认备份目录为：

```text
Data/backups
```

可通过配置修改：

```json
{
  "Backup": {
    "Path": "D:/DreamyCinema/Backups",
    "PortableSettingsPath": "D:/DreamyCinema/Data/portable-settings.json",
    "MaxUploadBytes": 2147483648,
    "MaxExpandedBytes": 4294967296
  }
}
```

每个 ZIP 使用 SQLite 在线备份 API 生成一致数据库快照，并包含：

```text
manifest.json
database/dreamy-cinema.db
configuration/portable-settings.json
assets/covers/...
assets/subtitles/...
```

`manifest.json` 记录格式版本、创建时间、应用版本、数据计数，以及每个文件的大小和 SHA-256。创建完成前使用临时目录和 `.partial` 文件，只有完整校验通过的包才会出现在列表中。

备份不会包含 `Videos/originals`、`Videos/derived`、`Videos/trash`、管理员凭据、安全审计日志、Cookie、`appsettings.Local.json`、AI API 密钥、Provider 地址或本机模型路径。`portable-settings.json` 只保存与字幕结果可迁移性有关的目标语言、翻译风格、限制、人物名和词汇表。迁移到另一台电脑时必须另外复制原片和硬字幕衍生文件；若恢复数据库时对应衍生文件不存在，服务会清空该硬字幕指针并将没有任何可播放文件的视频标记为缺失。

恢复既可以选择服务器已有备份，也可以先从网页导入 ZIP。服务器会在改动数据前校验 ZIP 条目、路径、大小、SHA-256、数据库结构、完整性、外键和记录计数，并自动保留一份“恢复前快照”。恢复范围只有数据库、`Videos/covers`、`Videos/subtitles` 和便携配置；`Videos/originals` 与 `Videos/trash` 不会被移动或删除。数据库、资产或配置应用失败时会回滚所有已改变的层。

恢复期间新的 API 写操作和后台媒体任务会被拒绝；已有写操作或 AI/同步任务未结束时，恢复请求会直接返回冲突。恢复完成后会按磁盘上的原视频重新计算缺失状态。便携 AI 字幕偏好在下次重启服务后加载，本机 Provider 地址、模型和密钥仍以 `appsettings.Local.json`、环境变量或命令行配置为准。

## 数据结构

视频和标签是多对多关系：

```text
Videos
TagCategories
Tags
VideoTags
SubtitleTracks
SubtitleCues
MediaJobs
AiJobChunks
```

视频编辑不会移动实际 mp4 文件。封面保存在 `Videos/covers/yyyy/MM`，字幕原文件保存在 `Videos/subtitles/{videoId}`，硬字幕衍生文件保存在 `Videos/derived/hard-subtitles/{videoId}`；数据库保存两种视频来源的可用状态、相对路径、硬字幕样式、轨道信息和稳定时间轴 Cue。字幕轨道还保存 `RevisionStage`：`RawRecognition`、`SourceCorrected`、`ChineseDraft`、`FinalPolished` 分层保存；Cue 使用 `IsManuallyEdited` 保护人工内容，校对流程不得覆盖原始识别稿或 AI 初译稿。

同步也会补齐已有视频中缺失的媒体信息和自动封面。手动上传的封面在保留期间不会被覆盖；在编辑窗口移除封面后，该视频会在下一次同步时重新生成自动封面。

片库中的“批量整理”只操作管理员逐个勾选或“全选已加载”的视频，不会隐式处理尚未加载的筛选结果。重新生成封面是明确的替换操作，会覆盖手动封面；重新分析和封面生成使用持久化 `MediaMaintenance` 任务，可取消、失败后重试，并在服务重启后恢复。列表和播放页会对 HEVC、AV1、未知或其他潜在不兼容编码给出提示，但不会自动转码。

普通删除会把视频移动到：

```text
Videos/trash/{videoId}/{storedFileName}.mp4
```

网页顶部“回收站”可以恢复视频或永久删除。永久删除会移除视频文件、封面、数据库记录和标签关联。缺失记录也可从这里清理；如果原文件已经重新出现，系统会阻止直接清理并恢复为可用状态。

默认标签类别：

```text
类型：伦理 / 动作 / 爱情 / 戏剧 / 悬疑 / 喜剧
地区：日本 / 欧美 / 国产 / 韩国
年份：2026 / 2025 / 2024 / 更早
清晰度：4K / 1080P / 720P
状态：未看 / 已看 / 收藏
```

默认标签只在数据库首次创建时加入。之后可以通过网页顶部的“标签”入口自由增删或重命名，重启服务不会恢复已删除的标签。

视频列表支持以下排序值：

```text
imported-desc：最近同步
created-desc：日期最新
created-asc：日期最早
number-asc：番号排序
size-desc：文件最大
```

手机端首屏加载 20 条视频，滚动接近列表底部时自动加载下一页。搜索、排序或标签筛选变化后会重新从第一页加载；封面图片使用浏览器懒加载。

同步按钮只负责创建后台任务。任务状态保存在 `MediaJobs` 表，同一时间只会运行一个 Worker；同类同步请求会返回已有活动任务。服务意外停止时，数据库中处于 `Running` 的任务会在下次启动后重新排队，`AttemptCount` 会记录实际执行次数。

目录和数据库可在 `appsettings.json` 中配置：

```json
"VideoStorage": {
  "RootPath": "Videos"
},
"Database": {
  "Path": "Data/dreamy-cinema.db"
},
"Security": {
  "CredentialPath": "Data/admin-credentials.json",
  "AuditLogPath": "Data/admin-audit.jsonl"
},
"MediaTools": {
  "FfprobePath": "ffprobe",
  "FfmpegPath": "ffmpeg",
  "TimeoutSeconds": 90
},
"AiSubtitles": {
  "Enabled": false,
  "TargetLanguage": "zh-CN",
  "Speech": { "Provider": "Disabled", "BaseUrl": "http://127.0.0.1:8001/v1", "Model": "large-v3" },
  "Translation": { "Provider": "Disabled", "BaseUrl": "http://127.0.0.1:8080/v1", "Model": "Qwen3-8B-Q4_K_M" }
}
```

`FfprobePath` 和 `FfmpegPath` 可以省略；只有工具未加入 PATH 或需要固定版本时才需要显式配置。

AI 字幕的任务、分块恢复、Provider 边界和家庭服务器部署结构见 `docs/ai-subtitle-architecture.md`。`Mock` 只用于隔离测试，默认配置不会生成模拟字幕。
