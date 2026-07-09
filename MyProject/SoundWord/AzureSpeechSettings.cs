namespace SoundWord;

public static class AzureSpeechSettings
{
    private const string KeyAssetName = "azure-speech-key.txt";
    private static string? _subscriptionKey;

    public static string SubscriptionKey => GetSubscriptionKey();
    public const string Region = "eastasia";
    public const string TargetLanguage = "zh-Hans";

    private static string GetSubscriptionKey()
    {
        if (!string.IsNullOrWhiteSpace(_subscriptionKey))
        {
            return _subscriptionKey;
        }

        var environmentKey = Environment.GetEnvironmentVariable("SOUNDWORD_AZURE_SPEECH_KEY");
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            _subscriptionKey = environmentKey.Trim();
            return _subscriptionKey;
        }

        try
        {
            using var stream = Android.App.Application.Context.Assets?.Open(KeyAssetName)
                ?? throw new InvalidOperationException("Asset manager is unavailable.");
            using var reader = new StreamReader(stream);
            var assetKey = reader.ReadToEnd().Trim();
            if (!string.IsNullOrWhiteSpace(assetKey))
            {
                _subscriptionKey = assetKey;
                return _subscriptionKey;
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"缺少 Azure Speech Key。请在 Assets/{KeyAssetName} 写入本机密钥，或设置 SOUNDWORD_AZURE_SPEECH_KEY 环境变量。",
                ex);
        }

        throw new InvalidOperationException(
            $"缺少 Azure Speech Key。请在 Assets/{KeyAssetName} 写入本机密钥，或设置 SOUNDWORD_AZURE_SPEECH_KEY 环境变量。");
    }
}
