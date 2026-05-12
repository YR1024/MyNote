using System.Windows;

namespace CodexApiProxy
{
    public partial class ProviderDialog : Window
    {
        public ApiProvider Provider { get; private set; }

        public ProviderDialog(ApiProvider provider = null)
        {
            InitializeComponent();
            if (provider != null)
            {
                Title = "编辑提供商";
                Provider = provider;
                txtName.Text = provider.Name;
                txtApiUrl.Text = provider.ApiUrl;
                txtApiKey.Password = provider.ApiKey;
                txtApiKeyPlain.Text = provider.ApiKey;
                txtModel.Text = provider.ModelName;
            }
            else
            {
                Title = "添加提供商";
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("请输入名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtApiUrl.Text))
            {
                MessageBox.Show("请输入 API 地址", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Provider == null)
                Provider = new ApiProvider();

            Provider.Name = txtName.Text.Trim();
            Provider.ApiUrl = txtApiUrl.Text.Trim();
            Provider.ApiKey = btnEye.IsChecked == true ? txtApiKeyPlain.Text : txtApiKey.Password;
            Provider.ModelName = txtModel.Text.Trim();

            DialogResult = true;
        }

        private void BtnEye_Click(object sender, RoutedEventArgs e)
        {
            if (btnEye.IsChecked == true)
            {
                txtApiKeyPlain.Text = txtApiKey.Password;
                txtApiKey.Visibility = Visibility.Collapsed;
                txtApiKeyPlain.Visibility = Visibility.Visible;
            }
            else
            {
                txtApiKey.Password = txtApiKeyPlain.Text;
                txtApiKeyPlain.Visibility = Visibility.Collapsed;
                txtApiKey.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
