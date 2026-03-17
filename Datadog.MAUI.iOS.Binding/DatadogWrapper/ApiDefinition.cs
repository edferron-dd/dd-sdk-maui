using Foundation;
using ObjCRuntime;

namespace DatadogWrapper;

// ---------------------------------------------------------------------------
// DDWrapperCore — Datadog SDK initialization
// ---------------------------------------------------------------------------
[BaseType(typeof(NSObject))]
interface DDWrapperCore
{
    /// <summary>
    /// Initialize Datadog with the given configuration.
    /// site: "us1" | "us3" | "us5" | "eu1" | "us1_fed" | "ap1"
    /// trackingConsent: "granted" | "notGranted" | "pending"
    /// </summary>
    [Static]
    [Export("initializeWithClientToken:env:site:service:trackingConsent:verbose:")]
    void Initialize(string clientToken, string env, string site, [NullAllowed] string service, string trackingConsent, bool verbose);

    [Static]
    [Export("setTrackingConsent:")]
    void SetTrackingConsent(string consent);

    [Static]
    [Export("setUserInfoWithUserId:name:email:extraInfo:")]
    void SetUserInfo(string userId, [NullAllowed] string name, [NullAllowed] string email, NSDictionary<NSString, NSObject> extraInfo);

    [Static]
    [Export("clearUserInfo")]
    void ClearUserInfo();
}

// ---------------------------------------------------------------------------
// DDWrapperRUM — Real User Monitoring
// ---------------------------------------------------------------------------
[BaseType(typeof(NSObject))]
interface DDWrapperRUM
{
    /// <summary>
    /// vitalsFrequency: "frequent" | "average" | "rare" | "never"
    /// </summary>
    [Static]
    [Export("enableWithApplicationId:sessionSampleRate:trackFrustrations:trackBackgroundEvents:vitalsFrequency:")]
    void Enable(string applicationId, float sessionSampleRate, bool trackFrustrations, bool trackBackgroundEvents, string vitalsFrequency);

    [Static]
    [Export("startViewWithKey:name:attributes:")]
    void StartView(string key, string name, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("stopViewWithKey:attributes:")]
    void StopView(string key, NSDictionary<NSString, NSObject> attributes);

    /// <summary>type: "tap" | "scroll" | "swipe" | "click" | "custom"</summary>
    [Static]
    [Export("addActionWithType:name:attributes:")]
    void AddAction(string type, string name, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("startResourceWithKey:method:url:attributes:")]
    void StartResource(string key, string method, string url, NSDictionary<NSString, NSObject> attributes);

    /// <summary>kind: "image" | "xhr" | "beacon" | "css" | "document" | "font" | "js" | "media" | "native" | "other"</summary>
    [Static]
    [Export("stopResourceWithKey:statusCode:kind:size:attributes:")]
    void StopResource(string key, [NullAllowed] NSNumber statusCode, string kind, [NullAllowed] NSNumber size, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("stopResourceWithError:errorType:errorMessage:attributes:")]
    void StopResourceWithError(string key, string errorType, string errorMessage, NSDictionary<NSString, NSObject> attributes);

    /// <summary>source: "source" | "network" | "webview" | "custom"</summary>
    [Static]
    [Export("addErrorWithMessage:source:stack:attributes:")]
    void AddError(string message, string source, [NullAllowed] string stack, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("addTimingWithName:")]
    void AddTiming(string name);

    [Static]
    [Export("addAttributeForKey:value:")]
    void AddAttribute(string key, string value);

    [Static]
    [Export("removeAttributeForKey:")]
    void RemoveAttribute(string key);

    [Static]
    [Export("startSession")]
    void StartSession();

    [Static]
    [Export("stopSession")]
    void StopSession();
}

// ---------------------------------------------------------------------------
// DDWrapperLogs — Logging
// ---------------------------------------------------------------------------
[BaseType(typeof(NSObject))]
interface DDWrapperLogs
{
    [Static]
    [Export("enable")]
    void Enable();

    /// <summary>Returns a logger ID (UUID) to be used in subsequent log calls.</summary>
    [Static]
    [Export("createLoggerWithName:service:")]
    string CreateLogger(string name, [NullAllowed] string service);

    // --- Simple log calls (no error) ---

    [Static]
    [Export("debugLog:message:attributes:")]
    void Debug(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("infoLog:message:attributes:")]
    void Info(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("noticeLog:message:attributes:")]
    void Notice(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("warnLog:message:attributes:")]
    void Warn(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("errorLog:message:attributes:")]
    void Error(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("criticalLog:message:attributes:")]
    void Critical(string loggerId, string message, NSDictionary<NSString, NSObject> attributes);

    // --- Log calls with NSError ---

    [Static]
    [Export("debugLogWithError:message:errorDomain:errorCode:errorDescription:attributes:")]
    void DebugWithError(string loggerId, string message, string errorDomain, nint errorCode, string errorDescription, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("infoLogWithError:message:errorDomain:errorCode:errorDescription:attributes:")]
    void InfoWithError(string loggerId, string message, string errorDomain, nint errorCode, string errorDescription, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("warnLogWithError:message:errorDomain:errorCode:errorDescription:attributes:")]
    void WarnWithError(string loggerId, string message, string errorDomain, nint errorCode, string errorDescription, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("errorLogWithError:message:errorDomain:errorCode:errorDescription:attributes:")]
    void ErrorWithError(string loggerId, string message, string errorDomain, nint errorCode, string errorDescription, NSDictionary<NSString, NSObject> attributes);

    [Static]
    [Export("criticalLogWithError:message:errorDomain:errorCode:errorDescription:attributes:")]
    void CriticalWithError(string loggerId, string message, string errorDomain, nint errorCode, string errorDescription, NSDictionary<NSString, NSObject> attributes);

    // --- Attributes & tags ---

    [Static]
    [Export("addAttributeForLogger:key:value:")]
    void AddAttribute(string loggerId, string key, string value);

    [Static]
    [Export("removeAttributeForLogger:key:")]
    void RemoveAttribute(string loggerId, string key);

    [Static]
    [Export("addTagForLogger:key:value:")]
    void AddTag(string loggerId, string key, string value);

    [Static]
    [Export("removeTagForLogger:key:")]
    void RemoveTag(string loggerId, string key);
}

// ---------------------------------------------------------------------------
// DDWrapperTrace — OpenTracing spans
// ---------------------------------------------------------------------------
[BaseType(typeof(NSObject))]
interface DDWrapperTrace
{
    [Static]
    [Export("enableWithSampleRate:")]
    void Enable(float sampleRate);

    /// <summary>Returns a span ID (UUID). Pass parentSpanId to create a child span.</summary>
    [Static]
    [Export("startSpanWithOperationName:parentSpanId:")]
    string StartSpan(string operationName, [NullAllowed] string parentSpanId);

    [Static]
    [Export("finishSpanWithId:")]
    void FinishSpan(string spanId);

    [Static]
    [Export("setStringTagForSpan:key:value:")]
    void SetStringTag(string spanId, string key, string value);

    [Static]
    [Export("setBoolTagForSpan:key:value:")]
    void SetBoolTag(string spanId, string key, bool value);

    [Static]
    [Export("setNumberTagForSpan:key:value:")]
    void SetNumberTag(string spanId, string key, NSNumber value);

    [Static]
    [Export("setErrorMessageForSpan:message:")]
    void SetErrorMessage(string spanId, string message);

    [Static]
    [Export("setNSErrorForSpan:error:")]
    void SetNSError(string spanId, NSError error);

    [Static]
    [Export("logEventForSpan:name:")]
    void LogEvent(string spanId, string name);

    /// <summary>Returns propagation headers as a flat string-to-string dictionary.</summary>
    [Static]
    [Export("injectHeadersForSpan:")]
    NSDictionary<NSString, NSString> InjectHeaders(string spanId);
}

// ---------------------------------------------------------------------------
// DDWrapperSessionReplay — Session Replay
// ---------------------------------------------------------------------------
[BaseType(typeof(NSObject))]
interface DDWrapperSessionReplay
{
    /// <summary>privacyLevel: "allow" | "mask" | "maskUserInput"</summary>
    [Static]
    [Export("enableWithSampleRate:privacyLevel:")]
    void Enable(float sampleRate, string privacyLevel);

    /// <summary>
    /// Granular privacy control with individual levels for text/input, image, and touch.
    /// textAndInputPrivacy: DDTextAndInputPrivacyLevel raw value (0=maskSensitiveInputs, 1=maskAllInputs, 2=maskAll)
    /// imagePrivacy:        DDImagePrivacyLevel raw value (0=maskNonBundledOnly, 1=maskAll, 2=maskNone)
    /// touchPrivacy:        DDTouchPrivacyLevel raw value (0=show, 1=hide)
    /// </summary>
    [Static]
    [Export("enableWithSampleRate:textAndInputPrivacy:imagePrivacy:touchPrivacy:startRecordingImmediately:")]
    void Enable(float sampleRate, nint textAndInputPrivacy, nint imagePrivacy, nint touchPrivacy, bool startRecordingImmediately);
}
