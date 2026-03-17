using Datadog.iOS.Trace;
using Datadog.iOS.Internal;
using Foundation;

namespace Datadog.Maui.Platforms.iOS;

/// <summary>
/// iOS span backed by the DatadogWrapper Swift static library.
/// Holds a span UUID instead of a native OTSpan — all span operations go through
/// DDWrapperTrace which manages the OTSpan registry on the Swift side.
/// </summary>
internal class IOSSpan : Tracing.ISpan
{
    private readonly string _spanId;

    public IOSSpan(string spanId)
    {
        _spanId = spanId;
    }

    // Exposed for Tracer.ios.cs header injection (internal UUID, not the OT span ID)
    internal string NativeSpanId => _spanId;

    // SpanId/TraceId are not surfaced through the ObjC OpenTracing API.
    string Tracing.ISpan.SpanId => string.Empty;
    public string TraceId => string.Empty;

    public void SetTag(string key, string value)
    {
        DDWrapperTrace.SetStringTag(_spanId, key, value);
    }

    public void SetTag(string key, object value)
    {
        switch (value)
        {
            case string s:
                DDWrapperTrace.SetStringTag(_spanId, key, s);
                break;
            case bool b:
                DDWrapperTrace.SetBoolTag(_spanId, key, b);
                break;
            case int i:
                DDWrapperTrace.SetNumberTag(_spanId, key, new NSNumber(i));
                break;
            case long l:
                DDWrapperTrace.SetNumberTag(_spanId, key, new NSNumber(l));
                break;
            case float f:
                DDWrapperTrace.SetNumberTag(_spanId, key, new NSNumber(f));
                break;
            case double d:
                DDWrapperTrace.SetNumberTag(_spanId, key, new NSNumber(d));
                break;
            default:
                DDWrapperTrace.SetStringTag(_spanId, key, value?.ToString() ?? string.Empty);
                break;
        }
    }

    public void SetError(Exception exception)
    {
        var nsError = NSError.FromDomain(
            new NSString("Exception"),
            0,
            NSDictionary<NSString, NSObject>.FromObjectAndKey(
                new NSString(exception.ToString()),
                NSError.LocalizedDescriptionKey
            )
        );
        DDWrapperTrace.SetNSError(_spanId, nsError);
    }

    public void SetError(string message)
    {
        DDWrapperTrace.SetErrorMessage(_spanId, message);
    }

    public void AddEvent(string name, Dictionary<string, object>? attributes = null)
    {
        DDWrapperTrace.LogEvent(_spanId, name);
    }

    public void Finish()
    {
        DDWrapperTrace.FinishSpan(_spanId);
    }

    public void Dispose()
    {
        Finish();
    }
}
