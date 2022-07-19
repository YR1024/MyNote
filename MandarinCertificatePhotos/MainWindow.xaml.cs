using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MandarinCertificatePhotos
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(() =>
            {
                DownPhotos();
            });
        }

        string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        void DownPhotos()
        {
            String folderName = System.IO.Path.Combine(BaseDirectory, "Photos");
            if (!System.IO.Directory.Exists(folderName))
            {
                System.IO.Directory.CreateDirectory(folderName);
            }
            try
            {
                long stuID = 5007217100450;
                long maxstuID = 5007217100550;
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    for (long i = stuID; i < maxstuID; i++)
                    {
                        String source = String.Format("http://cq.cltt.org/Web/common/GeneratePhotoByStuID.ashx?StuID={0}", i);
                        webclient.DownloadFile(source, folderName + i + ".png");
                    }
                }
                MessageBox.Show("下载结束");
            }
            catch(Exception e)
            {
                MessageBox.Show("下载出错");
                throw e;
            }
        }
    }
}
