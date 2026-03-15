using DatadogWrapper;
using Datadog.Maui.Logs;
using Foundation;

namespace Datadog.Maui.Platforms.iOS;

/// <summary>
/// iOS logger implementation backed by the DatadogWrapper Swift static library.
/// All dd-sdk-ios Swift class stubs are resolved inside Swift code — this class
/// only interacts with plain ObjC types from DDWrapperLogs.
/// </summary>
internal class IOSLogger : ILogger
{
    private readonly string _loggerId;

    public IOSLogger(string name)
    {
        Name = name;
        _loggerId = DDWrapperLogs.CreateLogger(name, service: null);
    }

    public string Name { get; }

    public void Debug(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        if (error != null)
            DDWrapperLogs.DebugWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Debug(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Info(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        if (error != null)
            DDWrapperLogs.InfoWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Info(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Notice(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        // Notice maps to info-level in the wrapper
        if (error != null)
            DDWrapperLogs.InfoWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Notice(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Warn(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        if (error != null)
            DDWrapperLogs.WarnWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Warn(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Error(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        if (error != null)
            DDWrapperLogs.ErrorWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Error(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Critical(string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        if (error != null)
            DDWrapperLogs.CriticalWithError(_loggerId, message, "Exception", 0, error.ToString(), Empty());
        else
            DDWrapperLogs.Critical(_loggerId, message, ConvertAttributes(attributes));
    }

    public void Log(Logs.LogLevel level, string message, Exception? error = null, Dictionary<string, object>? attributes = null)
    {
        switch (level)
        {
            case Logs.LogLevel.Debug:    Debug(message, error, attributes);    break;
            case Logs.LogLevel.Info:     Info(message, error, attributes);     break;
            case Logs.LogLevel.Notice:   Notice(message, error, attributes);   break;
            case Logs.LogLevel.Warn:     Warn(message, error, attributes);     break;
            case Logs.LogLevel.Error:    Error(message, error, attributes);    break;
            case Logs.LogLevel.Critical: Critical(message, error, attributes); break;
        }
    }

    public void AddAttribute(string key, object value)
    {
        DDWrapperLogs.AddAttribute(_loggerId, key, value?.ToString() ?? string.Empty);
    }

    public void RemoveAttribute(string key)
    {
        DDWrapperLogs.RemoveAttribute(_loggerId, key);
    }

    public void AddTag(string key, string value)
    {
        DDWrapperLogs.AddTag(_loggerId, key, value);
    }

    public void RemoveTag(string key)
    {
        DDWrapperLogs.RemoveTag(_loggerId, key);
    }

    private static NSDictionary<NSString, NSObject> ConvertAttributes(Dictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
            return Empty();

        var keys = attributes.Keys.Select(k => new NSString(k)).ToArray();
        var values = attributes.Values.Select(v => NSObject.FromObject(v)).ToArray();
        return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values, keys);
    }

    private static NSDictionary<NSString, NSObject> Empty() => new NSDictionary<NSString, NSObject>();
}
