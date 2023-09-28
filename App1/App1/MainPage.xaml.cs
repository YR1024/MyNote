using Android;
using Android.App;
using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace App1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            tclient = new TcpClient();
            HeartbeatCheckTask().Start();
            //ConnectTask().Start();
            ReceiveServerMessage().Start();

            LoadConfig();
        }


        void LoadConfig()
        {
            // 检索数据
            ipEditor.Text = Preferences.Get("IPValue", string.Empty);
            portEditor.Text = Preferences.Get("PortValue", string.Empty);
        }

        /// <summary>
        /// 获取手机短信
        /// </summary>
        public async void ReadSmsButton_Clicked(object sender, EventArgs e)
        {
            var smsService = DependencyService.Get<ISmsService>();
            if (smsService != null)
            {
                //string latestSms = await smsService.ReadLatestSmsAsync();
                string latestSms = await smsService.ReadLatestSmsAsync(new DateTime(2023, 9, 1), "95533");
                if (!string.IsNullOrEmpty(latestSms))
                {
                    // 处理最新的短信内容
                    // 将最新的短信内容显示在界面上或者执行其他操作
                }
                else
                {
                    // 没有找到短信
                }
            }
        }

        public async static Task<string> GetSMSTest()
        {
            var smsService = DependencyService.Get<ISmsService>();
            if (smsService != null)
            {
                //string latestSms = await smsService.ReadLatestSmsAsync();
                string latestSms = await smsService.ReadLatestSmsAsync(new DateTime(2023, 9, 1), "95533");
                if (!string.IsNullOrEmpty(latestSms))
                {
                    // 处理最新的短信内容
                    // 将最新的短信内容显示在界面上或者执行其他操作
                    return latestSms;
                }
                else
                {
                    // 没有找到短信
                    return "";
                }
            }
            return "";
        }


        public TcpClient tclient;
        bool TcpClientConnected = false;

        /// <summary>
        /// 心跳检测
        /// </summary>
        /// <returns></returns>
        Task HeartbeatCheckTask()
        {
            Task t = new Task(() =>
            {
                while (true)
                {
                    if (tclient.Connected != TcpClientConnected)
                    {
                        TcpClientConnected = tclient.Connected;
                        ShowCurrentConnectionStatus(TcpClientConnected ? ConnectionStatus.Connected : ConnectionStatus.UnConnect);
                    }
                    Thread.Sleep(50);
                }
            });
            return t;
        }


        /// <summary>
        /// 连接任务
        /// </summary>
        /// <returns></returns>
        Task ConnectTask()
        {
            Task t = new Task(() =>
            {
                try
                {
                    ShowCurrentConnectionStatus(ConnectionStatus.Connecting);
                    Connect();
                }
                catch (Exception e)
                {

                }
            });
            return t;
        }

        /// <summary>
        /// 连接到服务
        /// </summary>
        void Connect()
        {
            try
            {
                tclient.Connect(ipEditor.Text, Convert.ToInt32(portEditor.Text));
                ShowCurrentConnectionStatus(ConnectionStatus.Connected);
            }
            catch (Exception ex)
            {
                ShowCurrentConnectionStatus(0);
                //DisplayAlert("提示", "连接失败", "确定");
            }
        }

        /// <summary>
        /// 连接
        /// </summary>
        private void Connect_Clicked(object sender, EventArgs e)
        {
            ConnectTask().Start();
        }



        private void SendToService_Clicked(object sender, EventArgs e)
        {
            if (TcpClientConnected)
            {
                NetworkStream ns = tclient.GetStream();
                byte[] data = Encoding.Unicode.GetBytes(new Random().Next(0, 100).ToString());
                ns.Write(data, 0, data.Length);
            }
        }


        /// <summary>
        /// 接收服务段消息
        /// </summary>
        /// <returns></returns>
        Task ReceiveServerMessage()
        {
            Task t = new Task(() =>
            {
                while (true)
                {
                    if (tclient.Available > 0)
                    {
                        NetworkStream ns = tclient.GetStream();
                        byte[] data = new byte[1024];
                        ns.Read(data, 0, 1024);
                        String content = Encoding.Unicode.GetString(data);
                    }
                    Thread.Sleep(50);
                }
            });
            return t;
        }


        /// <summary>
        /// 设置当前连接状态
        /// </summary>
        /// <param name="s">0未连接，1正在连接，2已连接 </param>
        void ShowCurrentConnectionStatus(ConnectionStatus status)
        {
            this.Dispatcher.BeginInvokeOnMainThread(new Action(() =>
            {
                if (status == ConnectionStatus.UnConnect)
                {
                    TipsTxt.Text = "未连接";
                }
                else if (status == ConnectionStatus.Connecting)
                {
                    TipsTxt.Text = "正在连接";
                }
                else if (status == ConnectionStatus.Connected)
                {
                    TipsTxt.Text = "已连接";
                }

            }));
        }

        private void Save_Clicked(object sender, EventArgs e)
        {
            // 保存数据
            Preferences.Set("IPValue", ipEditor.Text);
            Preferences.Set("PortValue", portEditor.Text);
        }


    }

    public enum ConnectionStatus
    {
        UnConnect,

        Connecting,

        Connected
    }
}
