using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Media.Projection;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SoundWord;

[Service(Name = "com.companyname.SoundWord.CaptureTranslationService", Exported = false)]
public class CaptureTranslationService : Service
{
    private const string LogTag = "SoundWord";
    public const string ActionStart = "com.companyname.SoundWord.action.START";
    public const string ActionStop = "com.companyname.SoundWord.action.STOP";
    public const string ActionUnlockOverlay = "com.companyname.SoundWord.action.UNLOCK_OVERLAY";
    public const string ActionToggleOverlayLock = "com.companyname.SoundWord.action.TOGGLE_OVERLAY_LOCK";
    public const string ActionFontUp = "com.companyname.SoundWord.action.FONT_UP";
    public const string ActionFontDown = "com.companyname.SoundWord.action.FONT_DOWN";
    public const string ActionOpacityUp = "com.companyname.SoundWord.action.OPACITY_UP";
    public const string ActionOpacityDown = "com.companyname.SoundWord.action.OPACITY_DOWN";
    public const string ExtraSourceLanguage = "sourceLanguage";

    private const int NotificationId = 2001;
    private const string NotificationChannelId = "soundword.capture.controls";
    private const string OverlayPreferencesName = "soundword.overlay";
    private const string OverlayWidthKey = "widthDp";
    private const string OverlayHeightKey = "heightDp";
    private const string OverlayXKey = "xPx";
    private const string OverlayYKey = "yPx";
    private const string OverlayFontSizeKey = "fontSizeSp";
    private const string OverlayOpacityKey = "opacity";
    private const string OverlayLockedKey = "locked";
    private const int DefaultOverlayWidthDp = 360;
    private const int DefaultOverlayHeightDp = 92;
    private const int DefaultSubtitleSizeSp = 22;
    private const int DefaultOverlayOpacity = 170;
    private const int MinOverlayWidthDp = 220;
    private const int MinOverlayHeightDp = 54;
    private const int MinSubtitleSizeSp = 14;
    private const int MaxSubtitleSizeSp = 38;
    private const int MinOverlayOpacity = 40;
    private const int MaxOverlayOpacity = 230;
    private const int ResizeEdgeDp = 28;

    private readonly Handler _mainHandler = new(Looper.MainLooper!);

    private IWindowManager? _windowManager;
    private FrameLayout? _overlayRoot;
    private TextView? _subtitleView;
    private WindowManagerLayoutParams? _overlayParameters;

    private MediaProjection? _mediaProjection;
    private AudioRecord? _audioRecord;
    private PushAudioInputStream? _pushStream;
    private TranslationRecognizer? _recognizer;
    private CancellationTokenSource? _captureCancellation;
    private Task? _captureTask;
    private volatile bool _hasTranslationResult;
    private int _overlayWidthDp = DefaultOverlayWidthDp;
    private int _overlayHeightDp = DefaultOverlayHeightDp;
    private int _overlayX = int.MinValue;
    private int _overlayY = int.MinValue;
    private int _subtitleSizeSp = DefaultSubtitleSizeSp;
    private int _overlayOpacity = DefaultOverlayOpacity;
    private bool _overlayLocked;
    private string _lastNotificationContent = "正在翻译系统声音";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            _ = StopAllAsync();
            return StartCommandResult.NotSticky;
        }

        if (intent?.Action == ActionUnlockOverlay)
        {
            UnlockOverlay();
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionToggleOverlayLock)
        {
            ToggleOverlayLock();
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionFontUp)
        {
            ChangeSubtitleSize(2);
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionFontDown)
        {
            ChangeSubtitleSize(-2);
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionOpacityUp)
        {
            ChangeOverlayOpacity(30);
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionOpacityDown)
        {
            ChangeOverlayOpacity(-30);
            return StartCommandResult.Sticky;
        }

        if (intent?.Action == ActionStart)
        {
            var sourceLanguage = intent.GetStringExtra(ExtraSourceLanguage) ?? "ja-JP";
            _ = StartAllAsync(sourceLanguage);
        }

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _ = StopAllAsync();
        base.OnDestroy();
    }

    private async Task StartAllAsync(string sourceLanguage)
    {
        try
        {
            await StopAllAsync(stopSelf: false).ConfigureAwait(false);

            if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            {
                ShowOverlayText("系统音频捕获需要 Android 10+");
                StopSelf();
                return;
            }

            if (MediaProjectionPermissionStore.Data is null)
            {
                ShowOverlayText("缺少屏幕捕获授权，请回到应用重新启动。");
                StopSelf();
                return;
            }

            StartForeground(NotificationId, BuildNotification("正在启动实时字幕..."));
            CreateOverlay();
            ShowOverlayText("正在连接 Azure...");
            _hasTranslationResult = false;

            var projectionManager = (MediaProjectionManager?)GetSystemService(MediaProjectionService);
            _mediaProjection = projectionManager?.GetMediaProjection(
                MediaProjectionPermissionStore.ResultCode,
                MediaProjectionPermissionStore.Data);

            if (_mediaProjection is null)
            {
                ShowOverlayText("无法创建 MediaProjection。");
                StopSelf();
                return;
            }

            StartSpeechTranslation(sourceLanguage);
            StartAudioCaptureLoop();
            UpdateNotification("正在翻译系统声音");
            ShowOverlayText("等待系统声音...");
        }
        catch (Exception ex)
        {
            ShowOverlayText("启动失败：" + ex.Message);
            UpdateNotification("启动失败");
        }
    }

    private void StartSpeechTranslation(string sourceLanguage)
    {
        var speechConfig = SpeechTranslationConfig.FromSubscription(
            AzureSpeechSettings.SubscriptionKey,
            AzureSpeechSettings.Region);
        speechConfig.SpeechRecognitionLanguage = sourceLanguage;
        speechConfig.AddTargetLanguage(AzureSpeechSettings.TargetLanguage);

        var audioFormat = AudioStreamFormat.GetWaveFormatPCM(
            samplesPerSecond: 16000,
            bitsPerSample: 16,
            channels: 1);

        _pushStream = AudioInputStream.CreatePushStream(audioFormat);
        var audioConfig = AudioConfig.FromStreamInput(_pushStream);

        _recognizer = new TranslationRecognizer(speechConfig, audioConfig);
        _recognizer.Recognizing += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatingSpeech &&
                e.Result.Translations.TryGetValue(AzureSpeechSettings.TargetLanguage, out var text) &&
                !string.IsNullOrWhiteSpace(text))
            {
                _hasTranslationResult = true;
                ShowOverlayText(text);
            }
        };
        _recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatedSpeech &&
                e.Result.Translations.TryGetValue(AzureSpeechSettings.TargetLanguage, out var text) &&
                !string.IsNullOrWhiteSpace(text))
            {
                _hasTranslationResult = true;
                ShowOverlayText(text);
            }
        };
        _recognizer.Canceled += (_, e) =>
        {
            var details = string.IsNullOrWhiteSpace(e.ErrorDetails)
                ? e.Reason.ToString()
                : e.ErrorDetails;
            ShowOverlayText("识别已取消：" + details);
            UpdateNotification("识别已取消");
        };

        _recognizer.StartContinuousRecognitionAsync().GetAwaiter().GetResult();
    }

    private void StartAudioCaptureLoop()
    {
        var captureBuilder = new AudioPlaybackCaptureConfiguration.Builder(_mediaProjection!);
        captureBuilder.AddMatchingUsage(AudioUsageKind.Media);
        captureBuilder.AddMatchingUsage(AudioUsageKind.Game);
        captureBuilder.AddMatchingUsage(AudioUsageKind.Unknown);
        var config = captureBuilder.Build() ?? throw new InvalidOperationException("Unable to build audio capture config.");

        var formatBuilder = new AudioFormat.Builder();
        formatBuilder.SetEncoding(Android.Media.Encoding.Pcm16bit);
        formatBuilder.SetSampleRate(16000);
        formatBuilder.SetChannelMask(ChannelOut.Mono);
        var format = formatBuilder.Build() ?? throw new InvalidOperationException("Unable to build audio format.");

        var minBuffer = AudioRecord.GetMinBufferSize(
            16000,
            ChannelIn.Mono,
            Android.Media.Encoding.Pcm16bit);
        var bufferSize = Math.Max(minBuffer, 16000 * 2);

        var recordBuilder = new AudioRecord.Builder();
        recordBuilder.SetAudioFormat(format);
        recordBuilder.SetBufferSizeInBytes(bufferSize);
        recordBuilder.SetAudioPlaybackCaptureConfig(config);
        _audioRecord = recordBuilder.Build() ?? throw new InvalidOperationException("Unable to build audio recorder.");

        _audioRecord.StartRecording();

        _captureCancellation = new CancellationTokenSource();
        var token = _captureCancellation.Token;
        _captureTask = Task.Run(() =>
        {
            var buffer = new byte[bufferSize / 2];
            var totalBytes = 0L;
            var quietReads = 0;
            var lastPeak = 0;
            var lastDiagnostic = DateTimeOffset.UtcNow;
            while (!token.IsCancellationRequested)
            {
                var read = _audioRecord.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    continue;
                }

                totalBytes += read;
                var peak = GetPcm16Peak(buffer, read);
                lastPeak = Math.Max(lastPeak, peak);
                quietReads = peak < 256 ? quietReads + 1 : 0;

                var now = DateTimeOffset.UtcNow;
                if (now - lastDiagnostic >= TimeSpan.FromSeconds(1))
                {
                    Log.Info(LogTag, $"Audio capture bytes={totalBytes}, peak={lastPeak}, quietReads={quietReads}");
                    if (!_hasTranslationResult)
                    {
                        if (lastPeak < 256)
                        {
                            ShowOverlayText("系统音频捕获为静音\n请关蓝牙/开扬声器，或换一个App测试");
                        }
                        else
                        {
                            ShowOverlayText($"已捕获系统声音，等待识别...\n音量峰值：{lastPeak}");
                        }
                    }

                    totalBytes = 0;
                    lastPeak = 0;
                    lastDiagnostic = now;
                }

                if (read == buffer.Length)
                {
                    _pushStream?.Write(buffer);
                }
                else
                {
                    var chunk = new byte[read];
                    Array.Copy(buffer, chunk, read);
                    _pushStream?.Write(chunk);
                }
            }
        }, token);
    }

    private static int GetPcm16Peak(byte[] buffer, int byteCount)
    {
        var peak = 0;
        var sampleBytes = byteCount - byteCount % 2;
        for (var i = 0; i < sampleBytes; i += 2)
        {
            var sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            var value = Math.Abs((int)sample);
            if (value > peak)
            {
                peak = value;
            }
        }

        return peak;
    }

    private async Task StopAllAsync(bool stopSelf = true)
    {
        var cancellation = _captureCancellation;
        _captureCancellation = null;
        cancellation?.Cancel();

        try
        {
            _audioRecord?.Stop();
        }
        catch
        {
            // Ignore stop races when the recorder was not fully started.
        }

        _audioRecord?.Release();
        _audioRecord?.Dispose();
        _audioRecord = null;

        _pushStream?.Close();
        _pushStream?.Dispose();
        _pushStream = null;

        if (_recognizer is not null)
        {
            try
            {
                await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
            catch
            {
                // The recognizer may already be canceled if the audio stream closed first.
            }

            _recognizer.Dispose();
            _recognizer = null;
        }

        _mediaProjection?.Stop();
        _mediaProjection?.Dispose();
        _mediaProjection = null;

        RemoveOverlay();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            StopForeground(StopForegroundFlags.Remove);
        }
        else
        {
#pragma warning disable CA1422
            StopForeground(true);
#pragma warning restore CA1422
        }

        if (stopSelf)
        {
            StopSelf();
        }
    }

    private void CreateOverlay()
    {
        _mainHandler.Post(() =>
        {
            if (_overlayRoot is not null)
            {
                return;
            }

            _windowManager = GetSystemService(WindowService)?.JavaCast<IWindowManager>();
            if (_windowManager is null)
            {
                Log.Error(LogTag, "WindowManager service is unavailable.");
                Toast.MakeText(this, "无法创建悬浮窗：WindowManager 不可用", ToastLength.Long)?.Show();
                UpdateNotification("悬浮窗创建失败");
                return;
            }

            LoadOverlaySettings();

            _subtitleView = new TextView(this)
            {
                Text = "SoundWord",
                TextSize = _subtitleSizeSp,
                Gravity = GravityFlags.Center,
                Typeface = Typeface.DefaultBold
            };
            _subtitleView.SetTextColor(Color.White);
            _subtitleView.SetShadowLayer(4, 0, 2, Color.Black);
            _subtitleView.SetPadding(Dp(18), Dp(10), Dp(18), Dp(10));
            _subtitleView.SetIncludeFontPadding(false);

            _overlayRoot = new FrameLayout(this);
            _overlayRoot.SetBackgroundColor(Color.Argb(_overlayOpacity, 15, 23, 42));
            _overlayRoot.AddView(_subtitleView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));
            _overlayRoot.SetOnTouchListener(new OverlayTouchListener(
                _windowManager,
                () => _overlayLocked,
                Dp(ResizeEdgeDp),
                () => Dp(MinOverlayWidthDp),
                () => Dp(GetMaxOverlayWidthDp()),
                () => Dp(MinOverlayHeightDp),
                () => Dp(GetMaxOverlayHeightDp()),
                SaveOverlayBoundsFromParameters));

            var windowType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            var width = Dp(GetConstrainedOverlayWidth());
            var height = Dp(GetConstrainedOverlayHeight());
            if (_overlayX == int.MinValue || _overlayY == int.MinValue)
            {
                _overlayX = Math.Max(0, ((Resources?.DisplayMetrics?.WidthPixels ?? width) - width) / 2);
                _overlayY = Math.Max(0, (Resources?.DisplayMetrics?.HeightPixels ?? height) - height - Dp(120));
            }

            var parameters = new WindowManagerLayoutParams(
                width,
                height,
                windowType,
                GetOverlayWindowFlags(),
                Format.Translucent)
            {
                Gravity = GravityFlags.Top | GravityFlags.Left,
                X = _overlayX,
                Y = _overlayY
            };

            _overlayRoot.Tag = parameters;
            _overlayParameters = parameters;
            ApplyOverlaySettings(updateLayout: false);
            try
            {
                _windowManager.AddView(_overlayRoot, parameters);
                Log.Info(LogTag, "Subtitle overlay added.");
            }
            catch (Exception ex)
            {
                Log.Error(LogTag, "Failed to add subtitle overlay: " + ex);
                Toast.MakeText(this, "悬浮窗创建失败：" + ex.Message, ToastLength.Long)?.Show();
                UpdateNotification("悬浮窗创建失败");
                _overlayRoot = null;
                _subtitleView = null;
                _overlayParameters = null;
            }
        });
    }

    private void ChangeOverlayWidth(int deltaDp)
    {
        _mainHandler.Post(() =>
        {
            _overlayWidthDp = Math.Clamp(_overlayWidthDp + deltaDp, MinOverlayWidthDp, GetMaxOverlayWidthDp());
            SaveOverlaySettings();
            ApplyOverlaySettings();
        });
    }

    private void ChangeSubtitleSize(int deltaSp)
    {
        _mainHandler.Post(() =>
        {
            _subtitleSizeSp = Math.Clamp(_subtitleSizeSp + deltaSp, MinSubtitleSizeSp, MaxSubtitleSizeSp);
            SaveOverlaySettings();
            ApplyOverlaySettings();
            UpdateNotification(_lastNotificationContent);
        });
    }

    private void ChangeOverlayOpacity(int delta)
    {
        _mainHandler.Post(() =>
        {
            _overlayOpacity = Math.Clamp(_overlayOpacity + delta, MinOverlayOpacity, MaxOverlayOpacity);
            SaveOverlaySettings();
            ApplyOverlaySettings();
            UpdateNotification(_lastNotificationContent);
        });
    }

    private void LockOverlay()
    {
        _overlayLocked = true;
        SaveOverlaySettings();
        ApplyOverlaySettings();
        UpdateNotification(_lastNotificationContent);
        ShowOverlayText("悬浮窗已锁定\n回到 SoundWord 可解锁");
    }

    private void ToggleOverlayLock()
    {
        _mainHandler.Post(() =>
        {
            LoadOverlaySettings();
            _overlayLocked = !_overlayLocked;
            SaveOverlaySettings();
            ApplyOverlaySettings();
            UpdateNotification(_lastNotificationContent);
            ShowOverlayText(_overlayLocked ? "悬浮窗已锁定" : "悬浮窗已解锁");
        });
    }

    private void UnlockOverlay()
    {
        _mainHandler.Post(() =>
        {
            LoadOverlaySettings();
            _overlayLocked = false;
            SaveOverlaySettings();
            ApplyOverlaySettings();
            UpdateNotification(_lastNotificationContent);
            ShowOverlayText("悬浮窗已解锁");
            Toast.MakeText(this, "悬浮窗已解锁", ToastLength.Short)?.Show();
        });
    }

    private void ApplyOverlaySettings(bool updateLayout = true)
    {
        if (_subtitleView is not null)
        {
            _subtitleView.TextSize = _subtitleSizeSp;
        }

        _overlayRoot?.SetBackgroundColor(Color.Argb(_overlayOpacity, 15, 23, 42));

        if (_overlayParameters is not null)
        {
            _overlayParameters.Width = Dp(GetConstrainedOverlayWidth());
            _overlayParameters.Height = Dp(GetConstrainedOverlayHeight());
            _overlayParameters.X = _overlayX == int.MinValue ? _overlayParameters.X : _overlayX;
            _overlayParameters.Y = _overlayY == int.MinValue ? _overlayParameters.Y : _overlayY;
            _overlayParameters.Flags = GetOverlayWindowFlags();
        }

        if (updateLayout &&
            _windowManager is not null &&
            _overlayRoot is not null &&
            _overlayParameters is not null)
        {
            _windowManager.UpdateViewLayout(_overlayRoot, _overlayParameters);
        }
    }

    private WindowManagerFlags GetOverlayWindowFlags()
    {
        var flags = WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen;
        if (_overlayLocked)
        {
            flags |= WindowManagerFlags.NotTouchable;
        }
        else
        {
            flags |= WindowManagerFlags.NotTouchModal;
        }

        return flags;
    }

    private void LoadOverlaySettings()
    {
        var preferences = GetSharedPreferences(OverlayPreferencesName, FileCreationMode.Private);
        _overlayWidthDp = preferences?.GetInt(OverlayWidthKey, DefaultOverlayWidthDp) ?? DefaultOverlayWidthDp;
        _overlayHeightDp = preferences?.GetInt(OverlayHeightKey, DefaultOverlayHeightDp) ?? DefaultOverlayHeightDp;
        _overlayX = preferences?.GetInt(OverlayXKey, int.MinValue) ?? int.MinValue;
        _overlayY = preferences?.GetInt(OverlayYKey, int.MinValue) ?? int.MinValue;
        _subtitleSizeSp = preferences?.GetInt(OverlayFontSizeKey, DefaultSubtitleSizeSp) ?? DefaultSubtitleSizeSp;
        _overlayOpacity = preferences?.GetInt(OverlayOpacityKey, DefaultOverlayOpacity) ?? DefaultOverlayOpacity;
        _overlayLocked = preferences?.GetBoolean(OverlayLockedKey, false) ?? false;

        _overlayWidthDp = Math.Clamp(_overlayWidthDp, MinOverlayWidthDp, GetMaxOverlayWidthDp());
        _overlayHeightDp = Math.Clamp(_overlayHeightDp, MinOverlayHeightDp, GetMaxOverlayHeightDp());
        _subtitleSizeSp = Math.Clamp(_subtitleSizeSp, MinSubtitleSizeSp, MaxSubtitleSizeSp);
        _overlayOpacity = Math.Clamp(_overlayOpacity, MinOverlayOpacity, MaxOverlayOpacity);
    }

    private void SaveOverlaySettings()
    {
        var preferences = GetSharedPreferences(OverlayPreferencesName, FileCreationMode.Private);
        var editor = preferences?.Edit();
        if (editor is null)
        {
            return;
        }

        editor.PutInt(OverlayWidthKey, _overlayWidthDp);
        editor.PutInt(OverlayHeightKey, _overlayHeightDp);
        editor.PutInt(OverlayXKey, _overlayX);
        editor.PutInt(OverlayYKey, _overlayY);
        editor.PutInt(OverlayFontSizeKey, _subtitleSizeSp);
        editor.PutInt(OverlayOpacityKey, _overlayOpacity);
        editor.PutBoolean(OverlayLockedKey, _overlayLocked);
        editor.Apply();
    }

    private void SaveOverlayBoundsFromParameters(WindowManagerLayoutParams parameters)
    {
        var density = Resources?.DisplayMetrics?.Density ?? 1;
        _overlayWidthDp = Math.Clamp((int)(parameters.Width / density), MinOverlayWidthDp, GetMaxOverlayWidthDp());
        _overlayHeightDp = Math.Clamp((int)(parameters.Height / density), MinOverlayHeightDp, GetMaxOverlayHeightDp());
        _overlayX = Math.Max(0, parameters.X);
        _overlayY = Math.Max(0, parameters.Y);
        SaveOverlaySettings();
    }

    private int GetConstrainedOverlayWidth()
    {
        return Math.Clamp(_overlayWidthDp, MinOverlayWidthDp, GetMaxOverlayWidthDp());
    }

    private int GetConstrainedOverlayHeight()
    {
        return Math.Clamp(_overlayHeightDp, MinOverlayHeightDp, GetMaxOverlayHeightDp());
    }

    private int GetMaxOverlayWidthDp()
    {
        var density = Resources?.DisplayMetrics?.Density ?? 1;
        var widthPixels = Resources?.DisplayMetrics?.WidthPixels ?? Dp(DefaultOverlayWidthDp);
        return Math.Max(MinOverlayWidthDp, (int)(widthPixels / density) - 16);
    }

    private int GetMaxOverlayHeightDp()
    {
        var density = Resources?.DisplayMetrics?.Density ?? 1;
        var heightPixels = Resources?.DisplayMetrics?.HeightPixels ?? Dp(DefaultOverlayHeightDp);
        return Math.Max(MinOverlayHeightDp, (int)(heightPixels / density) - 80);
    }

    private void RemoveOverlay()
    {
        _mainHandler.Post(() =>
        {
            if (_windowManager is not null && _overlayRoot is not null)
            {
                try
                {
                    _windowManager.RemoveView(_overlayRoot);
                }
                catch
                {
                    // The overlay may already be detached during service shutdown.
                }
            }

            _overlayRoot = null;
            _subtitleView = null;
            _overlayParameters = null;
        });
    }

    private void ShowOverlayText(string text)
    {
        _mainHandler.Post(() =>
        {
            if (_subtitleView is not null)
            {
                _subtitleView.Text = text;
            }
        });
    }

    private Notification BuildNotification(string content)
    {
        EnsureNotificationChannel();
        LoadOverlaySettings();

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            launchIntent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var compactControls = CreateNotificationControls(Resource.Layout.notification_soundword_compact, content);
        var expandedControls = CreateNotificationControls(Resource.Layout.notification_soundword_expanded, content);
#pragma warning disable CS8602
        var builder = new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("SoundWord 实时字幕")
            .SetContentText(content)
            .SetSmallIcon(Resource.Drawable.notification_small_icon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetPriority((int)NotificationPriority.High)
            .SetContentIntent(pendingIntent)
            .SetCustomContentView(compactControls)
            .SetCustomBigContentView(expandedControls);
#pragma warning restore CS8602

        if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
        {
            builder.SetStyle(new Notification.DecoratedCustomViewStyle());
        }

        return builder.Build();
    }

    private void UpdateNotification(string content)
    {
        _lastNotificationContent = content;
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.Notify(NotificationId, BuildNotification(content));
    }

    private RemoteViews CreateNotificationControls(int layoutId, string content)
    {
        var views = new RemoteViews(PackageName, layoutId);
        views.SetTextViewText(Resource.Id.notification_title, "SoundWord 实时字幕");
        views.SetTextViewText(Resource.Id.notification_content, content);
        var isCompact = layoutId == Resource.Layout.notification_soundword_compact;
        views.SetTextViewText(Resource.Id.action_lock, _overlayLocked
            ? isCompact ? "解" : "解锁"
            : isCompact ? "锁" : "锁定");
        views.SetOnClickPendingIntent(Resource.Id.action_font_down, CreateServicePendingIntent(ActionFontDown, 1));
        views.SetOnClickPendingIntent(Resource.Id.action_font_up, CreateServicePendingIntent(ActionFontUp, 2));
        views.SetOnClickPendingIntent(Resource.Id.action_opacity_down, CreateServicePendingIntent(ActionOpacityDown, 3));
        views.SetOnClickPendingIntent(Resource.Id.action_opacity_up, CreateServicePendingIntent(ActionOpacityUp, 4));
        views.SetOnClickPendingIntent(Resource.Id.action_lock, CreateServicePendingIntent(ActionToggleOverlayLock, 5));
        views.SetOnClickPendingIntent(Resource.Id.action_stop, CreateServicePendingIntent(ActionStop, 6));
        return views;
    }

    private PendingIntent CreateServicePendingIntent(string action, int requestCode)
    {
        var intent = new Intent(this, typeof(CaptureTranslationService));
        intent.SetAction(action);
        return PendingIntent.GetService(
            this,
            requestCode,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        var channel = new NotificationChannel(
            NotificationChannelId,
            "SoundWord Capture",
            NotificationImportance.Default)
        {
            Description = "系统声音实时翻译字幕"
        };
        manager?.CreateNotificationChannel(channel);
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);

    private sealed class OverlayTouchListener : Java.Lang.Object, View.IOnTouchListener
    {
        private readonly IWindowManager _windowManager;
        private readonly Func<bool> _isLocked;
        private readonly int _resizeEdgePx;
        private readonly Func<int> _minWidthPx;
        private readonly Func<int> _maxWidthPx;
        private readonly Func<int> _minHeightPx;
        private readonly Func<int> _maxHeightPx;
        private readonly Action<WindowManagerLayoutParams> _onBoundsChanged;
        private float _startRawX;
        private float _startRawY;
        private int _startX;
        private int _startY;
        private int _startWidth;
        private int _startHeight;
        private bool _resizingWidth;
        private bool _resizingHeight;

        public OverlayTouchListener(
            IWindowManager windowManager,
            Func<bool> isLocked,
            int resizeEdgePx,
            Func<int> minWidthPx,
            Func<int> maxWidthPx,
            Func<int> minHeightPx,
            Func<int> maxHeightPx,
            Action<WindowManagerLayoutParams> onBoundsChanged)
        {
            _windowManager = windowManager;
            _isLocked = isLocked;
            _resizeEdgePx = resizeEdgePx;
            _minWidthPx = minWidthPx;
            _maxWidthPx = maxWidthPx;
            _minHeightPx = minHeightPx;
            _maxHeightPx = maxHeightPx;
            _onBoundsChanged = onBoundsChanged;
        }

        public bool OnTouch(View? view, MotionEvent? motionEvent)
        {
            if (_isLocked())
            {
                return false;
            }

            if (view is null ||
                view.Tag is not WindowManagerLayoutParams parameters ||
                motionEvent is null)
            {
                return false;
            }

            switch (motionEvent.Action)
            {
                case MotionEventActions.Down:
                    _startRawX = motionEvent.RawX;
                    _startRawY = motionEvent.RawY;
                    _startX = parameters.X;
                    _startY = parameters.Y;
                    _startWidth = parameters.Width;
                    _startHeight = parameters.Height;
                    _resizingWidth = view.Width - motionEvent.GetX() <= _resizeEdgePx;
                    _resizingHeight = view.Height - motionEvent.GetY() <= _resizeEdgePx;
                    return true;
                case MotionEventActions.Move:
                    var dx = (int)(motionEvent.RawX - _startRawX);
                    var dy = (int)(motionEvent.RawY - _startRawY);
                    if (_resizingWidth || _resizingHeight)
                    {
                        if (_resizingWidth)
                        {
                            parameters.Width = Math.Clamp(_startWidth + dx, _minWidthPx(), _maxWidthPx());
                        }

                        if (_resizingHeight)
                        {
                            parameters.Height = Math.Clamp(_startHeight + dy, _minHeightPx(), _maxHeightPx());
                        }
                    }
                    else
                    {
                        var metrics = view.Resources?.DisplayMetrics;
                        var maxX = Math.Max(0, (metrics?.WidthPixels ?? int.MaxValue) - parameters.Width);
                        var maxY = Math.Max(0, (metrics?.HeightPixels ?? int.MaxValue) - parameters.Height);
                        parameters.X = Math.Clamp(_startX + dx, 0, maxX);
                        parameters.Y = Math.Clamp(_startY + dy, 0, maxY);
                    }

                    _windowManager.UpdateViewLayout(view, parameters);
                    _onBoundsChanged(parameters);
                    return true;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _onBoundsChanged(parameters);
                    return true;
            }

            return false;
        }
    }
}
