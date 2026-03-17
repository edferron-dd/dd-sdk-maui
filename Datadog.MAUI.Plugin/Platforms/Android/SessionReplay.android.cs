using AndroidDatadog = Datadog.Android.Datadog;
using AndroidSessionReplay = Datadog.Android.SessionReplay.SessionReplay;

namespace Datadog.Maui.SessionReplay;

public static partial class SessionReplay
{
    static partial void PlatformStartRecording()
    {
        var sdkCore = AndroidDatadog.Instance;
        AndroidSessionReplay.Instance.StartRecording(sdkCore);
    }

    static partial void PlatformStopRecording()
    {
        var sdkCore = AndroidDatadog.Instance;
        AndroidSessionReplay.Instance.StopRecording(sdkCore);
    }
}
