using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;

namespace App1.Droid
{
    [Activity(Label = "App1", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        App app;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            app = new App();
            LoadApplication(app);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnStart()
        {
            base.OnStart();
            Intent serviceToStart = new Intent(this, typeof(MyServer));
            StartServer(this, serviceToStart);

        }
        public static void StartServer(ContextWrapper context, Intent intent)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

    }

    [Service]  //此处Service不可写成Service(IsolatedProcess=true)，否则普通调用服务方法不起作用
    public sealed class MyServer : Service
    {


        public override void OnCreate()
        {
            base.OnCreate();

            //此处在第一次调用服务，即服务创建时触发，第二次及以后再调用不会再触发
            const int NOTIFICATION_ID = 345;

            const string CHANNEL_ID = "myID224";

            const string CHANNEL_NAME = "服务器通知";

            CreateNotificationChannel(this, CHANNEL_ID, CHANNEL_NAME);

            var notification = CreateServerNotification("Title", "Content", this, CHANNEL_ID);

            StartForeground(NOTIFICATION_ID, notification);
        }

        public static void CreateNotificationChannel(Context context, string channelID, string channelName)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                ((NotificationManager)context.GetSystemService(Context.NotificationService))
                            .CreateNotificationChannel(new NotificationChannel(channelID, channelName, NotificationImportance.Default));
            }
        }
        public static Notification CreateServerNotification(string contentTitle, string contentText, Context context, string channelID)
        {
            return new AndroidX.Core.App.NotificationCompat.Builder(context, channelID)
                               .SetContentTitle(contentTitle)
                               .SetContentText(contentText)
                               .SetSmallIcon(Resource.Mipmap.icon)
                               .SetOngoing(true)
                               .Build();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            //此处可添加自己想要通过前台服务做的事情，比如后台定位功能，每次调用服务都会触发
            //startlocation()
            //app
            var a = App1.App.GetSMSTest().Result;
            return StartCommandResult.RedeliverIntent;  //此返回值可以在服务被终止时尝试重启服务，并可传回当时的Intent
        }



        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }
}