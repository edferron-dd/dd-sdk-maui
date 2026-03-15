import Foundation
import DatadogLogs

// Disambiguate from os.Logger which is available via Foundation
private typealias SDKLogger = DatadogLogs.Logger

@objc(DDWrapperLogs)
public class DDWrapperLogs: NSObject {

    private static let lock = NSLock()
    private static var loggers: [String: any LoggerProtocol] = [:]

    // MARK: - Enable

    @objc(enable)
    public static func enable() {
        Logs.enable(with: Logs.Configuration())
    }

    // MARK: - Logger Lifecycle

    /// Creates a named logger and returns a UUID string used to reference it.
    @objc(createLoggerWithName:service:)
    public static func createLogger(name: String, service: String?) -> String {
        let id = UUID().uuidString
        let svc: String? = (service?.isEmpty == false) ? service : nil
        let config = SDKLogger.Configuration(
            service: svc,
            name: name,
            networkInfoEnabled: true,
            bundleWithRumEnabled: true,
            bundleWithTraceEnabled: true
        )
        let logger = SDKLogger.create(with: config)
        lock.withLock { loggers[id] = logger }
        return id
    }

    // MARK: - Logging

    @objc(debugLog:message:attributes:)
    public static func debug(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.debug(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(infoLog:message:attributes:)
    public static func info(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.info(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(noticeLog:message:attributes:)
    public static func notice(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.notice(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(warnLog:message:attributes:)
    public static func warn(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.warn(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(errorLog:message:attributes:)
    public static func error(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.error(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(criticalLog:message:attributes:)
    public static func critical(loggerId: String, message: String, attributes: [String: Any]) {
        withLogger(loggerId) { $0.critical(message, error: nil, attributes: makeEncodableDict(attributes)) }
    }

    @objc(debugLogWithError:message:errorDomain:errorCode:errorDescription:attributes:)
    public static func debugWithError(loggerId: String, message: String, errorDomain: String, errorCode: Int, errorDescription: String, attributes: [String: Any]) {
        let err = NSError(domain: errorDomain, code: errorCode, userInfo: [NSLocalizedDescriptionKey: errorDescription])
        withLogger(loggerId) { $0.debug(message, error: err, attributes: makeEncodableDict(attributes)) }
    }

    @objc(infoLogWithError:message:errorDomain:errorCode:errorDescription:attributes:)
    public static func infoWithError(loggerId: String, message: String, errorDomain: String, errorCode: Int, errorDescription: String, attributes: [String: Any]) {
        let err = NSError(domain: errorDomain, code: errorCode, userInfo: [NSLocalizedDescriptionKey: errorDescription])
        withLogger(loggerId) { $0.info(message, error: err, attributes: makeEncodableDict(attributes)) }
    }

    @objc(warnLogWithError:message:errorDomain:errorCode:errorDescription:attributes:)
    public static func warnWithError(loggerId: String, message: String, errorDomain: String, errorCode: Int, errorDescription: String, attributes: [String: Any]) {
        let err = NSError(domain: errorDomain, code: errorCode, userInfo: [NSLocalizedDescriptionKey: errorDescription])
        withLogger(loggerId) { $0.warn(message, error: err, attributes: makeEncodableDict(attributes)) }
    }

    @objc(errorLogWithError:message:errorDomain:errorCode:errorDescription:attributes:)
    public static func errorWithError(loggerId: String, message: String, errorDomain: String, errorCode: Int, errorDescription: String, attributes: [String: Any]) {
        let err = NSError(domain: errorDomain, code: errorCode, userInfo: [NSLocalizedDescriptionKey: errorDescription])
        withLogger(loggerId) { $0.error(message, error: err, attributes: makeEncodableDict(attributes)) }
    }

    @objc(criticalLogWithError:message:errorDomain:errorCode:errorDescription:attributes:)
    public static func criticalWithError(loggerId: String, message: String, errorDomain: String, errorCode: Int, errorDescription: String, attributes: [String: Any]) {
        let err = NSError(domain: errorDomain, code: errorCode, userInfo: [NSLocalizedDescriptionKey: errorDescription])
        withLogger(loggerId) { $0.critical(message, error: err, attributes: makeEncodableDict(attributes)) }
    }

    // MARK: - Logger Attributes & Tags

    @objc(addAttributeForLogger:key:value:)
    public static func addAttribute(loggerId: String, key: String, value: String) {
        withLogger(loggerId) { $0.addAttribute(forKey: key, value: value) }
    }

    @objc(removeAttributeForLogger:key:)
    public static func removeAttribute(loggerId: String, key: String) {
        withLogger(loggerId) { $0.removeAttribute(forKey: key) }
    }

    @objc(addTagForLogger:key:value:)
    public static func addTag(loggerId: String, key: String, value: String) {
        withLogger(loggerId) { $0.addTag(withKey: key, value: value) }
    }

    @objc(removeTagForLogger:key:)
    public static func removeTag(loggerId: String, key: String) {
        withLogger(loggerId) { $0.removeTag(withKey: key) }
    }

    // MARK: - Private

    private static func withLogger(_ id: String, body: (any LoggerProtocol) -> Void) {
        guard let logger = lock.withLock({ loggers[id] }) else { return }
        body(logger)
    }
}
