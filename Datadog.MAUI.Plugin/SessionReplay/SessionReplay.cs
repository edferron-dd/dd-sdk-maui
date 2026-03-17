namespace Datadog.Maui.SessionReplay;

/// <summary>
/// Static API for Session Replay recording control.
/// </summary>
public static partial class SessionReplay
{
    /// <summary>
    /// Starts session recording. Use this when Session Replay was enabled with
    /// <c>startRecordingImmediately = false</c>, or to resume recording after
    /// <see cref="StopRecording"/> was called.
    /// </summary>
    public static void StartRecording()
    {
        PlatformStartRecording();
    }

    /// <summary>
    /// Stops the current session recording. Recording can be resumed by calling
    /// <see cref="StartRecording"/>.
    /// </summary>
    public static void StopRecording()
    {
        PlatformStopRecording();
    }

    // Platform-specific partial methods
    static partial void PlatformStartRecording();
    static partial void PlatformStopRecording();
}
