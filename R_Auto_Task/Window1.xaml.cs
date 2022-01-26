using R_Auto_Task.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;

namespace R_Auto_Task
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public MainViewModel ViewModel;
        public Window1()
        {
            InitializeComponent();
            ViewModel = this.DataContext as MainViewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OperationList.Add(new Operation());
        }

        private void SetImageFile_Click(object sender, RoutedEventArgs e)
        {
            var Operation = (sender as Button).Tag as Operation;
            //创建一个保存文件的对话框
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()
            {
                //Filter = "All Files|*.config*",
                //InitialDirectory = @"D:\",
            };
            //调用ShowDialog()方法显示该对话框，该方法的返回值代表用户是否点击了确定按钮
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                string FileName = dialog.FileName;
                Operation.ImageUrl = FileName;
                var bitmapImage = new Bitmap(FileName, true);
                Operation.ImgSource = ImageHelper.BitmapToImageSource(bitmapImage);
            }
        }

        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (gridControl.SelectedItem == null)
                return;
            ViewModel.OperationList.Remove(gridControl.SelectedItem as Operation);
            gridControl.SelectedItem = null;
        }
    }
}
