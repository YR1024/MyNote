using Android.Content;

namespace SoundWord;

public static class MediaProjectionPermissionStore
{
    public static int ResultCode { get; set; }

    public static Intent? Data { get; set; }
}
