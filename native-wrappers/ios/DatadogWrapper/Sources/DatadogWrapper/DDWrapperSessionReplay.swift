import Foundation
import DatadogSessionReplay
import DatadogInternal

@objc(DDWrapperSessionReplay)
public class DDWrapperSessionReplay: NSObject {

    /// privacyLevel: "allow" | "mask" | "maskUserInput"
    @objc(enableWithSampleRate:privacyLevel:)
    public static func enable(sampleRate: Float, privacyLevel: String) {
        // In dd-sdk-ios 3.x, SessionReplay.Configuration uses separate privacy levels
        // for text/input, image, and touch.
        let (textLevel, imageLevel, touchLevel) = parsePrivacyLevel(privacyLevel)
        let config = SessionReplay.Configuration(
            replaySampleRate: sampleRate,
            textAndInputPrivacyLevel: textLevel,
            imagePrivacyLevel: imageLevel,
            touchPrivacyLevel: touchLevel
        )
        SessionReplay.enable(with: config)
    }

    private static func parsePrivacyLevel(_ level: String) -> (TextAndInputPrivacyLevel, ImagePrivacyLevel, TouchPrivacyLevel) {
        switch level.lowercased() {
        case "allow":
            return (.maskSensitiveInputs, .maskNone, .show)
        case "mask":
            return (.maskAll, .maskAll, .hide)
        case "maskuserinput", "mask_user_input":
            return (.maskAllInputs, .maskNone, .hide)
        default:
            return (.maskAll, .maskAll, .hide)
        }
    }
}
