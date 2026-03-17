using Foundation;
using ObjCRuntime;

namespace Datadog.iOS.DatadogCore
{
	//
	// NOTE: All types from DatadogCore have been intentionally removed.
	//
	// DDConfiguration, DDSite, DDTrackingConsent, DDDatadog, etc. are Swift value
	// types compiled with library evolution — they live in __objc_stublist (not
	// __objc_classlist). Binding them causes the static registrar to emit
	// _OBJC_CLASS_$_DD* references that dyld cannot satisfy at launch.
	//
	// DDCrossPlatformExtension, DDSharedContext, DDDataEncryption, and
	// DDServerDateProvider were removed because:
	//   1. They are not used by Datadog.MAUI.Plugin.
	//   2. Their Action<T> block parameters force Xamarin.iOS to generate
	//      XamarinSwiftFunctions trampoline dispatch entries. If the trampoline
	//      table isn't fully initialized before ObjC class registration runs
	//      (_UIApplicationMainPreparations), calling through it hits a null
	//      function pointer → EXC_BAD_ACCESS at PC=0x0.
	//
	// All initialization goes through DatadogWrapper (DDWrapperCore, DDWrapperRUM,
	// DDWrapperLogs, DDWrapperTrace, DDWrapperSessionReplay), so no bindings from
	// this framework are needed.
	//
}
