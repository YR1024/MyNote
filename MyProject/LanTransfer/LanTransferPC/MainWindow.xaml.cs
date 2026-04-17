using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using FolderBrowserDialog =  System.Windows.Forms.FolderBrowserDialog;
using System.Windows.Media.Imaging; // 处理剪贴板图片所需
namespace LanTransferPC
{
    public partial class MainWindow : Window
    {
        private bool isServerRunning = false;

        // 网络通信核心对象
        private TcpListener _serverListener;
        private TcpClient _connectedClient;
        private NetworkStream _netStream;
        private CancellationTokenSource _cancellationTokenSource;


        // ====== 新增：UDP 和当前端口变量 ======
        private UdpClient _udpListener;
        private int _currentTcpPort;
        private const int UDP_DISCOVERY_PORT = 9999; // 固定一个UDP端口用于自动发现

        private string _configFile = "config.ini";
        public MainWindow()
        {
            InitializeComponent();
            LoadConfig(); // 窗口初始化时加载配置
        }

        // ==================== 新增：配置保存与加载 ====================
        private void LoadConfig()
        {
            // 默认值
            txtPort.Text = "8888";
            txtSavePath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (File.Exists(_configFile))
            {
                string[] lines = File.ReadAllLines(_configFile);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Port=")) txtPort.Text = line.Substring(5);
                    if (line.StartsWith("SavePath=")) txtSavePath.Text = line.Substring(9);
                }
            }
        }

        private void SaveConfig()
        {
            string content = $"Port={txtPort.Text}\nSavePath={txtSavePath.Text}";
            File.WriteAllText(_configFile, content);
        }

        // ==================== 新增：路径按钮事件 ====================
        private void BtnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择文件接收保存的目录";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtSavePath.Text = dialog.SelectedPath;
                    SaveConfig(); // 选完立刻保存
                }
            }
        }

        private void BtnOpenPath_Click(object sender, RoutedEventArgs e)
        {
            string path = txtSavePath.Text;
            if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", path); // 打开 Windows 资源管理器
            }
            else
            {
                System.Windows.MessageBox.Show("目录不存在！");
            }
        }
     

        private void BtnToggleService_Click(object sender, RoutedEventArgs e)
        {
            if (!isServerRunning)
            {
                if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("请输入有效的端口号 (1-65535)");
                    return;
                }
                SaveConfig(); // 开启服务时保存当前设置的端口
                StartServer(port);
            }
            else
            {
                StopServer();
            }
        }

        // ==================== 网络通信核心逻辑 ====================

        private void StartServer(int port)
        {
            try
            {
                _currentTcpPort = port; // 记录当前 TCP 端口，方便 UDP 回复时告诉手机

                // 1. 启动 TCP 监听 监听所有网卡 IP
                _serverListener = new TcpListener(IPAddress.Any, port);
                _serverListener.Start();
                _cancellationTokenSource = new CancellationTokenSource();

                isServerRunning = true;
                UpdateUIState(true);
                LogMessage($"系统提示: 服务已在端口 {port} 开启，等待手机连接...");

                // 2. 启动 UDP 监听 (用于手机端自动发现)
                Task.Run(() => ListenForUdpDiscoveryAsync(_cancellationTokenSource.Token));

                // 3. 在后台线程等待 TCP 客户端连接   在后台线程异步等待客户端连接，防止 UI 卡死
                Task.Run(() => AcceptClientAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动服务失败: {ex.Message}");
                StopServer();
            }
        }

        private void StopServer()
        {
            isServerRunning = false;
            _cancellationTokenSource?.Cancel();

            // ====== 新增：关闭 UDP ======
            _udpListener?.Close();

            _serverListener?.Stop();
            _netStream?.Close();
            _connectedClient?.Close();

            UpdateUIState(false);
            LogMessage("系统提示: 服务已停止，断开所有连接。");
        }

        // ==================== UDP 自动发现逻辑 ====================
        private async Task ListenForUdpDiscoveryAsync(CancellationToken token)
        {
            try
            {
                // 绑定到固定的 UDP 端口
                _udpListener = new UdpClient(UDP_DISCOVERY_PORT);
                LogMessage($"[自动发现] UDP 监听已在端口 {UDP_DISCOVERY_PORT} 开启...");

                while (!token.IsCancellationRequested)
                {
                    // 等待接收局域网内的 UDP 广播
                    UdpReceiveResult result = await _udpListener.ReceiveAsync();
                    string requestData = Encoding.UTF8.GetString(result.Buffer);

                    // 验证是不是我们自家手机 App 发来的暗号
                    if (requestData == "DISCOVER_LAN_SERVER")
                    {
                        // 收到暗号！准备回复。
                        // 回复格式设计为: "LAN_SERVER_REPLY|TCP端口号|电脑名称"
                        // 注：手机端不需要我们发送IP，因为手机在收到这个UDP包时，底层能直接提取出发送者的IP。
                        string replyMessage = $"LAN_SERVER_REPLY|{_currentTcpPort}|{Environment.MachineName}";
                        byte[] replyBytes = Encoding.UTF8.GetBytes(replyMessage);

                        // 按原路返回给手机端
                        await _udpListener.SendAsync(replyBytes, replyBytes.Length, result.RemoteEndPoint);

                        LogMessage($"[自动发现] 已响应来自 {result.RemoteEndPoint.Address} 的搜索请求。");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // 正常停止服务调用 _udpListener.Close() 时会触发此异常，忽略即可
            }
            catch (Exception ex)
            {
                if (isServerRunning) // 避免关闭服务时报异常
                {
                    LogMessage($"UDP监听异常: {ex.Message}");
                }
            }
        }

        private async Task AcceptClientAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 挂起等待，直到有手机连进来
                    TcpClient client = await _serverListener.AcceptTcpClientAsync();

                    // 为了简单起见，本示例目前只保持一个最新连接（踢掉旧连接）
                    if (_connectedClient != null) { _connectedClient.Close(); }

                    _connectedClient = client;
                    _netStream = client.GetStream();

                    LogMessage($"[成功连接] 手机端IP: {client.Client.RemoteEndPoint}");

                    // 开启专门的线程读取这台手机发来的数据
                    _ = Task.Run(() => ReceiveDataAsync(token));
                }
            }
            catch (ObjectDisposedException) { /* 正常停止服务时会触发，忽略即可 */ }
            catch (Exception ex)
            {
                LogMessage($"接收连接时出错: {ex.Message}");
            }
        }

        // ==================== 2. 接收数据 (完整版) ====================
        private async Task ReceiveDataAsync(CancellationToken token)
        {
            string currentReceivingFilePath = null; // 用于记录当前正在接收的文件，防中断

            try
            {
                while (!token.IsCancellationRequested && _connectedClient.Connected)
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
                        LogMessage($"手机端: {Encoding.UTF8.GetString(textBuf)}");
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

                        string savePath = string.Empty;
                        Dispatcher.Invoke(() => savePath = txtSavePath.Text);
                        string finalFilePath = GetUniqueFilePath(savePath, fileName);

                        // 标记当前正在写的文件路径
                        currentReceivingFilePath = finalFilePath;

                        Dispatcher.Invoke(() => UpdateProgressUI(true, $"接收中: {fileName} (0%)", 0));

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

                                int percent = (int)((double)totalRead / fileSize * 100);
                                Dispatcher.Invoke(() => UpdateProgressUI(true, $"接收中: {fileName} ({percent}%)", percent));
                            }
                        }

                        // 正常接收完毕，清除记录
                        currentReceivingFilePath = null;
                        LogMessage($"[收到文件]: {Path.GetFileName(finalFilePath)}");

                        _isTransferring = false;
                        Dispatcher.Invoke(() => UpdateProgressUI(false));
                    }
                }
            }
            catch (Exception)
            {
                LogMessage("系统提示: 已与手机端断开连接。");
            }
            finally
            {
                _isTransferring = false;
                Dispatcher.Invoke(() => UpdateProgressUI(false));

                // 核心：如果有记录的文件且没被清空（说明抛异常被中断了），删除这个残缺的空壳文件
                if (!string.IsNullOrEmpty(currentReceivingFilePath) && File.Exists(currentReceivingFilePath))
                {
                    try { File.Delete(currentReceivingFilePath); } catch { }
                }
            }
        }

        // 辅助方法：确保读取到指定长度的字节，解决 TCP 半包问题
        private async Task<int> ReadExactBytesAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken token)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead, token);
                if (read == 0) return 0; // 对方关闭了流
                totalRead += read;
            }
            return totalRead;
        }

        // ==================== 新增：拖拽与剪贴板事件 ====================
        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    await SendFileCoreAsync(files[0]); // 拖入多个文件时，目前先处理第一个
                }
            }
        }

        private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 如果焦点在输入框，且按下的是回车，走发送消息逻辑 (防止冲突)
            if (e.Key == Key.Enter && txtInput.IsFocused)
            {
                SendMessage();
                return;
            }

            // 监听 Ctrl + V 粘贴快捷键
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // 1. 剪贴板里是文件 (比如复制了某个文件)
                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files.Count > 0)
                    {
                        await SendFileCoreAsync(files[0]);
                    }
                }
                // 2. 剪贴板里是图片 (比如用截图工具截的图)
                else if (Clipboard.ContainsImage())
                {
                    try
                    {
                        var image = Clipboard.GetImage();
                        if (image != null)
                        {
                            // 【核心修复】：丢弃有问题的透明通道，强制转换为不透明的 Bgr32 格式
                            // 完美解决 Snipaste、QQ截图 等软件复制到 WPF 时保存为黑图/全透明的问题
                            var opaqueBitmap = new FormatConvertedBitmap(image, PixelFormats.Bgr32, null, 0);

                            string tempPath = Path.Combine(Path.GetTempPath(), $"截图_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                            using (var fileStream = new FileStream(tempPath, FileMode.Create))
                            {
                                BitmapEncoder encoder = new PngBitmapEncoder();
                                // 这里传入转换后的 opaqueBitmap，而不是原来的 image
                                encoder.Frames.Add(BitmapFrame.Create(opaqueBitmap));
                                encoder.Save(fileStream);
                            }
                            // 发送这个临时文件
                            await SendFileCoreAsync(tempPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"读取剪贴板图片失败: {ex.Message}");
                    }
                }
            }
        }

        // ==================== 新增：取消传输事件 ====================
        private void BtnCancelTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (_isTransferring && _connectedClient != null)
            {
                var result = System.Windows.MessageBox.Show("确定要中断传输吗？这会导致当前连接断开。", "取消传输", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // 最干脆利落的中断方式：直接释放 Socket 连接。
                    // 这会触发双端的 ReceiveDataAsync 抛出异常，从而安全地重置状态。
                    _connectedClient.Close();
                }
            }
        }

        // ==================== 3. 发送文本消息 (完整版) ====================
        private void SendMessage()
        {
            if (_isTransferring)
            {
                System.Windows.MessageBox.Show("正在传输文件，请稍后发送消息。");
                return;
            }

            string message = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;
            if (!CheckConnection()) return;

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);

                // 组装文本协议：[Type(1字节，值为0)] + [Payload长度(4字节)] + [Payload实际字节]
                byte[] header = new byte[5];
                header[0] = 0; // 类型 0 代表文本
                BitConverter.GetBytes(payload.Length).CopyTo(header, 1);

                _netStream.Write(header, 0, header.Length);
                _netStream.Write(payload, 0, payload.Length);
                _netStream.Flush();

                LogMessage($"我: {message}");
                txtInput.Clear();
            }
            catch (Exception ex)
            {
                LogMessage($"发送失败: {ex.Message}");
            }
        }

        // ==================== 辅助方法 (完整版) ====================
        private string GetUniqueFilePath(string folderPath, string fileName)
        {
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string finalPath = Path.Combine(folderPath, fileName);
            int count = 1;
            while (File.Exists(finalPath))
            {
                string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                finalPath = Path.Combine(folderPath, $"{nameOnly}({count}){ext}");
                count++;
            }
            return finalPath;
        }

        private void UpdateProgressUI(bool isVisible, string text = "", double percent = 0)
        {
            panelProgress.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            txtProgress.Text = text;
            pbTransfer.Value = percent;
            // 禁用按钮防止交叉操作
            btnSend.IsEnabled = !isVisible;
            btnAddFile.IsEnabled = !isVisible;
        }

        // 新增全局变量：防止传文件时发消息导致字节流错乱
        private bool _isTransferring = false;

        // ==================== 1. 发送文件 (完整版) ====================
        // ==================== 修改：提取公共的文件发送核心方法 ====================
        private async void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                await SendFileCoreAsync(openFileDialog.FileName);
            }
        }
        // 这是提取出来的发送方法，供“点击+号”、“拖拽”、“粘贴”共同调用
        private async Task SendFileCoreAsync(string filePath)
        {
            if (!CheckConnection()) return;
            if (_isTransferring)
            {
                System.Windows.MessageBox.Show("当前正在传输文件，请稍后再发送。");
                return;
            }

            try
            {
                _isTransferring = true;
                UpdateProgressUI(true, "准备发送...");

                string fileName = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;

                byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);

                // 1. 发送头
                byte[] header = new byte[1 + 8 + 4 + nameBytes.Length];
                header[0] = 1;
                BitConverter.GetBytes(fileSize).CopyTo(header, 1);
                BitConverter.GetBytes(nameBytes.Length).CopyTo(header, 9);
                nameBytes.CopyTo(header, 13);
                await _netStream.WriteAsync(header, 0, header.Length);

                // 2. 流式发送
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[64 * 1024];
                    int readBytes;
                    long totalSent = 0;

                    while ((readBytes = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await _netStream.WriteAsync(buffer, 0, readBytes);
                        totalSent += readBytes;

                        int percent = (int)((double)totalSent / fileSize * 100);
                        UpdateProgressUI(true, $"发送中: {fileName} ({percent}%)", percent);
                    }
                }
                await _netStream.FlushAsync();
                LogMessage($"[文件发出]: {fileName}");
            }
            catch (Exception ex)
            {
                // 如果是因为我们主动 Close 断开引发的异常，不弹错误提示
                if (_connectedClient != null && _connectedClient.Connected)
                {
                    System.Windows.MessageBox.Show($"发送文件出错: {ex.Message}");
                }
            }
            finally
            {
                _isTransferring = false;
                UpdateProgressUI(false);
            }
        }
      

        // 核心发送方法：打包头和包体
        private void SendData(byte type, byte[] payload)
        {
            // 组装协议头
            byte[] header = new byte[5];
            header[0] = type;
            byte[] lengthBytes = BitConverter.GetBytes(payload.Length);
            Array.Copy(lengthBytes, 0, header, 1, 4);

            // 发送头和体
            _netStream.Write(header, 0, header.Length);
            _netStream.Write(payload, 0, payload.Length);
            _netStream.Flush();
        }

        // ==================== UI 辅助方法 ====================

        private bool CheckConnection()
        {
            if (!isServerRunning)
            {
                MessageBox.Show("请先开启服务！"); return false;
            }
            if (_connectedClient == null || !_connectedClient.Connected)
            {
                MessageBox.Show("当前没有手机连接过来！"); return false;
            }
            return true;
        }

        private void UpdateUIState(bool running)
        {
            btnToggleService.Content = running ? "停止服务" : "开启服务";
            btnToggleService.Background = new SolidColorBrush(running ? Colors.IndianRed : (Color)ColorConverter.ConvertFromString("#4CAF50"));
            txtPort.IsEnabled = !running;
            txtStatus.Text = running ? "状态: 监听中..." : "状态: 未开启";
            txtStatus.Foreground = new SolidColorBrush(running ? Colors.Green : Colors.Gray);
        }

        private void LogMessage(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                lstMessages.Items.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (lstMessages.Items.Count > 0)
                {
                    lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
                }
            });
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendMessage();
        private void TxtInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) SendMessage(); }
    }
}