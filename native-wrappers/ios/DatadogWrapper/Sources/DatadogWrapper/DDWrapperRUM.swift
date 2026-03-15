import Foundation
import DatadogRUM

@objc(DDWrapperRUM)
public class DDWrapperRUM: NSObject {

    // MARK: - Enable

    @objc(enableWithApplicationId:sessionSampleRate:trackFrustrations:trackBackgroundEvents:vitalsFrequency:)
    public static func enable(
        applicationId: String,
        sessionSampleRate: Float,
        trackFrustrations: Bool,
        trackBackgroundEvents: Bool,
        vitalsFrequency: String
    ) {
        var config = RUM.Configuration(applicationID: applicationId)
        config.sessionSampleRate = sessionSampleRate
        config.trackFrustrations = trackFrustrations
        config.trackBackgroundEvents = trackBackgroundEvents
        config.vitalsUpdateFrequency = parseVitalsFrequency(vitalsFrequency)
        RUM.enable(with: config)
    }

    // MARK: - Views

    @objc(startViewWithKey:name:attributes:)
    public static func startView(key: String, name: String, attributes: [String: Any]) {
        RUMMonitor.shared().startView(key: key, name: name, attributes: makeEncodableDict(attributes))
    }

    @objc(stopViewWithKey:attributes:)
    public static func stopView(key: String, attributes: [String: Any]) {
        RUMMonitor.shared().stopView(key: key, attributes: makeEncodableDict(attributes))
    }

    // MARK: - Actions

    @objc(addActionWithType:name:attributes:)
    public static func addAction(type: String, name: String, attributes: [String: Any]) {
        RUMMonitor.shared().addAction(type: parseActionType(type), name: name, attributes: makeEncodableDict(attributes))
    }

    // MARK: - Resources

    @objc(startResourceWithKey:method:url:attributes:)
    public static func startResource(key: String, method: String, url: String, attributes: [String: Any]) {
        RUMMonitor.shared().startResource(
            resourceKey: key,
            httpMethod: parseHttpMethod(method),
            urlString: url,
            attributes: makeEncodableDict(attributes)
        )
    }

    @objc(stopResourceWithKey:statusCode:kind:size:attributes:)
    public static func stopResource(
        key: String,
        statusCode: NSNumber?,
        kind: String,
        size: NSNumber?,
        attributes: [String: Any]
    ) {
        RUMMonitor.shared().stopResource(
            resourceKey: key,
            statusCode: statusCode?.intValue,
            kind: parseResourceKind(kind),
            size: size?.int64Value,
            attributes: makeEncodableDict(attributes)
        )
    }

    @objc(stopResourceWithError:errorType:errorMessage:attributes:)
    public static func stopResourceWithError(
        key: String,
        errorType: String,
        errorMessage: String,
        attributes: [String: Any]
    ) {
        RUMMonitor.shared().stopResourceWithError(
            resourceKey: key,
            message: errorMessage,
            type: errorType,
            response: nil,
            attributes: makeEncodableDict(attributes)
        )
    }

    // MARK: - Errors

    @objc(addErrorWithMessage:source:stack:attributes:)
    public static func addError(
        message: String,
        source: String,
        stack: String?,
        attributes: [String: Any]
    ) {
        RUMMonitor.shared().addError(
            message: message,
            type: nil,
            stack: stack,
            source: parseErrorSource(source),
            attributes: makeEncodableDict(attributes)
        )
    }

    // MARK: - Timings

    @objc(addTimingWithName:)
    public static func addTiming(name: String) {
        RUMMonitor.shared().addTiming(name: name)
    }

    // MARK: - Attributes

    @objc(addAttributeForKey:value:)
    public static func addAttribute(key: String, value: String) {
        RUMMonitor.shared().addAttribute(forKey: key, value: value)
    }

    @objc(removeAttributeForKey:)
    public static func removeAttribute(key: String) {
        RUMMonitor.shared().removeAttribute(forKey: key)
    }

    // MARK: - Session

    // Note: startSession() was removed from RUMMonitorProtocol in dd-sdk-ios 3.x.
    // The session lifecycle is managed by the SDK automatically.
    @objc(startSession)
    public static func startSession() {
        // No-op: session is started automatically by RUM.enable(with:).
    }

    @objc(stopSession)
    public static func stopSession() {
        RUMMonitor.shared().stopSession()
    }

    // MARK: - Private Helpers

    private static func parseActionType(_ type: String) -> RUMActionType {
        switch type.lowercased() {
        case "tap":    return .tap
        case "scroll": return .scroll
        case "swipe":  return .swipe
        case "click":  return .tap
        default:       return .custom
        }
    }

    private static func parseResourceKind(_ kind: String) -> RUMResourceType {
        switch kind.lowercased() {
        case "image":    return .image
        case "xhr":      return .xhr
        case "beacon":   return .beacon
        case "css":      return .css
        case "document": return .document
        case "font":     return .font
        case "js":       return .js
        case "media":    return .media
        case "native":   return .native
        default:         return .other
        }
    }

    private static func parseErrorSource(_ source: String) -> RUMErrorSource {
        switch source.lowercased() {
        case "source":  return .source
        case "network": return .network
        case "webview": return .webview
        case "custom":  return .custom
        default:        return .source
        }
    }

    private static func parseHttpMethod(_ method: String) -> RUMMethod {
        switch method.uppercased() {
        case "GET":     return .get
        case "POST":    return .post
        case "PUT":     return .put
        case "DELETE":  return .delete
        case "HEAD":    return .head
        case "PATCH":   return .patch
        case "CONNECT": return .connect
        case "OPTIONS": return .options
        case "TRACE":   return .trace
        default:        return .get
        }
    }

    private static func parseVitalsFrequency(_ freq: String) -> RUM.Configuration.VitalsFrequency? {
        switch freq.lowercased() {
        case "frequent": return .frequent
        case "average":  return .average
        case "rare":     return .rare
        case "never":    return nil
        default:         return .average
        }
    }
}
