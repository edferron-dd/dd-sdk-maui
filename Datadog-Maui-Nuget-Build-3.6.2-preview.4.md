# Datadog.MAUI NuGet Build Guide — 3.6.2-preview.4

This document describes how to build and publish the `Datadog.MAUI` NuGet package family from source, including which steps are automated by scripts and which require manual action.

---

## Package Overview

The build produces packages in three dependency tiers:

```
Datadog.MAUI                          ← consumer-facing plugin (Tier 3)
├── Datadog.MAUI.iOS.Binding          ← iOS meta-package (Tier 2)
│   ├── Datadog.MAUI.iOS.Internal
│   ├── Datadog.MAUI.iOS.Core
│   ├── Datadog.MAUI.iOS.Logs
│   ├── Datadog.MAUI.iOS.RUM
│   ├── Datadog.MAUI.iOS.Trace
│   ├── Datadog.MAUI.iOS.CrashReporting
│   ├── Datadog.MAUI.iOS.SessionReplay
│   ├── Datadog.MAUI.iOS.WebViewTracking
│   ├── Datadog.MAUI.iOS.Flags
│   ├── Datadog.MAUI.iOS.OpenTelemetryApi
│   └── Datadog.MAUI.iOS.Wrapper        ← DatadogWrapper.xcframework (built from source)
└── Datadog.MAUI.Android.Binding       ← Android meta-package (Tier 2)
    ├── Datadog.MAUI.Android.Internal
    ├── Datadog.MAUI.Android.Core
    ├── Datadog.MAUI.Android.Logs
    ├── Datadog.MAUI.Android.RUM
    ├── Datadog.MAUI.Android.Trace
    ├── Datadog.MAUI.Android.NDK
    ├── Datadog.MAUI.Android.SessionReplay
    ├── Datadog.MAUI.Android.WebView
    ├── Datadog.MAUI.Android.Flags
    ├── Datadog.MAUI.Android.OkHttp
    ├── Datadog.MAUI.Android.Trace.OTel
    ├── Datadog.MAUI.Android.OkHttp.OTel
    └── Datadog.MAUI.Android.OpenTracing
```

---

## Prerequisites

| Requirement | Notes |
|---|---|
| macOS | Required for iOS builds. Android-only builds can run on Linux/Windows. |
| Xcode 15+ | `xcodebuild` must be in PATH. Xcode 26.x (beta) is used in this repo. |
| .NET SDK 9 or 10 | Both `net9.0-ios` and `net10.0-ios` targets are produced. |
| Android SDK | Required for Android binding compilation. |
| Objective Sharpie | Optional. Only needed when regenerating bindings from new framework headers. |

---

## Automation Status

### What Is Automated by Scripts

| Step | Script | Automated |
|---|---|---|
| Download Datadog iOS XCFrameworks from GitHub | `scripts/download-ios-frameworks.sh` | ✅ |
| Build DatadogWrapper.xcframework from Swift source | `scripts/build-ios-wrapper.sh` | ✅ |
| Build all Android and iOS module bindings | `scripts/build.sh` | ✅ |
| Validate project structure and dependencies | `scripts/validate-dependencies.sh` | ✅ |
| Pack all NuGet packages in dependency order | `scripts/pack.sh` | ✅ |
| Strip Swift class stub symbols from exported symbols list | MSBuild target in `Datadog.MAUI.targets` | ✅ |
| Propagate `-Wl,-U` weak-link flags via `buildTransitive` | MSBuild target in `Datadog.MAUI.targets` | ✅ |
| Preserve trimmer root assemblies | MSBuild target in `Datadog.MAUI.targets` | ✅ |

### What Requires Manual Action

| Step | Notes |
|---|---|
| Reviewing and editing `ApiDefinition.cs` files | Required when new iOS SDK versions introduce new or changed APIs. Sharpie output is a starting point only. |
| Identifying new Swift class stubs | When upgrading the iOS SDK, use `nm -m` on new framework binaries to check for new `__objc_stublist` entries and update `ApiDefinition.cs` and the stub symbol list in `Datadog.MAUI.targets` accordingly. |
| Bumping the version in `Directory.Build.props` | Must be done manually before each build to invalidate NuGet caches. |
| Updating `DatadogWrapper` Swift source | When new Datadog SDK functionality must be exposed via the wrapper (e.g., new session replay configuration options). |
| Publishing packages to NuGet.org | Must be done manually in dependency order (see below). |

---

## Build Steps

### Step 1 — Download iOS XCFrameworks

Downloads all Datadog iOS SDK XCFrameworks from GitHub releases into `Datadog.MAUI.iOS.Binding/artifacts/`.

```bash
./scripts/download-ios-frameworks.sh              # latest release
./scripts/download-ios-frameworks.sh 3.6.2        # specific version
```

Frameworks downloaded:
`DatadogCore`, `DatadogInternal`, `DatadogRUM`, `DatadogLogs`, `DatadogTrace`,
`DatadogCrashReporting`, `DatadogSessionReplay`, `DatadogWebViewTracking`,
`DatadogFlags`, `OpenTelemetryApi`

> **Note:** `DatadogWrapper.xcframework` is NOT downloaded — it is built from source in Step 2.

---

### Step 2 — Build DatadogWrapper.xcframework

Compiles the Swift wrapper package at `native-wrappers/ios/DatadogWrapper/` into a static XCFramework for both iOS device and simulator.

```bash
./scripts/build-ios-wrapper.sh           # incremental
./scripts/build-ios-wrapper.sh --clean   # clean rebuild
```

**Why this step exists:**
All Datadog iOS SDK frameworks are compiled with Swift library evolution (`BUILD_LIBRARY_FOR_DISTRIBUTION=YES`), which causes their `@objc` Swift classes to be emitted as stubs in `__objc_stublist`. The .NET static registrar generates `__objc_classrefs` references for these types, which dyld cannot resolve from stubs at launch.

`DatadogWrapper` is compiled **without** library evolution (`BUILD_LIBRARY_FOR_DISTRIBUTION=NO`), so its classes land in `__objc_classlist` and are fully resolvable at runtime. The wrapper calls into the Datadog SDK via Swift-to-Swift calls, bypassing the stub resolution problem entirely.

Output: `Datadog.MAUI.iOS.Binding/artifacts/DatadogWrapper.xcframework`

> **When to re-run:** Any time the Swift source in `native-wrappers/ios/DatadogWrapper/Sources/` is modified.

---

### Step 3 — Build All Module Bindings

Builds every Android and iOS module binding project and produces intermediate `.nupkg` files in `artifacts/`.

```bash
./scripts/build.sh           # Debug (default)
./scripts/build.sh Release   # Release
```

This script:
1. Builds each Android binding project (13 modules)
2. Builds each iOS binding project (11 modules, including `DatadogWrapper`)
3. Packs each module into `artifacts/` for use as a local NuGet source
4. Restores the full solution using `artifacts/` as the local source
5. Builds the iOS and Android meta-packages
6. Builds the `Datadog.MAUI.Plugin` consumer package

> **Requires:** Steps 1 and 2 must be complete. `artifacts/DatadogCore.xcframework` is used as the sentinel to detect whether iOS frameworks are present.

---

### Step 4 — Validate Dependencies (Optional)

Checks that all expected `PackageReference` entries are present in the meta-packages and consumer plugin.

```bash
./scripts/validate-dependencies.sh         # human-readable output
./scripts/validate-dependencies.sh json    # JSON output (for CI)
```

---

### Step 5 — Pack All NuGet Packages

Creates the final `.nupkg` files in `artifacts/` in the correct dependency order.

```bash
./scripts/pack.sh                          # Release to ./artifacts (default)
./scripts/pack.sh Release ./my-packages    # custom output directory
./scripts/pack.sh Debug                    # debug packages
```

Packing order (enforced by the script):
1. All Android module packages (Tier 1)
2. All iOS module packages (Tier 1)
3. `Datadog.MAUI.Android.Binding` meta-package (Tier 2)
4. `Datadog.MAUI.iOS.Binding` meta-package (Tier 2)
5. `Datadog.MAUI` consumer plugin (Tier 3)

> Each tier depends on the previous being available in `artifacts/` (the local NuGet source configured in `NuGet.Config`).

---

## Full Command Sequence (Clean Build)

```bash
# 1. Get iOS SDK frameworks
./scripts/download-ios-frameworks.sh

# 2. Build Swift wrapper (must be done before build.sh)
./scripts/build-ios-wrapper.sh --clean

# 3. Build and pack all modules
./scripts/build.sh Release

# 4. Validate
./scripts/validate-dependencies.sh

# 5. Pack final packages
./scripts/pack.sh Release

# Packages are now in ./artifacts/
```

---

## Bumping the Version

All packages share a single version defined in `Directory.Build.props`:

```xml
<DatadogSdkVersion>3.6.2-preview.4</DatadogSdkVersion>
```

Bump this value before rebuilding to invalidate the local NuGet cache and ensure consumers pick up the new packages. Also update any consumer app `.csproj` files that reference `Datadog.MAUI` directly.

---

## NuGet.Config

The repo's `NuGet.Config` configures `./artifacts` as the primary source, enabling the multi-tier build to resolve packages produced in earlier steps:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="./artifacts" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

When building `Datadog.MAUI.iOS.Binding` (Tier 2), it finds `Datadog.MAUI.iOS.Core` etc. in `./artifacts` without needing them published to NuGet.org first.

---

## Publishing to NuGet.org

Packages must be pushed in dependency order. If a higher-tier package is pushed before its dependencies are available on NuGet.org, consumers will fail to restore.

```bash
# Tier 1: Android modules
dotnet nuget push "artifacts/Datadog.MAUI.Android.*.nupkg" \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Tier 1: iOS modules
dotnet nuget push "artifacts/Datadog.MAUI.iOS.*.nupkg" \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Tier 2: Platform meta-packages
dotnet nuget push "artifacts/Datadog.MAUI.Android.Binding.*.nupkg" \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push "artifacts/Datadog.MAUI.iOS.Binding.*.nupkg" \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Tier 3: Consumer plugin (last)
dotnet nuget push "artifacts/Datadog.MAUI.3.6.2-preview.4.nupkg" \
  --api-key $NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

> **Important:** Do not use a glob for Tier 3 — it will match both `Datadog.MAUI.3.6.2-preview.4.nupkg` and the Android/iOS binding packages. Push the consumer plugin by exact filename.

---

## iOS-Specific: Swift Class Stub Handling

When the Datadog iOS SDK is upgraded, new Swift classes may be added. Any class compiled with library evolution will be a stub (`__objc_stublist`) and must **not** appear in `ApiDefinition.cs`. To identify stubs:

```bash
# Check for stub symbols in a framework binary
nm -m Datadog.MAUI.iOS.Binding/artifacts/DatadogCore.xcframework/ios-arm64_arm64e/DatadogCore.framework/DatadogCore \
  | grep "__objc_stublist" | grep "_OBJC_CLASS_"

# S = defined (safe to bind), I = indirect/stub (do not bind)
nm -m path/to/Framework \
  | grep "_OBJC_CLASS_\$_DD" \
  | awk '{print $NF, $(NF-1)}'
```

Any type reported as `I` (indirect) or found in `__objc_stublist` must be:
1. Removed from `ApiDefinition.cs`
2. Replaced with a method on the appropriate `DDWrapper*` class in `native-wrappers/ios/DatadogWrapper/`
3. The wrapper rebuilt with `./scripts/build-ios-wrapper.sh --clean`

The MSBuild target `_RemoveDatadogSwiftClassStubSymbols` in `Datadog.MAUI.Plugin/build/Datadog.MAUI.targets` also maintains a list of stub symbol names to strip from the exported symbols list at link time — keep this list in sync when adding or removing stub types.

---

## Troubleshooting

**"Skipping iOS modules (XCFrameworks not found)"**
Run `./scripts/download-ios-frameworks.sh` then `./scripts/build-ios-wrapper.sh`.

**`TypeLoadException` for a `DD*` type on device**
The type is a Swift class stub. Remove it from `ApiDefinition.cs` and add a `DDWrapper*` method to expose the functionality. Rebuild the wrapper.

**`ServiceCollection is read-only` during app startup**
`builder.Logging.AddDebug()` or similar service registration is being called after `builder.Build()`. Move all service registrations before the `Build()` call.

**NuGet cache serving stale package after version bump**
Delete the specific cached version: `rm -rf ~/.nuget/packages/datadog.maui*/<version>` or clear all: `rm -rf ~/.nuget/packages/datadog.maui*`.

**`error MSB1009: Project file does not exist` when running dotnet build**
Run `dotnet build` from the directory containing the `.csproj`, not from the repo root unless using the solution file.
