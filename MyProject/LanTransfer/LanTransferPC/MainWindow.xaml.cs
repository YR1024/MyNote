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

        private async Task ReceiveDataAsync(CancellationToken token)
        {
            try
            {
                byte[] headerBuffer = new byte[5]; // 1字节类型 + 4字节长度

                while (!token.IsCancellationRequested && _connectedClient.Connected)
                {
                    // 1. 读取包头 (5个字节)
                    int bytesRead = await ReadExactBytesAsync(_netStream, headerBuffer, 5, token);
                    if (bytesRead == 0) break; // 客户端断开

                    byte dataType = headerBuffer[0];
                    int dataLength = BitConverter.ToInt32(headerBuffer, 1);

                    // 2. 读取包体 (实际数据)
                    byte[] payloadBuffer = new byte[dataLength];
                    await ReadExactBytesAsync(_netStream, payloadBuffer, dataLength, token);

                    // 3. 解析数据
                    if (dataType == 0)
                    {
                        string message = Encoding.UTF8.GetString(payloadBuffer);
                        LogMessage($"手机端: {message}");
                    }
                    else if (dataType == 1) // 收到文件
                    {
                        // 1. 提取文件名长度 (前4个字节)
                        int nameLength = BitConverter.ToInt32(payloadBuffer, 0);
                        // 2. 提取文件名
                        string fileName = Encoding.UTF8.GetString(payloadBuffer, 4, nameLength);
                        // 3. 提取真实的文件数据
                        int fileDataLength = payloadBuffer.Length - 4 - nameLength;
                        byte[] fileData = new byte[fileDataLength];
                        Array.Copy(payloadBuffer, 4 + nameLength, fileData, 0, fileDataLength);

                        // 4. 保存到用户设置的真实路径
                        string savePath = string.Empty;
                        Dispatcher.Invoke(() => savePath = txtSavePath.Text); // 从 UI 获取路径

                        // 防止文件名冲突，如果有重名文件则在末尾加个数字
                        string finalFilePath = Path.Combine(savePath, fileName);
                        int count = 1;
                        while (File.Exists(finalFilePath))
                        {
                            string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                            string ext = Path.GetExtension(fileName);
                            finalFilePath = Path.Combine(savePath, $"{nameOnly}({count}){ext}");
                            count++;
                        }

                        File.WriteAllBytes(finalFilePath, fileData);
                        LogMessage($"[收到文件]: {Path.GetFileName(finalFilePath)}");
                    }
                }
            }
            catch (Exception)
            {
                LogMessage("系统提示: 手机端已断开连接。");
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

        // ==================== 发送数据逻辑 ====================

        private void SendMessage()
        {
            string message = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (!CheckConnection()) return;

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);
                SendData(0, payload); // 发送类型 0 (文本)
                LogMessage($"我: {message}");
                txtInput.Clear();
            }
            catch (Exception ex)
            {
                LogMessage($"发送失败: {ex.Message}");
            }
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckConnection()) return;

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileData = File.ReadAllBytes(filePath);

                    // 1. 将文件名转为 UTF8 字节
                    byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                    // 2. 组装 Payload: [文件名长度(4字节)] + [文件名字节] + [文件内容字节]
                    byte[] payload = new byte[4 + nameBytes.Length + fileData.Length];

                    BitConverter.GetBytes(nameBytes.Length).CopyTo(payload, 0); // 写入文件名长度
                    nameBytes.CopyTo(payload, 4); // 写入文件名
                    fileData.CopyTo(payload, 4 + nameBytes.Length); // 写入文件数据

                    SendData(1, payload); // 发送类型 1 (文件)
                    LogMessage($"[文件发出]: {fileName}");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"发送文件出错: {ex.Message}");
                }
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