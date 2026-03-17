using Datadog.iOS.Trace;
using Foundation;

namespace Datadog.Maui.Tracing;

public static partial class Tracer
{
    private static DDTracer? _nativeTracer;

    private static DDTracer NativeTracer
    {
        get
        {
            if (_nativeTracer == null)
            {
                _nativeTracer = DDTracer.Shared;
            }
            return _nativeTracer ?? throw new InvalidOperationException("Failed to initialize Datadog tracer");
        }
    }

    private static partial ISpan PlatformStartSpan(string operationName, ISpan? parent, DateTimeOffset? startTime)
    {
        string? parentSpanId = parent is Platforms.iOS.IOSSpan iosParent ? iosParent.NativeSpanId : null;
        var spanId = DDWrapperTrace.StartSpan(operationName, parentSpanId);
        return new Platforms.iOS.IOSSpan(spanId);
    }

    private static partial ISpan? PlatformGetActiveSpan()
    {
        // The iOS SDK doesn't expose an active span concept at this level.
        return null;
    }

    private static partial void PlatformInject(IDictionary<string, string> headers, ISpan? span)
    {
        if (span is not Platforms.iOS.IOSSpan iosSpan)
            return;

        var propagationHeaders = DDWrapperTrace.InjectHeaders(iosSpan.NativeSpanId);
        if (propagationHeaders == null)
            return;

        foreach (var key in propagationHeaders.Keys)
        {
            if (key is Foundation.NSString nsKey &&
                propagationHeaders[nsKey] is Foundation.NSString nsValue)
            {
                headers[nsKey.ToString()] = nsValue.ToString();
            }
        }
    }

    private static partial ISpan? PlatformExtract(IDictionary<string, string> headers)
    {
        // Extraction from carrier is not supported in this wrapper.
        return null;
    }
}
