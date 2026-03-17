# iOS Real-Device Crash Fixes

Branch: `fix/session-replay-granular-privacy`
NuGet version: `3.6.2-preview.4`
Target: iPhone 16 Pro Max (JMFTechHS), iOS 26.3.1, `net10.0-ios`

---

## Summary

Three distinct crashes prevented the app from launching on a real iOS device. Each was caused by a different root issue; fixes were applied in order as each was uncovered.

---

## Crash 1: DYLD `__objc_classrefs` — Swift Class Stub Resolution Failure

### Symptom

App terminated at launch with a DYLD error:

```
dyld: missing weak superclass for class _TtC18DatadogSessionReplay25objc_SessionReplayConfiguration
```

or variant forms like:

```
objc[...]: Class DDConfiguration is implemented in both ... one of the two will be used
```

The process never reached `main()`.

### Root Cause

Swift frameworks compiled **with library evolution** (`BUILD_LIBRARY_FOR_DISTRIBUTION=YES`) emit class stubs into `__objc_stublist` rather than `__objc_classlist`. When the .NET static registrar generates `__objc_classrefs` entries for these types (from `ApiDefinition.cs` binding declarations), dyld cannot resolve them at launch — `__objc_classrefs` lookups require entries in `__objc_classlist`, which stubs never satisfy.

Affected types across the Datadog iOS SDK:

| Framework | Stub types removed from ApiDefinition.cs |
|---|---|
| `DatadogCore` | `DDConfiguration`, `DDSite`, `DDTrackingConsent`, `DDDatadog`, `DDURLSessionInstrumentation`, `DDURLSessionInstrumentationConfiguration`, `DDURLSessionInstrumentationFirstPartyHostsTracing` |
| `DatadogRUM` | `DDRUMConfiguration`, `DDRUM` |
| `DatadogTrace` | `DDTrace`, `DDTraceConfiguration`, `DDTraceFirstPartyHostsTracing`, `DDTraceURLSessionTracking` |
| `DatadogLogs` | `DDLogEvent` (stub class), `SetEventMapper` method |
| `DatadogSessionReplay` | `DDSessionReplay`, `DDSessionReplayConfiguration` |

### Fix

**Part A — Remove stub types from all binding ApiDefinition.cs files**

All ObjC binding declarations for Swift class stubs were removed from:
- `Datadog.MAUI.iOS.Binding/DatadogCore/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogRUM/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogTrace/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogLogs/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogSessionReplay/ApiDefinition.cs`

Retained bindings: only types confirmed to be in `__objc_classlist` (proper ObjC classes, not Swift stubs), verified via `nm -m` on the framework binaries.

**Part B — Add DatadogWrapper.xcframework**

A new Swift static library (`native-wrappers/ios/DatadogWrapper/`) was created and compiled **without** library evolution (`BUILD_LIBRARY_FOR_DISTRIBUTION=NO`). Its `@objc` classes land in `__objc_classlist` and are resolvable at launch.

The wrapper provides safe ObjC-callable entry points for all functionality previously accessed through stub types:

| Wrapper class | Replaces |
|---|---|
| `DDWrapperCore` | `DDDatadog`, `DDConfiguration` |
| `DDWrapperRUM` | `DDRUM`, `DDRUMConfiguration` |
| `DDWrapperLogs` | `DDLogs`, `DDLogsConfiguration` (enable only) |
| `DDWrapperTrace` | `DDTrace`, `DDTraceConfiguration` |
| `DDWrapperSessionReplay` | `DDSessionReplay`, `DDSessionReplayConfiguration` |

**Part C — Update `.targets` file**

`Datadog.MAUI.Plugin/build/Datadog.MAUI.targets` was updated to propagate `-Wl,-U` linker flags via `buildTransitive` and to remove DD* class stub symbols from the exported symbols list at link time.

**Files changed:**
- `Datadog.MAUI.iOS.Binding/DatadogCore/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogRUM/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogTrace/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogLogs/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogSessionReplay/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/DatadogWrapper/` *(new project)*
- `native-wrappers/ios/DatadogWrapper/` *(new Swift package)*
- `Datadog.MAUI.Plugin/build/Datadog.MAUI.targets`

---

## Crash 2: `System.TypeLoadException` — DDSessionReplayConfiguration at Runtime

### Symptom

After the DYLD crash was fixed, the app crashed ~3 seconds after launch with:

```
System.TypeLoadException: Could not resolve type with token 010002c0 from typeref
(expected class 'Datadog.iOS.SessionReplay.DDSessionReplayConfiguration'
in assembly 'Datadog.MAUI.iOS.SessionReplay, ...')
   at MG365Mobile.UI.MauiProgram.CreateMauiApp()  (MauiProgram.cs:line 230)
```

Stack: `xamarin_process_managed_exception` → `willFinishLaunchingWithOptions` → `CreateMauiApp()`

### Root Cause

`MauiProgram.cs` called the removed stub types directly:

```csharp
// MauiProgram.cs:329 — inside EnableSessionReplay()
var srConfig = new Datadog.iOS.SessionReplay.DDSessionReplayConfiguration(
    sampleRate,
    Datadog.iOS.SessionReplay.DDTextAndInputPrivacyLevel.SensitiveInputs,
    Datadog.iOS.SessionReplay.DDImagePrivacyLevel.NonBundledOnly,
    Datadog.iOS.SessionReplay.DDTouchPrivacyLevel.Show);
srConfig.StartRecordingImmediately = true;
Datadog.iOS.SessionReplay.DDSessionReplay.EnableWith(srConfig);
```

`DDSessionReplayConfiguration` and `DDSessionReplay` were removed from the binding (Crash 1 fix), but the consumer code still referenced them. The surrounding `try/catch` did not help because `TypeLoadException` is thrown at AOT method-preparation time, before the try block is entered.

### Fix

**`DDWrapperSessionReplay` — new granular enable method**

Added a new overload to `DDWrapperSessionReplay.swift` that accepts the individual privacy enum raw values and `startRecordingImmediately`, mapping them to the Swift `SessionReplay.Configuration` struct internally:

```swift
// native-wrappers/ios/DatadogWrapper/Sources/DatadogWrapper/DDWrapperSessionReplay.swift
@objc(enableWithSampleRate:textAndInputPrivacy:imagePrivacy:touchPrivacy:startRecordingImmediately:)
public static func enable(sampleRate: Float,
                          textAndInputPrivacy: Int,
                          imagePrivacy: Int,
                          touchPrivacy: Int,
                          startRecordingImmediately: Bool)
```

Integer values map to the existing `DDTextAndInputPrivacyLevel`, `DDImagePrivacyLevel`, `DDTouchPrivacyLevel` ObjC enum raw values (which are proper ObjC enums in `__objc_classlist`, safe to use).

**`DatadogWrapper/ApiDefinition.cs` — expose the new overload**

```csharp
[Static]
[Export("enableWithSampleRate:textAndInputPrivacy:imagePrivacy:touchPrivacy:startRecordingImmediately:")]
void Enable(float sampleRate, nint textAndInputPrivacy, nint imagePrivacy, nint touchPrivacy, bool startRecordingImmediately);
```

**`MauiProgram.cs` — replace stub call with wrapper call**

```csharp
// Before
var srConfig = new Datadog.iOS.SessionReplay.DDSessionReplayConfiguration(...);
srConfig.StartRecordingImmediately = true;
Datadog.iOS.SessionReplay.DDSessionReplay.EnableWith(srConfig);

// After
DatadogWrapper.DDWrapperSessionReplay.Enable(
    sampleRate,
    (nint)Datadog.iOS.SessionReplay.DDTextAndInputPrivacyLevel.SensitiveInputs,
    (nint)Datadog.iOS.SessionReplay.DDImagePrivacyLevel.NonBundledOnly,
    (nint)Datadog.iOS.SessionReplay.DDTouchPrivacyLevel.Show,
    startRecordingImmediately: true);
```

**Files changed:**
- `native-wrappers/ios/DatadogWrapper/Sources/DatadogWrapper/DDWrapperSessionReplay.swift`
- `Datadog.MAUI.iOS.Binding/DatadogWrapper/ApiDefinition.cs`
- `MG365Mobile.UI/MauiProgram.cs` *(consumer app)*

---

## Crash 3: `InvalidOperationException` — ServiceCollection Read-Only After Build

### Symptom

After Crash 2 was fixed, the app crashed during startup with:

```
System.InvalidOperationException: ServiceCollection is read-only
   at ServiceCollection.ThrowReadOnlyException()
   at LoggingServiceCollectionExtensions.AddLogging(...)
   at MauiProgram.CreateMauiApp()  (MauiProgram.cs:233)
```

### Root Cause

`builder.Logging.AddDebug()` was called after `builder.Build()` had already been invoked. Once `Build()` seals the service container, any attempt to register new services throws.

This bug was pre-existing but was masked by Crash 2 — the `TypeLoadException` aborted execution before this line was ever reached.

```csharp
MauiApp mauiApp = builder.Build();   // line 201 — seals the container
// ...
EnableSessionReplay(100);            // line 230 — now succeeds
#if DEBUG
builder.Logging.AddDebug();          // line 233 — InvalidOperationException
#endif
```

### Fix

Moved `builder.Logging.AddDebug()` to before `builder.Build()`:

```csharp
// MauiProgram.cs
#if DEBUG
builder.Logging.AddDebug();    // ← moved here, before Build()
#endif

MauiApp mauiApp = builder.Build();
```

**Files changed:**
- `MG365Mobile.UI/MauiProgram.cs` *(consumer app)*

---

## Version History

| Version | Change |
|---|---|
| `3.6.2-preview.1` | Initial preview with DatadogWrapper Swift layer |
| `3.6.2-preview.2` | ApiDefinition cleanup (stub types removed) |
| `3.6.2-preview.3` | NuGet cache invalidation bump |
| `3.6.2-preview.4` | Granular `DDWrapperSessionReplay.Enable` overload |

---

## How to Pull Device Crash Logs

```bash
# Collect last N minutes of device logs
sudo /usr/bin/log collect \
  --device-udid 00008140-000A01401A90801C \
  --last 10m \
  --output /tmp/device-log.logarchive

# Filter for app output
/usr/bin/log show /tmp/device-log.logarchive \
  --predicate 'process == "EcolabEveryDay"' \
  --style syslog \
  --start "2026-03-16 12:00:00"

# Pull crash logs from device (synced archive)
xcrun devicectl device copy from \
  --device 3A109E4C-8E85-523C-A903-6CDFD720359A \
  --domain-type systemCrashLogs \
  --source /Retired \
  --destination /tmp/device-crashes/
```
