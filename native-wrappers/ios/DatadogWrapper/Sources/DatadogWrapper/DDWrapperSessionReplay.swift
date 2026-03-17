import Foundation
import DatadogSessionReplay
import DatadogInternal

@objc(DDWrapperSessionReplay)
public class DDWrapperSessionReplay: NSObject {

    /// privacyLevel: "allow" | "mask" | "maskUserInput"
    @objc(enableWithSampleRate:privacyLevel:)
    public static func enable(sampleRate: Float, privacyLevel: String) {
        let (textLevel, imageLevel, touchLevel) = parsePrivacyLevel(privacyLevel)
        let config = SessionReplay.Configuration(
            replaySampleRate: sampleRate,
            textAndInputPrivacyLevel: textLevel,
            imagePrivacyLevel: imageLevel,
            touchPrivacyLevel: touchLevel
        )
        SessionReplay.enable(with: config)
    }

    /// Granular privacy control with individual levels for text/input, image, and touch.
    /// textAndInputPrivacy: DDTextAndInputPrivacyLevel raw value (0=maskSensitiveInputs, 1=maskAllInputs, 2=maskAll)
    /// imagePrivacy:        DDImagePrivacyLevel raw value (0=maskNonBundledOnly, 1=maskAll, 2=maskNone)
    /// touchPrivacy:        DDTouchPrivacyLevel raw value (0=show, 1=hide)
    @objc(enableWithSampleRate:textAndInputPrivacy:imagePrivacy:touchPrivacy:startRecordingImmediately:)
    public static func enable(sampleRate: Float,
                              textAndInputPrivacy: Int,
                              imagePrivacy: Int,
                              touchPrivacy: Int,
                              startRecordingImmediately: Bool) {
        let textLevel: TextAndInputPrivacyLevel
        switch textAndInputPrivacy {
        case 0:  textLevel = .maskSensitiveInputs
        case 1:  textLevel = .maskAllInputs
        default: textLevel = .maskAll
        }

        let imageLevel: ImagePrivacyLevel
        switch imagePrivacy {
        case 0:  imageLevel = .maskNonBundledOnly
        case 2:  imageLevel = .maskNone
        default: imageLevel = .maskAll
        }

        let touchLevel: TouchPrivacyLevel = touchPrivacy == 0 ? .show : .hide

        var config = SessionReplay.Configuration(
            replaySampleRate: sampleRate,
            textAndInputPrivacyLevel: textLevel,
            imagePrivacyLevel: imageLevel,
            touchPrivacyLevel: touchLevel
        )
        config.startRecordingImmediately = startRecordingImmediately
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
