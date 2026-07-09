using Android.Content;
using Android.Media.Projection;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace SoundWord;

[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true)]
public class MainActivity : Activity
{
    private const int MediaProjectionRequestCode = 1001;
    private const int NotificationPermissionRequestCode = 1002;
    private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";

    private RadioGroup? _languageGroup;
    private TextView? _statusView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        BuildUi();
        RequestNotificationPermissionIfNeeded();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode != MediaProjectionRequestCode)
        {
            return;
        }

        if (resultCode != Result.Ok || data is null)
        {
            SetStatus("屏幕/音频捕获授权被取消。");
            return;
        }

        MediaProjectionPermissionStore.ResultCode = (int)resultCode;
        MediaProjectionPermissionStore.Data = data;

        var intent = new Intent(this, typeof(CaptureTranslationService));
        intent.SetAction(CaptureTranslationService.ActionStart);
        intent.PutExtra(CaptureTranslationService.ExtraSourceLanguage, GetSelectedLanguageCode());

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            StartForegroundService(intent);
        }
        else
        {
            StartService(intent);
        }

        SetStatus("已启动。系统会显示一个悬浮中文字幕窗口。");
    }

    private void BuildUi()
    {
        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(Dp(20), Dp(24), Dp(20), Dp(20));
        root.SetBackgroundColor(Android.Graphics.Color.Rgb(248, 250, 252));

        var title = new TextView(this)
        {
            Text = "SoundWord",
            TextSize = 28,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        title.SetTextColor(Android.Graphics.Color.Rgb(15, 23, 42));
        root.AddView(title, MatchWrap());

        var subtitle = new TextView(this)
        {
            Text = "系统声音实时翻译成中文字幕",
            TextSize = 15
        };
        subtitle.SetTextColor(Android.Graphics.Color.Rgb(71, 85, 105));
        subtitle.SetPadding(0, Dp(4), 0, Dp(20));
        root.AddView(subtitle, MatchWrap());

        var languageLabel = new TextView(this)
        {
            Text = "输入语言",
            TextSize = 16,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        languageLabel.SetTextColor(Android.Graphics.Color.Rgb(30, 41, 59));
        root.AddView(languageLabel, MatchWrap());

        _languageGroup = new RadioGroup(this)
        {
            Orientation = Orientation.Horizontal
        };

        var japanese = new RadioButton(this)
        {
            Id = View.GenerateViewId(),
            Text = "日语",
            TextSize = 16,
            Checked = true
        };
        japanese.Tag = "ja-JP";

        var english = new RadioButton(this)
        {
            Id = View.GenerateViewId(),
            Text = "英语",
            TextSize = 16
        };
        english.Tag = "en-US";

        _languageGroup.AddView(japanese, WrapWrap());
        _languageGroup.AddView(english, WrapWrap());
        root.AddView(_languageGroup, MatchWrap());

        var startButton = new Button(this)
        {
            Text = "启动实时字幕"
        };
        startButton.Click += (_, _) => StartCaptureFlow();
        root.AddView(startButton, MatchWrap(topMargin: Dp(20)));

        var stopButton = new Button(this)
        {
            Text = "停止"
        };
        stopButton.Click += (_, _) => StopCaptureService();
        root.AddView(stopButton, MatchWrap(topMargin: Dp(8)));

        var unlockButton = new Button(this)
        {
            Text = "解锁悬浮窗"
        };
        unlockButton.Click += (_, _) => UnlockOverlay();
        root.AddView(unlockButton, MatchWrap(topMargin: Dp(8)));

        _statusView = new TextView(this)
        {
            Text = "先授予悬浮窗和屏幕捕获权限，然后播放英语/日语音频测试。",
            TextSize = 14
        };
        _statusView.SetTextColor(Android.Graphics.Color.Rgb(71, 85, 105));
        _statusView.SetPadding(0, Dp(20), 0, 0);
        root.AddView(_statusView, MatchWrap());

        SetContentView(root);
    }

    private void StartCaptureFlow()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
        {
            SetStatus("系统音频捕获需要 Android 10 或更高版本。");
            return;
        }

        if (!Settings.CanDrawOverlays(this))
        {
            SetStatus("请先打开“允许显示在其他应用上层”，回到本应用后再次点启动。");
            var overlayIntent = new Intent(
                Settings.ActionManageOverlayPermission,
                Android.Net.Uri.Parse("package:" + PackageName));
            StartActivity(overlayIntent);
            return;
        }

        RequestNotificationPermissionIfNeeded();

        var projectionManager = (MediaProjectionManager?)GetSystemService(MediaProjectionService);
        if (projectionManager is null)
        {
            SetStatus("无法获取 MediaProjection 服务。");
            return;
        }

        StartActivityForResult(projectionManager.CreateScreenCaptureIntent(), MediaProjectionRequestCode);
    }

    private void StopCaptureService()
    {
        var intent = new Intent(this, typeof(CaptureTranslationService));
        intent.SetAction(CaptureTranslationService.ActionStop);
        StartService(intent);
        SetStatus("已请求停止。");
    }

    private void UnlockOverlay()
    {
        var intent = new Intent(this, typeof(CaptureTranslationService));
        intent.SetAction(CaptureTranslationService.ActionUnlockOverlay);
        StartService(intent);
        SetStatus("已请求解锁悬浮窗。");
    }

    private string GetSelectedLanguageCode()
    {
        if (_languageGroup is null)
        {
            return "ja-JP";
        }

        var selected = FindViewById<RadioButton>(_languageGroup.CheckedRadioButtonId);
        return selected?.Tag?.ToString() ?? "ja-JP";
    }

    private void RequestNotificationPermissionIfNeeded()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
            CheckSelfPermission(PostNotificationsPermission) != Android.Content.PM.Permission.Granted)
        {
            RequestPermissions(
                [PostNotificationsPermission],
                NotificationPermissionRequestCode);
        }
    }

    private void SetStatus(string message)
    {
        if (_statusView is not null)
        {
            _statusView.Text = message;
        }

        Toast.MakeText(this, message, ToastLength.Short)?.Show();
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);

    private static LinearLayout.LayoutParams MatchWrap(int topMargin = 0)
    {
        var layout = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        layout.TopMargin = topMargin;
        return layout;
    }

    private static LinearLayout.LayoutParams WrapWrap()
    {
        return new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent);
    }
}
