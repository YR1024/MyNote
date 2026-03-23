// MainWindow.xaml.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScheduleReminder
{
    public partial class MainWindow : Window
    {

        public ObservableCollection<Alarm> Alarms { get; set; } = new ObservableCollection<Alarm>();

        public MainWindow()
        {
            InitializeComponent();
            InitBaseData();
            StartAllAlarms();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        //初始化基础数据
        void InitBaseData()
        {
            HourComboBox.ItemsSource = new ObservableCollection<int>(Enumerable.Range(0, 24));
            HourComboBox.SelectedIndex = 13;
            MinuteComboBox.ItemsSource = new ObservableCollection<int>(Enumerable.Range(0, 60));
            MinuteComboBox.SelectedIndex = 30;
            AlarmList.ItemsSource = Alarms = new ObservableCollection<Alarm>(Properties.Settings.Default.Alarms);
            SoundPath.Text = @"C:\Users\YR\Desktop\test.mp3";
        }

        void StartAllAlarms()
        {
            foreach (var alarm in Alarms)
            {
                var alarmClock = new AlarmClock(alarm);
                alarmClock.AlarmClockTriggered += AlarmClock_AlarmClockTriggered;
                alarmClock.Start();
            }
        }

        /// <summary>
        /// 闹钟触发后回调
        /// </summary>
        /// <param name="sender"></param>
        public void AlarmClock_AlarmClockTriggered(object sender)
        {
            var alarm = sender as Alarm;
            if (alarm != null && alarm.RepeatType == RepeatType.OnlyOnce)
            {
                // 仅一次的闹钟 触发后 删除它
                this.Dispatcher.Invoke(() => {
                    Alarms.Remove(alarm);
                    SaveAlarms();
                });
            }
        }


        //保存闹钟
        void SaveAlarms()
        {
            Properties.Settings.Default.Alarms = Alarms.ToList();
            Properties.Settings.Default.Save();
        }

        //重复类型 点击
        private void RepeatType_Click(object sender, RoutedEventArgs e)
        {
            if(sender is CheckBox)
            {
                OnlyOnce.IsChecked = false;
                EveryDay.IsChecked = false;
                WorkDays.IsChecked = false;
                Holidays.IsChecked = false;
            }
            else if(sender is RadioButton)
            {
                foreach (CheckBox item in WeekBox.Children)
                {
                    item.IsChecked = false;
                }
            }
        }

        //添加
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Alarm alarm = new Alarm();
            alarm.Time = new TimeSpan(HourComboBox.SelectedIndex, MinuteComboBox.SelectedIndex, 0);
            RepeatType repeatType = RepeatType.OnlyOnce;
            if (OnlyOnce.IsChecked == true)
                repeatType = RepeatType.OnlyOnce;
            else if (EveryDay.IsChecked == true)
                repeatType = RepeatType.EveryDay;
            else if (WorkDays.IsChecked == true)
                repeatType = RepeatType.WorkDays;
            else if (Holidays.IsChecked == true)
                repeatType = RepeatType.Holidays;
            else if (WeekBox.Children.OfType<CheckBox>().Any(c => c.IsChecked == true))
            {
                repeatType = RepeatType.Custom;
                alarm.DayOfWeek = GetSelectedDaysOfWeek().ToArray();
            }
            alarm.RepeatType = repeatType;
            alarm.Ringtone = SoundPath.Text;
            
            Alarms.Add(alarm);
            SaveAlarms();

            // 创建并启动闹钟
            var alarmClock = new AlarmClock(alarm);
            alarmClock.AlarmClockTriggered += AlarmClock_AlarmClockTriggered;
            alarmClock.Start();
        }

        //获取勾选的星期
        List<int> GetSelectedDaysOfWeek()
        {
            List<int> daysOfWeek = new List<int>();
            foreach (CheckBox item in WeekBox.Children)
            {
                if (item.IsChecked == true)
                {
                    if (int.TryParse(item.Tag.ToString(), out int value))
                    {
                        daysOfWeek.Add(value);
                    }
                }
            }
            return daysOfWeek;
        }

        //删除
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Alarms.Remove(AlarmList.SelectedItem as Alarm);
            SaveAlarms();
        }

        //选择铃声
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "音频文件|*.mp3;*.wav;*.wma;*.aac;*.flac;*.alac;*.aiff;*.au;*.m4a;*.ogg;*.oga;*.opus;*.tta;*.mp4;*.m4a;*.m4r;*.3gp;*.3g2;*.amr;*.awb;*.wmv;*.webm;*.mkv;*.mov;*.mpg;*.mpeg;*.vob;*.flv;*.swf;*.avi;*.wmv;*.rm;*.rmvb;*.asf;*.ogv;*.mng;*.qt;*.mpe;*.mpv;*.m2v;*.m4v;*.svi;*.3gp2;*.3gpp;*.mxf;*.roq;*.nsv;*.flv;*.";
            if (fileDialog.ShowDialog() == true)
            {
                SoundPath.Text = fileDialog.FileName;
            }
            
        }

        MediaPlayer MediaPlayer;
        private void TestPlay_Click(object sender, RoutedEventArgs e)
        {
            if((sender as Button).Content.ToString() == "播放")
            {
                if (string.IsNullOrEmpty(SoundPath.Text))
                    return;
                if (!System.IO.File.Exists(SoundPath.Text))
                    return;
                //播放音频文件
                MediaPlayer = new MediaPlayer();
                MediaPlayer.MediaOpened += (s, args) =>
                {
                    MediaPlayer.Play();
                };
                MediaPlayer.MediaFailed += (s, args) =>
                {
                    MessageBox.Show("音频加载失败: " + args.ErrorException?.Message);
                };
                MediaPlayer.Open(new Uri(SoundPath.Text));
                (sender as Button).Content = "停止";
            }
            else if ((sender as Button).Content.ToString() == "停止")
            {
                MediaPlayer?.Stop();
                MediaPlayer = null;
                (sender as Button).Content = "播放";
            }
        }
    }

    /// <summary>
    /// 定时任务
    /// </summary>
    public class AlarmClock
    {
        private Timer timer;
        private Alarm alarm;
        private bool isRunning;

        public AlarmClock(Alarm alarm)
        {
            this.alarm = alarm;
            this.isRunning = false;
        }

        public void Start()
        {
            if (isRunning)
                return;

            CalculateNextTriggerTime();
            isRunning = true;
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            timer?.Dispose();
            isRunning = false;
        }

        private void CalculateNextTriggerTime()
        {
            DateTime now = DateTime.Now;
            DateTime nextAlarmTime = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                alarm.Time.Hours,
                alarm.Time.Minutes,
                0);

            // 如果今天的闹钟时间已过，则设置为明天
            if (nextAlarmTime <= now)
            {
                nextAlarmTime = nextAlarmTime.AddDays(1);
            }

            // 根据重复类型调整闹钟触发时间
            switch (alarm.RepeatType)
            {
                case RepeatType.OnlyOnce:
                    // 仅一次，不需要特殊处理
                    break;

                case RepeatType.EveryDay:
                    // 每天，不需要特殊处理
                    break;

                case RepeatType.WorkDays:
                    // 工作日（周一到周五）
                    while (nextAlarmTime.DayOfWeek == DayOfWeek.Saturday ||
                           nextAlarmTime.DayOfWeek == DayOfWeek.Sunday)
                    {
                        nextAlarmTime = nextAlarmTime.AddDays(1);
                    }
                    break;

                case RepeatType.Holidays:
                    // 节假日（周六和周日）
                    while (nextAlarmTime.DayOfWeek != DayOfWeek.Saturday &&
                           nextAlarmTime.DayOfWeek != DayOfWeek.Sunday)
                    {
                        nextAlarmTime = nextAlarmTime.AddDays(1);
                    }
                    break;

                case RepeatType.Custom:
                    // 自定义重复日期
                    int currentDayOfWeek = (int)nextAlarmTime.DayOfWeek;
                    if (currentDayOfWeek == 0) // 周日
                        currentDayOfWeek = 7;

                    if (!alarm.DayOfWeek.Contains(currentDayOfWeek))
                    {
                        // 找到下一个自定义日期
                        DateTime nextCustomDay = nextAlarmTime.AddDays(1);
                        while (true)
                        {
                            int nextDayOfWeek = (int)nextCustomDay.DayOfWeek;
                            if (nextDayOfWeek == 0) // 周日
                                nextDayOfWeek = 7;

                            if (alarm.DayOfWeek.Contains(nextDayOfWeek))
                            {
                                nextAlarmTime = nextCustomDay;
                                break;
                            }
                            nextCustomDay = nextCustomDay.AddDays(1);
                        }
                    }
                    break;
            }

            // 计算触发间隔
            TimeSpan timeToTrigger = nextAlarmTime - now;
            timer = new Timer(AlarmCallback, null, (int)timeToTrigger.TotalMilliseconds, Timeout.Infinite);
        }

        //计时结束触发事件
        private void AlarmCallback(object state)
        {
            //Console.WriteLine($"闹钟触发！时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Task.Run(() => {
              
                //MessageBox.Show("叮！叮！叮！叮！叮！叮！叮！叮！叮！叮！叮！叮！叮！");
                Thread notificationThread = new Thread(() =>
                {

                    MediaPlayer player = null;
                    if (!string.IsNullOrEmpty(alarm.Ringtone) && System.IO.File.Exists(alarm.Ringtone))
                    {
                        //播放音频文件
                        player = new MediaPlayer();
                        player.Volume = 1.0;
                        player.MediaOpened += (s, args) =>
                        {
                            player.Play();
                        };
                        player.MediaFailed += (s, args) =>
                        {
                            MessageBox.Show("音频加载失败: " + args.ErrorException?.Message);
                        };
                        player.Open(new Uri(alarm.Ringtone));
                    }

                    NotifyWnd notifyWindow = NotifyWnd.NewInstance;
                    notifyWindow.Message = "叮！叮！叮！叮！叮！";
                    notifyWindow.Show();
                    notifyWindow.Activate();
                    notifyWindow.Closed += (s, args) =>
                    {
                        player?.Stop();
                        player = null;
                    };
                    System.Windows.Threading.Dispatcher.Run();
                });

                notificationThread.SetApartmentState(ApartmentState.STA);
                notificationThread.IsBackground = true;
                notificationThread.Start();
             
            });
            
            AlarmClockTriggered?.Invoke(this.alarm);

            // 根据重复类型决定是否继续
            if (alarm.RepeatType == RepeatType.OnlyOnce)
            {
                Stop();
            }
            else
            {
                // 重新计算下一次触发时间
                CalculateNextTriggerTime();
            }
        }


        public Action<object> AlarmClockTriggered { get; set; }

    }





    public enum RepeatType 
    {
        OnlyOnce,
        EveryDay,
        WorkDays,
        Holidays,
        Custom,
    }



    public class Alarm
    {
        /// <summary>
        /// 时间
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        /// 重复类型
        /// </summary>
        public RepeatType RepeatType { get; set; }

        /// <summary>
        /// 星期
        /// </summary>
        public int[] DayOfWeek { get; set; } = new int[0];

        public string Repeat
        {
            get
            {
                switch (RepeatType)
                {
                    case RepeatType.OnlyOnce:
                        return "仅一次";
                    case RepeatType.EveryDay:
                        return "每天";
                    case RepeatType.WorkDays:
                        return "工作日";
                    case RepeatType.Holidays:
                        return "节假日";
                    case RepeatType.Custom:
                        string daysOfWeek = string.Join(",", DayOfWeek.Select(d => dayNames[d]));
                        return $"{daysOfWeek}";
                    default:
                        return "";
                }
            }
        }
        public static Dictionary<int, string> dayNames = new Dictionary<int, string>
        {
            { 1, "周一" },
            { 2, "周二" },
            { 3, "周三" },
            { 4, "周四" },
            { 5, "周五" },
            { 6, "周六" },
            { 7, "周日" },
            //{ System.DayOfWeek.Sunday, "周日" }
        };

        /// <summary>
        /// 铃声路径
        /// </summary>
        public string Ringtone { get; set; } = "";
    

        public override string ToString()
        {
            return $"{Time.ToString(@"hh\:mm")}         {Repeat}";
        }
    }

    public class DayOfWeekItem : INotifyPropertyChanged
    {
        public DayOfWeek Day { get; set; }
        public string Name { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DayOfWeekItem Clone()
        {
            return new DayOfWeekItem
            {
                Day = Day,
                Name = Name,
                IsSelected = IsSelected
            };
        }
    }
}