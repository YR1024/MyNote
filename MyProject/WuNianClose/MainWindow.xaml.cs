using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace WuNianClose
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


		private void Button_Click(object sender, RoutedEventArgs e)
        {

			(sender as Button).IsEnabled = false;
			int.TryParse(hour.Text, out int h);
			int.TryParse(minute.Text, out int m);

			var closeTime = DateTime.Now.Date.AddHours(h).AddMinutes(m);
			if(DateTime.Now >= closeTime)
            {
                closeTime = closeTime.AddDays(1);
			}

			Task.Run(delegate
			{
				try
				{
					while (true)
					{
						if (DateTime.Now > closeTime)
						{
							Process[] qqSpeedProcesses = Process.GetProcessesByName("GameApp");
							Process[] allProcesses = Process.GetProcesses();
							if (qqSpeedProcesses.Length >= 1)
							{
								foreach (var p in qqSpeedProcesses)
                                {
									p.Kill();
                                }
								var wunianProc = allProcesses.FirstOrDefault(p => p.ProcessName.Contains("勿念"));

                                if (wunianProc != null)
								{
                                    wunianProc.Kill();
								}
								Process.GetCurrentProcess().Kill();
							}
						}
						else
						{
							Thread.Sleep(5000);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			});
		}
    }
}
