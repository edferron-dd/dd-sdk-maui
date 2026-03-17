using DatadogWrapper;

namespace Datadog.Maui.SessionReplay;

public static partial class SessionReplay
{
    static partial void PlatformStartRecording()
    {
        DDWrapperSessionReplay.StartRecording();
    }

    static partial void PlatformStopRecording()
    {
        DDWrapperSessionReplay.StopRecording();
    }
}
