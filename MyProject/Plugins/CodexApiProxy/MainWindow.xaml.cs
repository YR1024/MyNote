using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using CodexApiProxy.Properties;
using Brushes = System.Windows.Media.Brushes;

namespace CodexApiProxy
{
    public partial class MainWindow : Window
    {
        private readonly ProxyServer _proxy;
        private readonly ProviderManager _providers;
        private NotifyIcon _trayIcon;
        private bool _forceExit;

        public MainWindow()
        {
            InitializeComponent();
            _proxy = new ProxyServer();
            _proxy.Log += OnProxyLog;
            _providers = new ProviderManager();
            _providers.Load();
            LoadSettings();
            RefreshProviderList();
            UpdateProxyUrl();
            InitTrayIcon();
        }

        private void InitTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("显示主窗口", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ForceExit());

            var icon = SystemIcons.Application;
            _trayIcon = new NotifyIcon
            {
                Icon = icon,
                Text = "Codex API Proxy",
                ContextMenuStrip = contextMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ForceExit()
        {
            _forceExit = true;
            _proxy.Stop();
            _proxy.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void LoadSettings()
        {
            txtPort.Text = Settings.Default.Port.ToString();
        }

        private void SaveSettings()
        {
            if (int.TryParse(txtPort.Text.Trim(), out int port))
                Settings.Default.Port = port;
            Settings.Default.Save();
        }

        private void RefreshProviderList()
        {
            cmbProviders.ItemsSource = null;
            cmbProviders.ItemsSource = _providers.Providers;

            var active = _providers.ActiveProvider;
            if (active != null)
                cmbProviders.SelectedItem = active;
        }

        private void UpdateProxyUrl()
        {
            var port = txtPort.Text.Trim();
            txtProxyUrl.Text = $"http://127.0.0.1:{port}/v1";
        }

        private ApiProvider GetSelectedProvider()
        {
            return cmbProviders.SelectedItem as ApiProvider;
        }

        private void CmbProviders_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var provider = GetSelectedProvider();
            if (provider != null)
            {
                _providers.SetActive(provider.Id);
            }
        }

        private void BtnAddProvider_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProviderDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _providers.Add(dlg.Provider);
                RefreshProviderList();
                cmbProviders.SelectedItem = dlg.Provider;
            }
        }

        private void BtnEditProvider_Click(object sender, RoutedEventArgs e)
        {
            var provider = GetSelectedProvider();
            if (provider == null)
            {
                System.Windows.MessageBox.Show("请先选择一个提供商", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dlg = new ProviderDialog(provider) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _providers.Update(dlg.Provider);
                RefreshProviderList();
            }
        }

        private void BtnRemoveProvider_Click(object sender, RoutedEventArgs e)
        {
            var provider = GetSelectedProvider();
            if (provider == null)
            {
                System.Windows.MessageBox.Show("请先选择一个提供商", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (System.Windows.MessageBox.Show($"确定删除 \"{provider.Name}\" 吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _providers.Remove(provider.Id);
                RefreshProviderList();
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var provider = GetSelectedProvider();
            if (provider == null)
            {
                System.Windows.MessageBox.Show("请先添加并选择一个 API 提供商", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                System.Windows.MessageBox.Show("当前提供商未设置 API Key，请编辑配置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                System.Windows.MessageBox.Show("端口范围: 1-65535", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettings();

            _proxy.ApiUrl = provider.ApiUrl;
            _proxy.ApiKey = provider.ApiKey;
            _proxy.Model = provider.ModelName;
            _proxy.Port = port;

            try
            {
                await _proxy.StartAsync();
                SetRunningState(true);
                _trayIcon.Text = $"Codex API Proxy - {provider.Name} (:{port})";
                AppendLog($"[信息] 使用提供商: {provider.Name} ({provider.ModelName})");
            }
            catch (Exception ex)
            {
                AppendLog($"[错误] 启动失败: {ex.Message}");
                System.Windows.MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _proxy.Stop();
            SetRunningState(false);
            _trayIcon.Text = "Codex API Proxy";
        }

        private void SetRunningState(bool running)
        {
            btnStart.IsEnabled = !running;
            btnStop.IsEnabled = running;
            cmbProviders.IsEnabled = !running;
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
            if (!_forceExit)
            {
                e.Cancel = true;
                Hide();
                //_trayIcon.ShowBalloonTip(2000, "Codex API Proxy", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            base.OnClosed(e);
        }
    }
}
