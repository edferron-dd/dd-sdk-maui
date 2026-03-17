using DatadogWrapper;
using Datadog.Maui.Configuration;
using Datadog.iOS.Core;
using Datadog.iOS.RUM;
using Datadog.iOS.Logs;
using Datadog.iOS.Trace;
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
        var traceConfiguration = new DDTraceConfiguration();
        traceConfiguration.SampleRate = tracingConfig.SampleRate;

        // Configure URLSession tracking with first-party hosts
        if (tracingConfig.FirstPartyHosts.Length > 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Datadog] Configuring URLSession tracking for {tracingConfig.FirstPartyHosts.Length} first-party hosts");

                // Create NSSet of host strings
                var hosts = new NSSet<NSString>(
                    tracingConfig.FirstPartyHosts.Select(h => new NSString(h)).ToArray()
                );

                // Create first-party hosts tracing configuration
                var firstPartyHostsTracing = new DDTraceFirstPartyHostsTracing(hosts);

                // Create URLSession tracking configuration
                var urlSessionTracking = new DDTraceURLSessionTracking(firstPartyHostsTracing);

                // Apply to trace configuration
                traceConfiguration.SetURLSessionTracking(urlSessionTracking);

                System.Diagnostics.Debug.WriteLine("[Datadog] ✓ URLSession tracking configured");

                // Log configured hosts
                foreach (var host in tracingConfig.FirstPartyHosts)
                {
                    System.Diagnostics.Debug.WriteLine($"[Datadog]   - {host}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Datadog] ⚠ Failed to configure URLSession tracking: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[Datadog] ℹ No first-party hosts configured for tracing");
        }

        DDTrace.EnableWith(traceConfiguration);

        // EXPERIMENTAL: Try to enable URLSession instrumentation
        EnableURLSessionInstrumentation(tracingConfig);
    }

    private static void EnableURLSessionInstrumentation(TracingConfiguration tracingConfig)
    {
        // EXPERIMENTAL: Try to enable URLSession instrumentation
        // This may or may not work without a specific delegate class
        //
        // NOTE: URLSessionInstrumentation requires an INSUrlSessionDataDelegate instance
        // However, .NET MAUI's HttpClient uses an internal delegate that we can't access.
        // This method is disabled for now until we find a working approach.
        //
        // For now, the URLSession tracking configuration in InitializeTracing() may be
        // sufficient to enable automatic HTTP tracing. Testing needed.

        if (tracingConfig.FirstPartyHosts.Length == 0)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine("[Datadog] ℹ URLSession instrumentation requires a delegate instance");
        System.Diagnostics.Debug.WriteLine("[Datadog]   Relying on URLSession tracking configuration instead");
        System.Diagnostics.Debug.WriteLine("[Datadog]   If automatic HTTP tracing doesn't work, see docs for manual approach");

        // TODO: Implement one of these approaches:
        // 1. Create a custom NSUrlSessionDataDelegate subclass
        // 2. Use a DelegatingHandler wrapper for HttpClient
        // 3. Explore runtime method swizzling from C#
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
