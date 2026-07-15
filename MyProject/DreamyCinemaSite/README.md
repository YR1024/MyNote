# DreamyCinemaSite

面向手机浏览器的本地视频片库。

## 技术栈

- 前端：Vue 3、TypeScript、Vite、Vue Router、Pinia。
- 后端：ASP.NET Core、EF Core、SQLite。
- 部署：前端源码位于 `DreamyCinema.Web`，生产构建输出到 `wwwroot`，由 ASP.NET Core 通过同一地址提供页面和 API。
- 播放：封面和视频共用一个媒体区域；点击中央按钮在列表内播放，“播放页”按钮进入 `/videos/{id}/play` 独立播放页。
- 媒体处理：同步时使用 ffprobe 提取时长、分辨率、编码和内嵌字幕轨道，并使用 FFmpeg 自动截取视频中间帧作为封面。
- 字幕：外挂或内嵌文本字幕会解析为数据库 Cue，并按需输出 WebVTT 给浏览器播放。
- AI 字幕：识别和翻译通过可替换 Provider 接口进入持久化后台任务；当前公司环境默认关闭，真实本地模型将在家用服务器接入。
- 后台任务：同步请求写入 SQLite 任务队列，由单个后台 Worker 执行；页面可查看进度、取消、重试，并在刷新或服务重启后恢复。

## FFmpeg

自动媒体信息和封面依赖 FFmpeg。Windows 可通过 winget 安装：

```powershell
winget install --id Gyan.FFmpeg --exact
```

程序会依次使用 `MediaTools` 中配置的路径、WinGet 命令链接和系统 `PATH`。FFmpeg 暂时不可用时，视频仍会正常导入，并在同步结果中显示媒体处理警告。

## 运行

```powershell
nvm use 24.18.0
dotnet run
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
- `POST /api/jobs/{id}/cancel`：取消等待中或运行中的任务；运行中会在当前文件步骤结束后安全停止。
- `POST /api/jobs/{id}/retry`：重新排队失败或已取消的任务。
- `GET /api/videos/{id}/stream`：按数据库中的 VideoId 返回 mp4 文件流，并启用 HTTP Range，浏览器可以拖动进度条。

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

视频编辑不会移动实际 mp4 文件。封面保存在 `Videos/covers/yyyy/MM`，字幕原文件保存在 `Videos/subtitles/{videoId}`，数据库保存相对路径、轨道信息和稳定时间轴 Cue。

同步也会补齐已有视频中缺失的媒体信息和自动封面。手动上传的封面在保留期间不会被覆盖；在编辑窗口移除封面后，该视频会在下一次同步时重新生成自动封面。

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
  "CredentialPath": "Data/admin-credentials.json"
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
