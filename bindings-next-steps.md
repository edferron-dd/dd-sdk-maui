# iOS Bindings: Next Steps

## Current State

The binding generation script (`scripts/generate-ios-bindings-sharpie.sh`) has been
updated to work with Xcode 26.2 and produces fresh `ApiDefinitions.cs` +
`StructsAndEnums.cs` files from the Datadog iOS xcframeworks.

**Script run results (Xcode 26.2, Sharpie 3.5, SDK iphoneos26.2):**

| Framework | Status | Notes |
|-----------|--------|-------|
| DatadogCore | OK (323 lines) | Includes DDConfiguration, DDSite, DDTrackingConsent, DDDatadog, etc. |
| DatadogInternal | OK (50 lines) | DDInternalLogger, DDTracingHeaderType |
| DatadogRUM | OK (6639 lines) | Large API surface |
| DatadogLogs | OK (497 lines) | DDLogEvent, DDLogger, DDLogs, etc. |
| DatadogTrace | OK (325 lines) | DDTrace, DDTraceConfiguration, DDTracer, etc. |
| DatadogCrashReporting | OK (13 lines) | DDCrashReporter only |
| DatadogSessionReplay | OK (98 lines) | DDSessionReplay, DDSessionReplayConfiguration, etc. |
| DatadogWebViewTracking | OK (20 lines) | DDWebViewTracking |
| DatadogFlags | Failed | No ObjC-bindable types (Swift-only constants) |
| OpenTelemetryApi | Skipped | Pure Swift module with no ObjC headers |

## Root Cause of the iOS Linker / Runtime Crash

The Datadog iOS SDK xcframeworks are compiled with **Swift library evolution**
(`-enable-library-evolution`).  This means certain ObjC-visible classes are
emitted as **class stubs** (`__objc_stublist`) rather than regular ObjC class
definitions (`__objc_classlist`).

Classes affected (have `_OBJC_METACLASS_$_` but no `_OBJC_CLASS_$_`):

- **DatadogCore**: DDConfiguration, DDSite, DDTrackingConsent,
  DDURLSessionInstrumentationConfiguration,
  DDURLSessionInstrumentationFirstPartyHostsTracing
- **DatadogRUM**: DDRUMConfiguration
- **DatadogTrace**: DDTraceConfiguration, DDTraceFirstPartyHostsTracing,
  DDTraceURLSessionTracking
- **DatadogLogs**: DDLogEvent
- **DatadogSessionReplay**: DDSessionReplayConfiguration

The .NET iOS build system generates `registrar.mm` / `registrar.o` with
`[DDConfiguration class]` syntax that compiles to direct
`_OBJC_CLASS_$_DDConfiguration` symbol references.  These symbols don't exist
in class-stub frameworks, causing:

1. **Linker error**: "Undefined symbols for architecture arm64"
2. **dyld crash**: When `-Wl,-U` suppresses the linker error, dyld still can't
   resolve the symbols at load time
3. **Runtime nil class**: When `registrar.mm` is patched to use
   `objc_getClass()` / `NSClassFromString()`, these return `nil` because the
   ObjC runtime's class stub resolution apparently does not make the class
   available by name in this context

## Recommended Next Steps

### 1. Regenerate bindings from fresh Sharpie output

The existing `ApiDefinition.cs` files in each sub-project were likely generated
with a different Xcode version or via the older `generate-ios-bindings.sh` script
(which used a different namespace convention and may have fallen back to umbrella
headers).

**Action:**
```bash
# Generate fresh bindings
./scripts/generate-ios-bindings-sharpie.sh

# Compare generated output with existing bindings
diff Datadog.MAUI.iOS.Binding/Generated/DatadogCore/ApiDefinitions.cs \
     Datadog.MAUI.iOS.Binding/DatadogCore/ApiDefinition.cs
```

Review each framework's generated output against its existing `ApiDefinition.cs`
and reconcile differences.  Pay attention to:

- Missing classes or methods in the current bindings
- `[Verify]` attributes that indicate Sharpie uncertainty
- `[BaseType]` annotations that may need to be added
- `NativeHandle Constructor` patterns

### 2. Investigate the class stub runtime resolution failure

`objc_getClass("DDConfiguration")` and `NSClassFromString(@"DDConfiguration")`
both return `nil` at runtime despite the DatadogCore.framework being loaded and
containing valid `__objc_stublist` entries.

**Possible causes to investigate:**

- **iOS 26 beta regression**: Class stub resolution may behave differently on
  this beta.  Test on an iOS 18.x device if available.
- **Framework loading order**: The `registrar.mm` class map initialization may
  run before the DatadogCore framework's `__objc_stublist` entries are processed
  by the ObjC runtime.
- **Code signing invalidation**: The `_ResignNativeFrameworks` target re-signs
  embedded frameworks with ad-hoc signatures.  This may corrupt class stub
  metadata.
- **`-exported_symbols_list` interaction**: The mtouch-symbols.list may
  interfere with how dyld processes class stubs from dynamic frameworks.

**Diagnostic steps:**

```bash
# Verify stubs exist in embedded framework
otool -l EcolabEveryDay.app/Frameworks/DatadogCore.framework/DatadogCore \
  | grep -A4 stublist

# Check if a simple ObjC test app can resolve class stubs from the same framework
# (isolates the issue to .NET iOS vs the framework itself)

# Try building without the _ResignNativeFrameworks target to rule out signing
```

### 3. Consider alternative approaches to the class stub problem

If class stubs cannot be resolved at runtime, these alternatives should be
evaluated:

#### Option A: Provide class symbols via a static ObjC shim library

Create a small `.m` file with `__attribute__((constructor))` that forces class
stub resolution at framework load time and stores the results:

```objc
#import <objc/runtime.h>

// Force class stub resolution by referencing the metaclass
__attribute__((constructor))
static void _DatadogResolveClassStubs(void) {
    // The act of calling objc_getMetaClass triggers stub resolution
    // for classes in __objc_stublist
    (void)objc_getMetaClass("DDConfiguration");
    (void)objc_getMetaClass("DDSite");
    // ... etc
}
```

Ship this as an `ObjcBindingNativeLibrary` in the DatadogCore binding project.

#### Option B: Downgrade Datadog iOS SDK

Check if an older version of the Datadog iOS SDK was compiled without
`-enable-library-evolution`.  Versions prior to the Swift 5.1 adoption may not
use class stubs.

#### Option C: Build Datadog iOS SDK from source without library evolution

Fork the Datadog iOS SDK, remove `-enable-library-evolution` from the Swift
compiler flags, and rebuild the xcframeworks.  This would produce standard
`_OBJC_CLASS_$_` symbols for all classes.

#### Option D: Use the Datadog iOS SDK's Swift Package Manager integration

Instead of binding the xcframeworks, use the native Swift PM packages directly
via .NET MAUI's Swift interop (experimental).  This avoids the ObjC binding
layer entirely.

### 4. Clean up deprecated script

The `scripts/generate-ios-bindings.sh` script uses a different namespace
convention (`DatadogMaui.iOS.$FRAMEWORK` vs `Datadog.iOS.$FRAMEWORK`) and
should be either removed or clearly marked as deprecated to prevent confusion.

### 5. Remove unused binding types

The class stub audit identified 5 types that are bound but never used in the
plugin code:

- DDURLSessionInstrumentationConfiguration
- DDURLSessionInstrumentationFirstPartyHostsTracing
- DDTraceFirstPartyHostsTracing
- DDTraceURLSessionTracking
- DDLogEvent

Removing these from the binding `ApiDefinition.cs` files would:
- Reduce the linker surface (fewer class stubs to work around)
- Simplify the `_RemoveDatadogSwiftClassStubSymbols` target
- Reduce binary size

### 6. Update RepositoryUrl in all csproj files

All binding `.csproj` files currently point to `https://github.com/DataDog/dd-sdk-maui`.
Update to point to the correct fork: `https://github.com/kyletaylored/dd-sdk-maui`.
