using DatadogWrapper;
using Datadog.Maui.Configuration;
using Foundation;

namespace Datadog.Maui;

public static partial class Datadog
{
    static partial void PlatformInitialize(DatadogConfiguration configuration)
    {
        DDWrapperCore.Initialize(
            clientToken: configuration.ClientToken,
            env: configuration.Environment,
            site: MapSite(configuration.Site),
            service: configuration.ServiceName,
            trackingConsent: MapTrackingConsent(configuration.TrackingConsent),
            verbose: configuration.VerboseLogging
        );

        // Enable RUM if configured
        if (configuration.Rum != null)
        {
            InitializeRum(configuration.Rum);
        }

        // Enable Logs if configured
        if (configuration.Logs != null)
        {
            InitializeLogs(configuration.Logs);
        }

        // Enable Tracing if configured
        if (configuration.Tracing != null)
        {
            InitializeTracing(configuration.Tracing);
        }
    }

    private static void InitializeRum(RumConfiguration rumConfig)
    {
        DDWrapperRUM.Enable(
            applicationId: rumConfig.ApplicationId,
            sessionSampleRate: rumConfig.SessionSampleRate,
            trackFrustrations: rumConfig.TrackUserInteractions,
            trackBackgroundEvents: true,
            vitalsFrequency: MapVitalsFrequency(rumConfig.VitalsUpdateFrequency)
        );
    }

    private static void InitializeLogs(LogsConfiguration logsConfig)
    {
        DDWrapperLogs.Enable();
    }

    private static void InitializeTracing(TracingConfiguration tracingConfig)
    {
        DDWrapperTrace.Enable(sampleRate: tracingConfig.SampleRate);
    }

    static partial void PlatformSetUser(UserInfo userInfo)
    {
        var extraInfo = userInfo.ExtraInfo != null && userInfo.ExtraInfo.Count > 0
            ? NSDictionary<NSString, NSObject>.FromObjectsAndKeys(
                userInfo.ExtraInfo.Values.Select(v => NSObject.FromObject(v)).ToArray(),
                userInfo.ExtraInfo.Keys.Select(k => new NSString(k)).ToArray()
            )
            : new NSDictionary<NSString, NSObject>();

        DDWrapperCore.SetUserInfo(
            userId: userInfo.Id ?? string.Empty,
            name: userInfo.Name,
            email: userInfo.Email,
            extraInfo: extraInfo
        );
    }

    static partial void PlatformSetTags(Dictionary<string, string> tags)
    {
        // iOS SDK doesn't have a global SetTag API at the Datadog level.
        // Tags are set per-logger or per-RUM monitor.
    }

    static partial void PlatformSetTrackingConsent(TrackingConsent consent)
    {
        DDWrapperCore.SetTrackingConsent(MapTrackingConsent(consent));
    }

    static partial void PlatformClearUser()
    {
        DDWrapperCore.ClearUserInfo();
    }

    static partial void PlatformAddAttribute(string key, object value)
    {
        DDWrapperRUM.AddAttribute(key, value?.ToString() ?? string.Empty);
    }

    static partial void PlatformRemoveAttribute(string key)
    {
        DDWrapperRUM.RemoveAttribute(key);
    }

    // Helper methods to map enums to string values used by the Swift wrapper

    private static string MapSite(DatadogSite site)
    {
        return site switch
        {
            DatadogSite.US1     => "us1",
            DatadogSite.US3     => "us3",
            DatadogSite.US5     => "us5",
            DatadogSite.EU1     => "eu1",
            DatadogSite.US1_FED => "us1_fed",
            DatadogSite.AP1     => "ap1",
            _                   => "us1"
        };
    }

    private static string MapTrackingConsent(TrackingConsent consent)
    {
        return consent switch
        {
            TrackingConsent.Granted    => "granted",
            TrackingConsent.NotGranted => "notGranted",
            TrackingConsent.Pending    => "pending",
            _                          => "pending"
        };
    }

    private static string MapVitalsFrequency(VitalsUpdateFrequency frequency)
    {
        return frequency switch
        {
            VitalsUpdateFrequency.Frequent => "frequent",
            VitalsUpdateFrequency.Average  => "average",
            VitalsUpdateFrequency.Rare     => "rare",
            _                              => "average"
        };
    }
}
