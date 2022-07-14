using R_Auto_Task.Helper;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace R_Auto_Task
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
            //TestTask();

            (sender as Button).Content = "停止";
            Thread.Sleep(2000);
            start = true;
            EnterTask().Start(); //开启挂机
            SendInfo("开启挂机");
            IsEnd = true; // 开启检测是否完成
            RestartTask().Start();
            SendInfo("开启检测是否完成");
        }

        double Similarity;
        void TestTask()
        {
            //Task.Run(new Action(() =>
            //{

            //    //string picda = @"C:\Users\YR\Desktop\111111.png";
            //    //string picx = @"C:\Users\YR\Desktop\总成绩.png";
            //    // EmguCvHelper.GetMatchPos(picda, picx);


            //    string pic = @"C:\Users\YR\Desktop\win.png";
            //    var rct = EmguCvHelper.GetMatchPos(pic);
            //    MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

            //    //Thread.Sleep(1000);
            //    //pic = @"C:\Users\YR\Desktop\qq.png";
            //    //rct = EmguCvHelper.GetMatchPos(pic);
            //    //MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

            //    Thread.Sleep(1000);
            //    pic = @"C:\Users\YR\Desktop\QQ2.png";
            //    rct = EmguCvHelper.GetMatchPos(pic);
            //    MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

            //    Thread.Sleep(3000);
            //    pic = @"C:\Users\YR\Desktop\qqhao.png";
            //    rct = EmguCvHelper.GetMatchPos(pic);
            //    MouseHelper.MouseDownUp(rct.X + rct.Width, rct.Y + rct.Height / 2);

            //    Thread.Sleep(1000);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D1);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D9); 
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D3);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D5); 
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D8); 
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D9); 
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D3); 
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D7);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D5);

            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Tab);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Y);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.R);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D1);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D8);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D7);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D2);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D3);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D7);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D5);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D0);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D0);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D4);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D1);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.OemPeriod);
            //    Thread.Sleep(50);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.OemPeriod);

            //    Thread.Sleep(50);
            //    Thread.Sleep(1000);
            //    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);

            //    //pic = @"C:\Users\YR\Desktop\login.png";
            //    //rct = EmguCvHelper.GetMatchPos(pic);
            //    //MouseHelper.MouseDownUp(rct.X + rct.Width, rct.Y + rct.Height / 2);
            //}));
        }


        #region GTATask

        #endregion
        void GTATask()
        {
            Task.Run(new Action(() =>
            {
                int i = 0;
             
                ////总成绩
                //while (i < 20)
                //{
                //    string pic = @"C:\Users\YR\Desktop\2.png";
                //    var rct = EmguCvHelper.GetMatchPos(pic, out Similarity);
                //    if (rct != System.Drawing.Rectangle.Empty)
                //    {
                //        WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);
                //        SendInfo("找到总成绩，按下回车");

                //        i = 0;
                //        break;
                //    }

                //    Thread.Sleep(1000);
                //    i++;
                //    SendInfo("i="+i);
                //    SendInfo("相似度："+ Similarity);

                //}

                SendInfo("start:"+start.ToString());
                SendInfo("IsEnd"+IsEnd.ToString());

                SendInfo("开始查找随机.png");
                //重玩
                while (i < 60)
                {
                    string pic = @"C:\Users\YR\Desktop\随机.png";
                    var rct = EmguCvHelper.GetMatchPos(pic, out Similarity);
                    if (rct != System.Drawing.Rectangle.Empty)
                    {
                        SendInfo("找到随机.png，点击");
                        WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.S);
                        SendInfo("按键S");
                        Thread.Sleep(500);
                        WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.S);
                        SendInfo("按键S");
                        Thread.Sleep(500);
                        WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);
                        SendInfo("回车");

                        //Thread.Sleep(100);
                        //MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

                        i = 0;
                        break;
                    }
                    Thread.Sleep(1000);
                    i++;
                    SendInfo("i=" + i);
                    SendInfo("未找到随机.png，相似度：" + Similarity);
                }

                SendInfo("等待进入房间.png");
                while (i < 60)
                {
                    string pic = @"C:\Users\YR\Desktop\房间.png";
                    var rct = EmguCvHelper.GetMatchPos(pic, out Similarity);
                    if (rct != System.Drawing.Rectangle.Empty)
                    {
                        SendInfo("找到房间.png，点击");
                        i = 0;
                        break;
                    }
                    Thread.Sleep(1000);
                    i++;
                    SendInfo("i=" + i);
                    SendInfo("未找到房间.png，相似度：" + Similarity);
                }

                Thread.Sleep(1000);
                SendInfo("右：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D);
                Thread.Sleep(500);
                SendInfo("上：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.W);
                Thread.Sleep(500);
                SendInfo("回车：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(6000);
                SendInfo("上：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.W);
                Thread.Sleep(500);
                SendInfo("回车：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);
                Thread.Sleep(1000);
                SendInfo("回车：");
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);


                start = true;
                EnterTask().Start(); //开启挂机
                SendInfo("开启挂机");
                IsEnd = true; // 开启检测是否完成
                RestartTask().Start();
                SendInfo("开启检测是否完成");

            }));
        }


        bool start = false;
        Task EnterTask()
        {
            return new Task(new Action(() =>
            {
                while (start)
                {
                    WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.Enter);
                    SendInfo("按下回车");
                    Thread.Sleep(1500);
                }
            }));
        }

        bool IsEnd = false;
        Task RestartTask()
        {
            return new Task(new Action(() =>
            {
                SendInfo("长时间挂机开始 检测到柱子任务等待10min, 次数：" + times++ );
                Thread.Sleep(10 * 60 * 1000);

                while (IsEnd)
                {
                    string pic = @"C:\Users\YR\Desktop\柱子.png";
                    var rct = EmguCvHelper.GetMatchPos(pic, out Similarity, 0.98);
                    if (rct != System.Drawing.Rectangle.Empty)
                    {
                        SendInfo("RestartTask（） ，检测到柱子.png");
                        SendInfo("相似度：" + Similarity);

                        IsEnd = false;
                        start = false; //结束EnterTask
                        GTATask(); //重新开始
                        break;
                    }
                    SendInfo("未检测到柱子.png,等待1500ms");
                    SendInfo("相似度：" + Similarity);

                    Thread.Sleep(1500);
                }
            }));
        }
        int times = 0;
        void SendInfo(string info)
        {
            this.Dispatcher.Invoke(new Action(()=> {
                if(InputTxt.Text.Length > 10_0000)
                {
                    write_txt(InputTxt.Text);
                    InputTxt.Text = "";
                }
                InputTxt.Text += "\n" + DateTime.Now +"----"+ info;
                InputTxt.ScrollToLine(InputTxt.LineCount - 1);
            }));
        }

        private static void write_txt( string content)
        {
            string FILE_NAME = @"C:\Users\YR\Desktop\"+ DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";//每天按照日期建立一个不同的文件名
            StreamWriter sr;
            if (File.Exists(FILE_NAME)) //如果文件存在,则创建File.AppendText对象
            {
                sr = File.AppendText(FILE_NAME);
            }
            else  //如果文件不存在,则创建File.CreateText对象
            {
                sr = File.CreateText(FILE_NAME);
            }

            sr.WriteLine(content);//将传入的字符串加上时间写入文本文件一行
            sr.Close();
        }

    }
}
