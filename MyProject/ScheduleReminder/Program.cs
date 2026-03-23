using System;
using System.Windows.Forms;


namespace ScheduleReminder
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //if (Properties.Settings.Default.StartUp)
            //{
            //    ProgramTool.StartUp();
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new APP());

        }
    }

    class APP: ApplicationContext
    {
        MainWindow mainWindow;
        public APP()
        {
            mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
   
}

