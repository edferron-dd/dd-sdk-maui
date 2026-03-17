using DatadogWrapper;
using Datadog.Maui.Platforms.iOS;
using Datadog.iOS.Logs;
using Foundation;

namespace Datadog.Maui.Logs;

public static partial class Logs
{
    private static partial ILogger PlatformCreateLogger(string name)
    {
        return new IOSLogger(name);
    }

    private static partial void PlatformAddAttribute(string key, object value)
    {
        // Global log attributes are not directly supported by the iOS SDK.
        // Attributes are set per-logger via IOSLogger.AddAttribute.
    }

    private static partial void PlatformRemoveAttribute(string key)
    {
        // No-op: per-logger attributes only.
    }

    private static partial void PlatformAddTag(string key, string value)
    {
        // Tags are per-logger only in the iOS SDK.
    }

    private static partial void PlatformRemoveTag(string key)
    {
        // Tags are per-logger only in the iOS SDK.
    }
}
