import Foundation
import DatadogTrace
import DatadogInternal

@objc(DDWrapperTrace)
public class DDWrapperTrace: NSObject {

    private static let lock = NSLock()
    private static var spans: [String: any OTSpan] = [:]

    // MARK: - Enable

    @objc(enableWithSampleRate:)
    public static func enable(sampleRate: Float) {
        Trace.enable(with: Trace.Configuration(sampleRate: sampleRate))
    }

    // MARK: - Spans

    @objc(startSpanWithOperationName:parentSpanId:)
    public static func startSpan(operationName: String, parentSpanId: String?) -> String {
        let id = UUID().uuidString
        let tracer = Tracer.shared()
        let span: any OTSpan

        if let parentId = parentSpanId,
           let parentSpan = lock.withLock({ spans[parentId] }) {
            span = tracer.startSpan(operationName: operationName, childOf: parentSpan.context)
        } else {
            span = tracer.startSpan(operationName: operationName)
        }

        lock.withLock { spans[id] = span }
        return id
    }

    @objc(finishSpanWithId:)
    public static func finishSpan(spanId: String) {
        lock.withLock {
            spans[spanId]?.finish()
            spans.removeValue(forKey: spanId)
        }
    }

    // MARK: - Tags (OTSpan.setTag only has key:value: in 3.x, value is any Encodable)

    @objc(setStringTagForSpan:key:value:)
    public static func setStringTag(spanId: String, key: String, value: String) {
        lock.withLock { spans[spanId] }?.setTag(key: key, value: value)
    }

    @objc(setBoolTagForSpan:key:value:)
    public static func setBoolTag(spanId: String, key: String, value: Bool) {
        lock.withLock { spans[spanId] }?.setTag(key: key, value: value)
    }

    @objc(setNumberTagForSpan:key:value:)
    public static func setNumberTag(spanId: String, key: String, value: NSNumber) {
        lock.withLock { spans[spanId] }?.setTag(key: key, value: value.doubleValue)
    }

    // MARK: - Errors

    @objc(setErrorMessageForSpan:message:)
    public static func setError(spanId: String, message: String) {
        guard let span = lock.withLock({ spans[spanId] }) else { return }
        span.setTag(key: "error", value: true)
        span.setTag(key: "error.message", value: message)
    }

    @objc(setNSErrorForSpan:error:)
    public static func setNSError(spanId: String, error: NSError) {
        lock.withLock { spans[spanId] }?.setError(error)
    }

    // MARK: - Events

    @objc(logEventForSpan:name:)
    public static func logEvent(spanId: String, name: String) {
        lock.withLock { spans[spanId] }?.log(fields: ["event": name])
    }

    // MARK: - Header Injection

    /// Returns Datadog distributed tracing headers for the given span.
    @objc(injectHeadersForSpan:)
    public static func injectHeaders(spanId: String) -> NSDictionary {
        guard let span = lock.withLock({ spans[spanId] }) else { return NSDictionary() }
        let tracer = Tracer.shared()
        let writer = HTTPHeadersWriter(traceContextInjection: .sampled)
        tracer.inject(spanContext: span.context, writer: writer)
        return writer.traceHeaderFields as NSDictionary
    }
}
