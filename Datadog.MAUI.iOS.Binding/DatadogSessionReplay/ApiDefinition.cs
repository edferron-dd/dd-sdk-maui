using Foundation;
using ObjCRuntime;
using UIKit;

namespace Datadog.iOS.SessionReplay
{
	// @interface DatadogSessionReplay_Swift_818 (UIView)
	// Note: Category properties don't work in .NET 9/10 binding generator
	// This extension has been commented out - functionality may be accessed through other means
	// [Category]
	// [BaseType (typeof(UIView))]
	// interface UIView_DatadogSessionReplay_Swift_818
	// {
	// 	// @property (readonly, nonatomic, strong) DDSessionReplayPrivacyOverrides * _Nonnull ddSessionReplayPrivacyOverrides;
	// 	[Export ("ddSessionReplayPrivacyOverrides")]
	// 	DDSessionReplayPrivacyOverrides DdSessionReplayPrivacyOverrides { get; }
	// }

	// NOTE: DDSessionReplay and DDSessionReplayConfiguration have been removed.
	// DDSessionReplayConfiguration is a Swift class stub (__objc_stublist) that
	// dyld cannot resolve at launch. Session replay goes through DDWrapperSessionReplay.

	// @interface DDSessionReplayPrivacyOverrides : NSObject
	[BaseType (typeof(NSObject))]
	[DisableDefaultCtor]
	interface DDSessionReplayPrivacyOverrides
	{
		// -(instancetype _Nonnull)initWithView:(UIView * _Nonnull)view __attribute__((objc_designated_initializer));
		[Export ("initWithView:")]
		[DesignatedInitializer]
		NativeHandle Constructor (UIView view);

		// @property (nonatomic) enum DDTextAndInputPrivacyLevelOverride textAndInputPrivacy;
		[Export ("textAndInputPrivacy", ArgumentSemantic.Assign)]
		DDTextAndInputPrivacyLevelOverride TextAndInputPrivacy { get; set; }

		// @property (nonatomic) enum DDImagePrivacyLevelOverride imagePrivacy;
		[Export ("imagePrivacy", ArgumentSemantic.Assign)]
		DDImagePrivacyLevelOverride ImagePrivacy { get; set; }

		// @property (nonatomic) enum DDTouchPrivacyLevelOverride touchPrivacy;
		[Export ("touchPrivacy", ArgumentSemantic.Assign)]
		DDTouchPrivacyLevelOverride TouchPrivacy { get; set; }

		// @property (nonatomic, strong) NSNumber * _Nullable hide;
		[NullAllowed, Export ("hide", ArgumentSemantic.Strong)]
		NSNumber Hide { get; set; }
	}
}
