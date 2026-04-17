using System.Net;
using System.Net.Sockets;
using System.Text;
#if ANDROID
using Android.Content;
using Android.Provider;
using Application = Android.App.Application;

#endif
namespace LanTransferApp;

public partial class MainPage : ContentPage
{
    private TcpClient _tcpClient;
    private NetworkStream _netStream;
    private CancellationTokenSource _cancellationTokenSource;

    private const int UDP_DISCOVERY_PORT = 9999;
    private const string DISCOVERY_MSG = "DISCOVER_LAN_SERVER";

    public MainPage()
    {
        InitializeComponent();
        LoadSettings(); // 启动时加载配置  
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // 启动时先检查并申请权限
        await CheckAndRequestStoragePermission();

        await AutoDiscoverServerAsync();
    }

    private async Task CheckAndRequestStoragePermission()
    {
#if ANDROID
        // 仅针对 Android 11 (API 30) 及以上版本
        if ((int)Android.OS.Build.VERSION.SdkInt >= 30)
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                bool result = await DisplayAlert("权限请求", "为了方便你查找文件，我们需要【所有文件访问权限】将文件直接保存到公共的 Download 文件夹中。请在接下来的设置界面中允许。", "去设置", "取消");
                if (result)
                {
                    try
                    {
                        var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                        intent.SetData(Android.Net.Uri.Parse("package:" + Application.Context.PackageName));
                        intent.SetFlags(ActivityFlags.NewTask);
                        Application.Context.StartActivity(intent);
                    }
                    catch (Exception)
                    {
                        // 兼容极少数魔改系统：如果上面的 Intent 失败，退而求其次打开总的文件管理权限列表
                        var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                        intent.SetFlags(ActivityFlags.NewTask);
                        Application.Context.StartActivity(intent);
                    }
                }
            }
        }
        else
        {
            // Android 10 及以下版本，走普通的 MAUI 权限申请逻辑
            var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.StorageWrite>();
                await Permissions.RequestAsync<Permissions.StorageRead>();
            }
        }
#endif
    }

    // ==================== 0. 配置与设置面板逻辑 ====================
    private void LoadSettings()
    {
        // 使用 MAUI 自带的 Preferences 保存轻量级配置
        txtIp.Text = Preferences.Default.Get("SavedIp", "");
        txtPort.Text = Preferences.Default.Get("SavedPort", "8888").ToString();

        txtSettingIp.Text = txtIp.Text;
        txtSettingPort.Text = txtPort.Text;
        txtSettingPath.Text = FileSystem.AppDataDirectory; // 手机端强制使用 App 沙盒目录以避免权限崩溃
    }

    private void BtnOpenSettings_Clicked(object sender, EventArgs e) => ContainerSettings.IsVisible = true;

    private void BtnSaveSettings_Clicked(object sender, EventArgs e)
    {
        Preferences.Default.Set("SavedIp", txtSettingIp.Text?.Trim());
        Preferences.Default.Set("SavedPort", txtSettingPort.Text?.Trim());

        txtIp.Text = txtSettingIp.Text;
        txtPort.Text = txtSettingPort.Text;

        ContainerSettings.IsVisible = false;
    }

    // ==================== 1. 连接逻辑 (自动 + 手动 + 重连) ====================
    private async Task AutoDiscoverServerAsync()
    {
        // 界面初始化
        ContainerLogin.IsVisible = true;
        ContainerChat.IsVisible = false;
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;
        lblStatus.Text = "正在局域网寻找电脑端...";
        ContainerManual.IsVisible = false;

        using UdpClient udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        try
        {
            byte[] requestData = Encoding.UTF8.GetBytes(DISCOVERY_MSG);
            await udpClient.SendAsync(requestData, requestData.Length, new IPEndPoint(IPAddress.Broadcast, UDP_DISCOVERY_PORT));

            var receiveTask = udpClient.ReceiveAsync();
            if (await Task.WhenAny(receiveTask, Task.Delay(2000)) == receiveTask)
            {
                var result = await receiveTask;
                string[] parts = Encoding.UTF8.GetString(result.Buffer).Split('|');
                if (parts.Length == 3 && parts[0] == "LAN_SERVER_REPLY")
                {
                    await ConnectToServerAsync(result.RemoteEndPoint.Address.ToString(), int.Parse(parts[1]));
                    return;
                }
            }
        }
        catch (Exception) { }

        // 寻找失败，显示手动连接
        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;
        lblStatus.Text = "未找到电脑，请手动连接";
        ContainerManual.IsVisible = true;
    }

    private async void BtnManualConnect_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtIp.Text) || !int.TryParse(txtPort.Text, out int port)) return;

        lblStatus.Text = "正在连接...";
        loadingIndicator.IsVisible = true;
        loadingIndicator.IsRunning = true;
        await ConnectToServerAsync(txtIp.Text.Trim(), port);
    }

    private async void BtnReconnect_Clicked(object sender, EventArgs e)
    {
        btnReconnect.IsVisible = false;
        await AutoDiscoverServerAsync(); // 重新走一遍发现与连接流程
    }

    private async Task ConnectToServerAsync(string ip, int port)
    {
        try
        {
            _tcpClient?.Close(); // 确保清理旧连接
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ip, port);
            _netStream = _tcpClient.GetStream();
            _cancellationTokenSource = new CancellationTokenSource();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ContainerLogin.IsVisible = false;
                ContainerChat.IsVisible = true;
                btnReconnect.IsVisible = false;
                LogTextMessage("系统", "成功连接到电脑端！");
            });

            _ = Task.Run(() => ReceiveDataAsync(_cancellationTokenSource.Token));
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblStatus.Text = "连接失败，请检查配置";
                loadingIndicator.IsVisible = false;
                ContainerManual.IsVisible = true;
            });
        }
    }

    // ==================== 新增：中断传输事件 ====================
    private async void BtnCancelTransfer_Clicked(object sender, EventArgs e)
    {
        if (_isTransferring && _tcpClient != null)
        {
            bool result = await DisplayAlert("取消传输", "确定要中断传输吗？这会导致当前连接断开。", "确定", "取消");
            if (result)
            {
                // 粗暴但最安全的打断方式，触发异常走清理逻辑
                _tcpClient.Close();
            }
        }
    }


    // ==================== 2. 收发协议升级 (包含文件名) ====================
    // ==================== 2. 接收数据 (完整版) ====================
    private async Task ReceiveDataAsync(CancellationToken token)
    {
        string currentReceivingFilePath = null; // 用于记录当前接收路径，防止中断产生废文件

        try
        {
            while (!token.IsCancellationRequested && _tcpClient.Connected)
            {
                byte[] typeBuf = new byte[1];
                if (await ReadExactBytesAsync(_netStream, typeBuf, 1, token) == 0) break;
                byte dataType = typeBuf[0];

                if (dataType == 0) // 收到文本
                {
                    byte[] lenBuf = new byte[4];
                    await ReadExactBytesAsync(_netStream, lenBuf, 4, token);
                    int textLen = BitConverter.ToInt32(lenBuf, 0);

                    byte[] textBuf = new byte[textLen];
                    await ReadExactBytesAsync(_netStream, textBuf, textLen, token);

                    LogTextMessage("电脑", Encoding.UTF8.GetString(textBuf));
                }
                else if (dataType == 1) // 收到文件
                {
                    _isTransferring = true;

                    byte[] headerBuf = new byte[12];
                    await ReadExactBytesAsync(_netStream, headerBuf, 12, token);
                    long fileSize = BitConverter.ToInt64(headerBuf, 0);
                    int nameLength = BitConverter.ToInt32(headerBuf, 8);

                    byte[] nameBuf = new byte[nameLength];
                    await ReadExactBytesAsync(_netStream, nameBuf, nameLength, token);
                    string fileName = Encoding.UTF8.GetString(nameBuf);

                    string saveDir = FileSystem.AppDataDirectory;
#if ANDROID
                    saveDir = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "LanTransfer");
                    if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
#endif

                    string finalFilePath = Path.Combine(saveDir, fileName);
                    int count = 1;
                    while (File.Exists(finalFilePath))
                    {
                        string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        finalFilePath = Path.Combine(saveDir, $"{nameOnly}({count}){ext}");
                        count++;
                    }

                    // 标记文件路径
                    currentReceivingFilePath = finalFilePath;

                    MainThread.BeginInvokeOnMainThread(() => UpdateProgressUI(true, $"接收中: {fileName} (0%)", 0));

                    using (FileStream fs = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[64 * 1024];
                        long totalRead = 0;

                        while (totalRead < fileSize)
                        {
                            int bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                            int readBytes = await ReadExactBytesAsync(_netStream, buffer, bytesToRead, token);
                            if (readBytes == 0) throw new Exception("网络异常断开");

                            await fs.WriteAsync(buffer, 0, readBytes, token);
                            totalRead += readBytes;

                            double percent = (double)totalRead / fileSize;
                            MainThread.BeginInvokeOnMainThread(() => UpdateProgressUI(true, $"接收中: {fileName} ({(int)(percent * 100)}%)", percent));
                        }
                    }

                    // 正常完成，清空记录
                    currentReceivingFilePath = null;
                    LogFileMessage("电脑", Path.GetFileName(finalFilePath), finalFilePath);

                    _isTransferring = false;
                    MainThread.BeginInvokeOnMainThread(() => UpdateProgressUI(false));

                    MainThread.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("接收完毕", $"文件已接收: {Path.GetFileName(finalFilePath)}", "确定");
                    });
                }
            }
        }
        catch (Exception)
        {
            // 异常说明断线或被取消了
        }
        finally
        {
            _isTransferring = false;

            // 核心：清理没传完的半截文件
            if (!string.IsNullOrEmpty(currentReceivingFilePath) && File.Exists(currentReceivingFilePath))
            {
                try { File.Delete(currentReceivingFilePath); } catch { }
            }

            MainThread.BeginInvokeOnMainThread(() => {
                UpdateProgressUI(false);
                LogTextMessage("系统", "已断开连接。");
                btnReconnect.IsVisible = true;
            });
        }
    }

    private async Task<int> ReadExactBytesAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
            if (read == 0) return 0;
            totalRead += read;
        }
        return totalRead;
    }

    private void BtnSend_Clicked(object sender, EventArgs e) => SendMessage();
    private void TxtInput_Completed(object sender, EventArgs e) => SendMessage();

    // ==================== 3. 发送文本消息 (完整版) ====================
    private void SendMessage()
    {
        if (_isTransferring) return; // 传文件时静默拦截发消息

        string message = txtInput.Text?.Trim();
        if (string.IsNullOrEmpty(message) || _tcpClient == null || !_tcpClient.Connected) return;

        try
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);

            byte[] header = new byte[5];
            header[0] = 0; // 文本类型
            BitConverter.GetBytes(payload.Length).CopyTo(header, 1);

            _netStream.Write(header, 0, header.Length);
            _netStream.Write(payload, 0, payload.Length);
            _netStream.Flush();

            LogTextMessage("我", message);
            txtInput.Text = string.Empty;
        }
        catch (Exception)
        {
            LogTextMessage("系统", "发送失败，已断开");
        }
    }

    private bool _isTransferring = false;

    // ==================== 1. 发送文件 (完整版) ====================
    private async void BtnAddFile_Clicked(object sender, EventArgs e)
    {
        if (_tcpClient == null || !_tcpClient.Connected) return;
        if (_isTransferring)
        {
            await DisplayAlert("提示", "当前正在传输文件，请稍候。", "确定");
            return;
        }

        try
        {
            var result = await FilePicker.Default.PickAsync();
            if (result != null)
            {
                _isTransferring = true;
                UpdateProgressUI(true, "准备发送...");

                string fileName = result.FileName;
                using var stream = await result.OpenReadAsync();
                long fileSize = stream.Length;

                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);

                // 发送协议头: [Type(1)] + [Size(8)] + [NameLen(4)] + [NameBytes]
                byte[] header = new byte[1 + 8 + 4 + nameBytes.Length];
                header[0] = 1;
                BitConverter.GetBytes(fileSize).CopyTo(header, 1);
                BitConverter.GetBytes(nameBytes.Length).CopyTo(header, 9);
                nameBytes.CopyTo(header, 13);
                await _netStream.WriteAsync(header, 0, header.Length);

                // 分块流式读取与发送
                byte[] buffer = new byte[64 * 1024];
                int readBytes;
                long totalSent = 0;

                while ((readBytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await _netStream.WriteAsync(buffer, 0, readBytes);
                    totalSent += readBytes;

                    double percent = (double)totalSent / fileSize;
                    UpdateProgressUI(true, $"发送中: {fileName} ({(int)(percent * 100)}%)", percent);
                }

                await _netStream.FlushAsync();

                // 传输完成，直接用双击打开的方法渲染 UI
                LogFileMessage("我", fileName, result.FullPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"发送失败: {ex.Message}", "确定");
        }
        finally
        {
            _isTransferring = false;
            UpdateProgressUI(false);
        }
    }
    // ==================== 辅助方法 (完整版) ====================
    private void UpdateProgressUI(bool isVisible, string text = "", double progress = 0)
    {
        // MAUI的UI更新必须放在主线程
        MainThread.BeginInvokeOnMainThread(() => {
            panelProgress.IsVisible = isVisible;
            txtProgress.Text = text;
            pbTransfer.Progress = progress;
        });
    }

    private void SendData(byte type, byte[] payload)
    {
        byte[] header = new byte[5];
        header[0] = type;
        BitConverter.GetBytes(payload.Length).CopyTo(header, 1);
        _netStream.Write(header, 0, header.Length);
        _netStream.Write(payload, 0, payload.Length);
        _netStream.Flush();
    }
    /*
    private void LogMessage(string msg)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 使用 Editor 替代 Label，并设为只读，这样用户就可以长按或双击复制文本了 (Issue 1)
            var editor = new Editor
            {
                Text = $"[{DateTime.Now:HH:mm}] {msg}",
                IsReadOnly = true,
                AutoSize = EditorAutoSizeOption.TextChanges, // 自动撑开高度
                BackgroundColor = Colors.Transparent,
                Margin = new Thickness(0, 2)
            };
            lstMessages.Children.Add(editor);

            await Task.Delay(100);
            await scrollMessages.ScrollToAsync(lstMessages, ScrollToPosition.End, true);
        });
    }
    */
    // ==================== 专门处理【文本】消息的 UI 渲染 ====================
    private void LogTextMessage(string sender, string rawMessage)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 界面上显示的带时间的完整字符串
            string displayString = $"[{DateTime.Now:HH:mm}] {sender}: {rawMessage}";

            var label = new Label
            {
                Text = displayString,
                FontSize = 14,
                Margin = new Thickness(0, 5)
            };

            // 添加双击手势
            var tapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tapGesture.Tapped += async (s, e) =>
            {
                // 核心点：只把 rawMessage (纯文本) 塞进剪贴板，不要带时间和发送者
                await Clipboard.Default.SetTextAsync(rawMessage);
                await DisplayAlert("提示", "消息已复制到剪贴板", "确定");
            };
            label.GestureRecognizers.Add(tapGesture);

            lstMessages.Children.Add(label);
            await Task.Delay(100);
            await scrollMessages.ScrollToAsync(lstMessages, ScrollToPosition.End, true);
        });
    }

    // ==================== 专门处理【文件】消息的 UI 渲染 ====================
    private void LogFileMessage(string sender, string fileName, string filePath)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // 界面上加个【文件】前缀，并提示双击打开
            string displayString = $"[{DateTime.Now:HH:mm}] {sender}: 📁 [文件] {fileName}\n(👉 双击使用其他应用打开)";

            var label = new Label
            {
                Text = displayString,
                TextColor = Colors.MediumPurple, // 给个特殊的颜色区分
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 5)
            };

            // 添加双击手势
            var tapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tapGesture.Tapped += async (s, e) =>
            {
                try
                {
                    // 核心点：利用 MAUI 的 Launcher 包装私有路径，唤起系统弹窗选择第三方软件打开
                    await Launcher.Default.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(filePath)));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("错误", $"无法打开此文件: {ex.Message}", "确定");
                }
            };
            label.GestureRecognizers.Add(tapGesture);

            lstMessages.Children.Add(label);
            await Task.Delay(100);
            await scrollMessages.ScrollToAsync(lstMessages, ScrollToPosition.End, true);
        });
    }
}