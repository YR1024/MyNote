using System;
using System.Windows;
using System.Windows.Media;
using CodexApiProxy.Properties;

namespace CodexApiProxy
{
    public partial class MainWindow : Window
    {
        private readonly ProxyServer _proxy;

        public MainWindow()
        {
            InitializeComponent();
            _proxy = new ProxyServer();
            _proxy.Log += OnProxyLog;
            LoadSettings();
            UpdateProxyUrl();
        }

        private void LoadSettings()
        {
            txtApiUrl.Text = Settings.Default.ApiUrl;
            txtApiKey.Password = Settings.Default.ApiKey;
            txtModel.Text = Settings.Default.ModelName;
            txtPort.Text = Settings.Default.Port.ToString();
        }

        private void SaveSettings()
        {
            Settings.Default.ApiUrl = txtApiUrl.Text.Trim();
            Settings.Default.ApiKey = txtApiKey.Password;
            Settings.Default.ModelName = txtModel.Text.Trim();
            if (int.TryParse(txtPort.Text.Trim(), out int port))
                Settings.Default.Port = port;
            Settings.Default.Save();
        }

        private void UpdateProxyUrl()
        {
            var port = txtPort.Text.Trim();
            txtProxyUrl.Text = $"http://127.0.0.1:{port}/v1";
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiKey.Password))
            {
                MessageBox.Show("请先填写 API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("端口范围: 1-65535", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettings();

            _proxy.ApiUrl = txtApiUrl.Text.Trim();
            _proxy.ApiKey = txtApiKey.Password;
            _proxy.Model = txtModel.Text.Trim();
            _proxy.Port = port;

            try
            {
                await _proxy.StartAsync();
                SetRunningState(true);
            }
            catch (Exception ex)
            {
                AppendLog($"[错误] 启动失败: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _proxy.Stop();
            SetRunningState(false);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            UpdateProxyUrl();
            AppendLog("[信息] 配置已保存");
        }

        private void SetRunningState(bool running)
        {
            btnStart.IsEnabled = !running;
            btnStop.IsEnabled = running;
            txtApiUrl.IsEnabled = !running;
            txtApiKey.IsEnabled = !running;
            txtModel.IsEnabled = !running;
            txtPort.IsEnabled = !running;
            txtStatus.Text = running ? "运行中" : "已停止";
            txtStatus.Foreground = running ? Brushes.Green : Brushes.Gray;
        }

        private void OnProxyLog(object sender, LogEventArgs e)
        {
            Dispatcher.Invoke(() => AppendLog(e.Message));
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _proxy.Stop();
            _proxy.Dispose();
        }
    }
}
