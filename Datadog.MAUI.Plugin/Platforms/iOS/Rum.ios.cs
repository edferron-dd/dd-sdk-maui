using Datadog.iOS.RUM;
using Foundation;
using Datadog.iOS.Internal;

namespace Datadog.Maui.Rum;

public static partial class Rum
{
    static partial void PlatformStartView(string key, string name, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.StartView(key, name, ConvertAttributes(attributes));
    }

    static partial void PlatformStopView(string key, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.StopView(key, ConvertAttributes(attributes));
    }

    static partial void PlatformAddAction(RumActionType type, string name, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.AddAction(MapActionType(type), name, ConvertAttributes(attributes));
    }

    static partial void PlatformStartResource(string key, string method, string url, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.StartResource(key, method, url, ConvertAttributes(attributes));
    }

    static partial void PlatformStopResource(string key, int? statusCode, long? size, RumResourceKind kind, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.StopResource(
            key,
            statusCode.HasValue ? new NSNumber(statusCode.Value) : null,
            MapResourceKind(kind),
            size.HasValue ? new NSNumber(size.Value) : null,
            ConvertAttributes(attributes)
        );
    }

    static partial void PlatformStopResourceWithError(string key, Exception error, Dictionary<string, object>? attributes)
    {
        DDWrapperRUM.StopResourceWithError(
            key,
            errorType: "Exception",
            errorMessage: error.Message,
            ConvertAttributes(attributes)
        );
    }

    static partial void PlatformAddError(string message, RumErrorSource source, Exception? exception, Dictionary<string, object>? attributes)
    {
        var monitor = DDRUMMonitor.Shared;
        var errorSource = MapErrorSource(source);
        var nsAttributes = attributes != null ? ConvertAttributes(attributes) : null;

        if (exception != null)
        {
            // Create NSError with detailed exception information including stack trace
            var userInfo = new NSMutableDictionary<NSString, NSObject>();

            // Use provided message or exception message
            var errorMessage = message ?? exception.Message;
            userInfo[NSError.LocalizedDescriptionKey] = new NSString(errorMessage);

            // Include exception type and full details
            userInfo[new NSString("ExceptionType")] = new NSString(exception.GetType().FullName ?? exception.GetType().Name);
            userInfo[new NSString("Message")] = new NSString(exception.Message);

            // Include full stack trace if available
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                userInfo[new NSString("StackTrace")] = new NSString(exception.StackTrace);
            }

            // Include inner exception if present
            if (exception.InnerException != null)
            {
                userInfo[new NSString("InnerException")] = new NSString(exception.InnerException.ToString());
            }

            // Create NSError with exception type as domain
            var nsError = NSError.FromDomain(
                new NSString(exception.GetType().Name),
                -1,
                userInfo
            );

            // Add error using NSError overload
            monitor.AddError(
                error: nsError,
                source: errorSource,
                attributes: nsAttributes
            );
        }
        else
        {
            monitor.AddError(
                message: message,
                source: errorSource,
                stack: null,
                attributes: nsAttributes
            );
        }
    }

    static partial void PlatformAddTiming(string name)
    {
        DDWrapperRUM.AddTiming(name);
    }

    static partial void PlatformAddAttribute(string key, object value)
    {
        DDWrapperRUM.AddAttribute(key, value?.ToString() ?? string.Empty);
    }

    static partial void PlatformRemoveAttribute(string key)
    {
        DDWrapperRUM.RemoveAttribute(key);
    }

    static partial void PlatformStartSession()
    {
        DDWrapperRUM.StartSession();
    }

    static partial void PlatformStopSession()
    {
        DDWrapperRUM.StopSession();
    }

    private static string MapActionType(RumActionType type)
    {
        return type switch
        {
            RumActionType.Tap    => "tap",
            RumActionType.Scroll => "scroll",
            RumActionType.Swipe  => "swipe",
            RumActionType.Click  => "click",
            RumActionType.Custom => "custom",
            _                    => "custom"
        };
    }

    private static string MapResourceKind(RumResourceKind kind)
    {
        return kind switch
        {
            RumResourceKind.Image    => "image",
            RumResourceKind.Xhr      => "xhr",
            RumResourceKind.Beacon   => "beacon",
            RumResourceKind.Css      => "css",
            RumResourceKind.Document => "document",
            RumResourceKind.Font     => "font",
            RumResourceKind.Js       => "js",
            RumResourceKind.Media    => "media",
            RumResourceKind.Native   => "native",
            RumResourceKind.Other    => "other",
            _                        => "native"
        };
    }

    private static string MapErrorSource(RumErrorSource source)
    {
        return source switch
        {
            RumErrorSource.Source  => "source",
            RumErrorSource.Network => "network",
            RumErrorSource.WebView => "webview",
            RumErrorSource.Custom  => "custom",
            _                      => "source"
        };
    }

    private static NSDictionary<NSString, NSObject> ConvertAttributes(Dictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return new NSDictionary<NSString, NSObject>();

        var keys = attributes.Keys.Select(k => new NSString(k)).ToArray();
        var values = attributes.Values.Select(v => NSObject.FromObject(v)).ToArray();
        return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values, keys);
    }
}
