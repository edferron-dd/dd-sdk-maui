import Foundation
import DatadogCore

/// ObjC-visible wrapper for Datadog Core initialization.
///
/// All dd-sdk-ios Swift class stubs (DDConfiguration, DDSite, DDTrackingConsent, …)
/// are used entirely within Swift code here.  The C# binding only sees this plain
/// NSObject subclass, which lives in __objc_classlist — not __objc_stublist —
/// because this library is compiled WITHOUT Swift Library Evolution.
@objc(DDWrapperCore)
public class DDWrapperCore: NSObject {

    // MARK: - Initialize

    @objc(initializeWithClientToken:env:site:service:trackingConsent:verbose:)
    public static func initialize(
        clientToken: String,
        env: String,
        site: String,
        service: String?,
        trackingConsent: String,
        verbose: Bool
    ) {
        var config = Datadog.Configuration(
            clientToken: clientToken,
            env: env,
            site: parseSite(site)
        )
        config.service = service

        Datadog.initialize(with: config, trackingConsent: parseConsent(trackingConsent))

        if verbose {
            Datadog.verbosityLevel = .debug
        }
    }

    // MARK: - Tracking Consent

    @objc(setTrackingConsent:)
    public static func setTrackingConsent(_ consent: String) {
        Datadog.set(trackingConsent: parseConsent(consent))
    }

    // MARK: - User Info

    @objc(setUserInfoWithUserId:name:email:extraInfo:)
    public static func setUserInfo(
        userId: String,
        name: String?,
        email: String?,
        extraInfo: [String: Any]
    ) {
        let encodable = makeEncodableDict(extraInfo)
        Datadog.setUserInfo(id: userId, name: name, email: email, extraInfo: encodable)
    }

    @objc(clearUserInfo)
    public static func clearUserInfo() {
        Datadog.clearUserInfo()
    }

    // MARK: - Private Helpers

    private static func parseSite(_ site: String) -> DatadogSite {
        switch site.lowercased() {
        case "us1":      return .us1
        case "us3":      return .us3
        case "us5":      return .us5
        case "eu1":      return .eu1
        case "us1_fed":  return .us1_fed
        case "ap1":      return .ap1
        default:         return .us1
        }
    }

    private static func parseConsent(_ consent: String) -> TrackingConsent {
        switch consent.lowercased() {
        case "granted":    return .granted
        case "notgranted": return .notGranted
        default:           return .pending
        }
    }
}
