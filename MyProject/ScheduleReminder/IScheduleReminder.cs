using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScheduleReminder
{
    public interface IScheduleReminder
    {
        void Start();

        void ShowWindow();
    }

    public class Schedule: IScheduleReminder
    {
        private Window Window;
        public void Start()
        {
            Window = new MainWindow();
        }

        public void ShowWindow()
        {
            if(Window == null)
            {
                Start();
            }
            Window.Show();
        }
    }
}
