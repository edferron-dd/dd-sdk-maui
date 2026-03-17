# Session Replay Guide

This guide covers how to enable and control Session Replay in your .NET MAUI app using the `Datadog.MAUI` SDK.

---

## Overview

Session Replay captures a visual record of user interactions for playback in the Datadog dashboard. You can enable it at SDK initialization time and then control recording programmatically with `StartRecording()` and `StopRecording()`.

---

## Enabling Session Replay

Call `EnableSessionReplay` during app startup (e.g., `MauiProgram.cs`), after the SDK is initialized.

```csharp
#if ANDROID
var srConfig = new Datadog.Android.SessionReplay.SessionReplayConfiguration.Builder(sampleRate: 100f)
    .SetTextAndInputPrivacy(Datadog.Android.SessionReplay.TextAndInputPrivacy.MaskSensitiveInputs!)
    .SetImagePrivacy(Datadog.Android.SessionReplay.ImagePrivacy.MaskLargeOnly!)
    .SetTouchPrivacy(Datadog.Android.SessionReplay.TouchPrivacy.Show!)
    .StartRecordingImmediately(false)   // set to false to control recording manually
    .Build();
Datadog.Android.SessionReplay.SessionReplay.Enable(srConfig);

#elif IOS
var srConfig = new Datadog.iOS.SessionReplay.DDSessionReplayConfiguration(
    replaySampleRate: 100f,
    textAndInputPrivacyLevel: Datadog.iOS.SessionReplay.DDTextAndInputPrivacyLevel.SensitiveInputs,
    imagePrivacyLevel: Datadog.iOS.SessionReplay.DDImagePrivacyLevel.NonBundledOnly,
    touchPrivacyLevel: Datadog.iOS.SessionReplay.DDTouchPrivacyLevel.Show);
srConfig.StartRecordingImmediately = false;   // set to false to control recording manually
Datadog.iOS.SessionReplay.DDSessionReplay.EnableWith(srConfig);
#endif
```

---

## Controlling Recording with StartRecording and StopRecording

Use the cross-platform `Datadog.Maui.SessionReplay.SessionReplay` API to start and stop recording at any point after Session Replay is enabled.

```csharp
using Datadog.Maui.SessionReplay;

// Start recording (e.g., when user enters a key flow)
SessionReplay.StartRecording();

// Stop recording (e.g., on a sensitive screen such as payment entry)
SessionReplay.StopRecording();

// Resume recording when the sensitive screen is dismissed
SessionReplay.StartRecording();
```

### Typical Usage Pattern

```csharp
public partial class CheckoutPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Stop recording on sensitive payment screen
        SessionReplay.StopRecording();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Resume recording when leaving payment screen
        SessionReplay.StartRecording();
    }
}
```

---

## Implementation Plan

### Goals
Add `StartRecording()` and `StopRecording()` methods to the `dd-sdk-maui` .NET MAUI plugin so developers can control session recording without directly touching the native Android or iOS SDK bindings.

### Architecture

```
Datadog.Maui.SessionReplay.SessionReplay   (cross-platform API)
    ├── Platforms/iOS/SessionReplay.ios.cs  → DDWrapperSessionReplay.StartRecording()
    │                                         DDWrapperSessionReplay.StopRecording()
    │                                           ↓
    │                                         native-wrappers/ios/.../DDWrapperSessionReplay.swift
    │                                           ↓
    │                                         iOS SessionReplay.startRecording()
    │                                         iOS SessionReplay.stopRecording()
    │
    └── Platforms/Android/SessionReplay.android.cs
                                              → Datadog.Android.SessionReplay.SessionReplay.Instance
                                                  .StartRecording(Datadog.Android.Datadog.Instance)
                                                  .StopRecording(Datadog.Android.Datadog.Instance)
```

### Files Changed

| File | Change |
|------|--------|
| `native-wrappers/ios/DatadogWrapper/Sources/DatadogWrapper/DDWrapperSessionReplay.swift` | Added `startRecording()` and `stopRecording()` static methods that delegate to the native iOS `SessionReplay` API |
| `Datadog.MAUI.iOS.Binding/DatadogWrapper/ApiDefinition.cs` | Added ObjC binding declarations for `startRecording` and `stopRecording` on `DDWrapperSessionReplay` |
| `Datadog.MAUI.Plugin/SessionReplay/SessionReplay.cs` | New cross-platform `SessionReplay` static partial class with `StartRecording()` and `StopRecording()` |
| `Datadog.MAUI.Plugin/Platforms/iOS/SessionReplay.ios.cs` | iOS platform implementation calling `DDWrapperSessionReplay` |
| `Datadog.MAUI.Plugin/Platforms/Android/SessionReplay.android.cs` | Android platform implementation using `Datadog.Android.SessionReplay.SessionReplay.Instance` with `Datadog.Android.Datadog.Instance` as the SdkCore |
| `docs/guides/session-replay.md` | This documentation file |

### Notes

- **Android**: `StartRecording` and `StopRecording` are instance methods on the `SessionReplay` Kotlin object (singleton). They require an `ISdkCore` argument, which is obtained via `Datadog.Android.Datadog.Instance` — the default initialized core.
- **iOS**: The native `DDSessionReplay` already exposes `+startRecording` and `+stopRecording` as static Objective-C methods. The Swift wrapper (`DDWrapperSessionReplay`) bridges these to the bound C# API.
- **No new native SDK dependencies**: Both Android and iOS native SDKs already support these methods. The work is purely binding/wrapper additions.

---

## Privacy Levels Reference

| Level | Android | iOS |
|-------|---------|-----|
| Mask sensitive inputs only | `TextAndInputPrivacy.MaskSensitiveInputs` | `DDTextAndInputPrivacyLevel.SensitiveInputs` |
| Mask all inputs | `TextAndInputPrivacy.MaskAllInputs` | `DDTextAndInputPrivacyLevel.AllInputs` |
| Mask everything | `TextAndInputPrivacy.MaskAll` | `DDTextAndInputPrivacyLevel.All` |
| Mask large images | `ImagePrivacy.MaskLargeOnly` | `DDImagePrivacyLevel.NonBundledOnly` |
| Show touches | `TouchPrivacy.Show` | `DDTouchPrivacyLevel.Show` |
| Hide touches | `TouchPrivacy.Hide` | `DDTouchPrivacyLevel.Hide` |
