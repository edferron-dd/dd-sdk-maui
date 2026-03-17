# Datadog MAUI SDK Scripts

This document describes each script in the `scripts/` folder, what it does, and when and in what order to use it.

---

## Quick Reference: Common Workflows

### First-time setup (full build from scratch)
```
1. download-ios-frameworks.sh
2. generate-ios-bindings-sharpie.sh  (optional, if regenerating bindings)
3. consolidate-ios-bindings.sh       (optional, if regenerating bindings)
4. build.sh
5. validate-dependencies.sh
6. pack.sh
```

### Upgrading the Datadog Android SDK to a new version
```
1. analyze-android-dependencies.sh   (understand what changed)
2. generate-android-dependencies.sh  (get new csproj snippets)
3. setup-android-bindings.sh         (build, detect errors, get fixes)
4. build.sh
5. validate-dependencies.sh
6. pack.sh
```

### Adding a brand-new Android binding project
```
1. create-android-binding-projects.sh
2. generate-android-dependencies.sh
3. setup-android-bindings.sh
4. build.sh
5. pack.sh
```

### Routine build and pack (no SDK version change)
```
1. build.sh [Release]
2. validate-dependencies.sh
3. pack.sh
```

---

## Script Reference

### `build.sh`

**What it does:**
Orchestrates the full build sequence for the Datadog MAUI SDK solution. It:
1. Cleans previous build output
2. Builds and packs each individual Android and iOS module binding project into `./artifacts`
3. Restores NuGet packages for the whole solution (using locally-packed modules)
4. Builds the platform meta-packages (`Datadog.MAUI.iOS.Binding`, `Datadog.MAUI.Android.Binding`) and the main consumer plugin (`Datadog.MAUI.Plugin`)

**When to use:**
- Any time you want to compile the SDK — either for local development or before creating release packages
- iOS modules are only built on macOS and only if XCFrameworks are present in `Datadog.MAUI.iOS.Binding/`

**Usage:**
```bash
./scripts/build.sh              # Debug (default)
./scripts/build.sh Release      # Release
```

---

### `pack.sh`

**What it does:**
Creates all NuGet `.nupkg` files in the correct dependency order:
- **Step A:** Packs every individual Android and iOS module binding package
- **Step B:** Packs the platform meta-packages (`Datadog.MAUI.Android.Binding`, `Datadog.MAUI.iOS.Binding`)
- **Step C:** Packs the final consumer plugin package (`Datadog.MAUI`)

The ordering is critical — each layer depends on the previous one being available as a local NuGet source.

**When to use:**
- After a successful Release build, when you want to produce packages for publishing to NuGet.org or local testing
- Always run `build.sh Release` first

**Usage:**
```bash
./scripts/pack.sh                           # Release to ./artifacts (default)
./scripts/pack.sh Release ./my-packages     # Custom output directory
./scripts/pack.sh Debug                     # Debug packages
```

---

### `validate-dependencies.sh`

**What it does:**
Validates that all expected NuGet package dependency declarations are present in the `.csproj` files for:
- The iOS meta-package (`Datadog.MAUI.iOS.Binding`)
- The Android meta-package (`Datadog.MAUI.Android.Binding`)
- The consumer plugin (`Datadog.MAUI.Plugin`)
- Key individual bindings (iOS Core, Android Core)

Reports pass/fail counts and exits with a non-zero code if anything is missing.

**When to use:**
- As a sanity check before packing or publishing
- After adding or removing modules, to confirm the meta-packages reference everything expected
- Can be integrated into CI pipelines

**Usage:**
```bash
./scripts/validate-dependencies.sh          # Human-readable output
./scripts/validate-dependencies.sh json     # JSON output (for CI)
```

---

### `download-ios-frameworks.sh`

**What it does:**
Downloads Datadog iOS XCFrameworks from GitHub releases (`DataDog/dd-sdk-ios`) and installs them into `Datadog.MAUI.iOS.Binding/artifacts/`. If no version is specified, it fetches the latest release automatically.

Downloaded frameworks include: `DatadogCore`, `DatadogInternal`, `DatadogRUM`, `DatadogLogs`, `DatadogTrace`, `DatadogCrashReporting`, `DatadogSessionReplay`, `DatadogWebViewTracking`, `DatadogFlags`, `OpenTelemetryApi`.

**When to use:**
- First-time setup on a new machine
- When upgrading to a new Datadog iOS SDK version
- Whenever `build.sh` reports "XCFrameworks not found"
- macOS only

**Usage:**
```bash
./scripts/download-ios-frameworks.sh                          # Latest release
./scripts/download-ios-frameworks.sh 3.5.0                   # Specific version
./scripts/download-ios-frameworks.sh 3.5.0 ./custom/path     # Custom output path
```

---

### `generate-ios-bindings-sharpie.sh`

**What it does:**
Runs Objective Sharpie against each downloaded XCFramework to auto-generate draft C# binding files (`ApiDefinitions.cs`, `StructsAndEnums.cs`) into `Datadog.MAUI.iOS.Binding/Generated/<FrameworkName>/`.

Automatically selects the most compatible Xcode version (prefers Xcode 15.x, falls back to 16.x) and uses the simulator slice of each framework for Sharpie compatibility.

**When to use:**
- When adding a new Datadog iOS framework to the binding project
- When a new iOS SDK release contains significant API changes and you need a fresh baseline
- macOS only; requires [Objective Sharpie](https://aka.ms/objective-sharpie) to be installed

**Usage:**
```bash
./scripts/generate-ios-bindings-sharpie.sh
```

> Note: Output is a starting point only. Always review generated files for `[Verify]` attributes and resolve any issues before using in a build.

---

### `generate-ios-bindings.sh`

**What it does:**
Similar to `generate-ios-bindings-sharpie.sh` but uses a slightly different invocation style (targets the iOS device slice rather than the simulator slice, and accepts a custom binding project path). Also generates output into `<binding_path>/Generated/`.

**When to use:**
- Alternative to `generate-ios-bindings-sharpie.sh` when you need to target the device slice
- Accepts a custom binding project path as an argument

**Usage:**
```bash
./scripts/generate-ios-bindings.sh                          # Default path
./scripts/generate-ios-bindings.sh Datadog.MAUI.iOS.Binding
```

---

### `consolidate-ios-bindings.sh`

**What it does:**
Merges all the per-framework `ApiDefinitions.cs` and `StructsAndEnums.cs` files generated by Sharpie (from `Datadog.MAUI.iOS.Binding/Generated/`) into single consolidated files:
- `Datadog.MAUI.iOS.Binding/ApiDefinition.cs`
- `Datadog.MAUI.iOS.Binding/StructsAndEnums.cs`

Automatically backs up existing files before overwriting and adds separator comments between framework sections.

**When to use:**
- After running `generate-ios-bindings-sharpie.sh` or `generate-ios-bindings.sh`, as the next step before building the iOS binding project
- macOS only

**Usage:**
```bash
./scripts/consolidate-ios-bindings.sh
```

> After consolidation, manually review the output files, remove `[Verify]` attributes, and resolve any namespace conflicts before building.

---

### `analyze-android-dependencies.sh`

**What it does:**
Fetches Maven POM files from Maven Central for all Datadog Android SDK modules and reports their `com.datadoghq` transitive dependencies. Covers core, feature, and integration packages.

This is a read-only analysis tool — it makes no changes to the project.

**When to use:**
- When evaluating an upgrade to a new Datadog Android SDK version
- To understand the dependency graph before adding a new binding project
- To check whether a module has gained or lost dependencies between releases

**Usage:**
```bash
./scripts/analyze-android-dependencies.sh          # Defaults to v3.5.0
./scripts/analyze-android-dependencies.sh 3.7.0    # Specific version
```

---

### `generate-android-dependencies.sh`

**What it does:**
Fetches the Maven POM for a single Datadog Android artifact and outputs ready-to-paste XML snippets for:
- `AndroidMavenLibrary` entries for the `.csproj` file (using `$(DatadogSdkVersion)` for Datadog packages)
- `PackageReference` entries for the `.csproj` file (AndroidX/NuGet)
- `PackageVersion` entries for `Directory.Packages.props`
- Kotlin dependency notes (handled centrally, no action needed)

**When to use:**
- When adding a new Android module binding project
- When an SDK upgrade introduces new transitive dependencies for a specific module
- Run once per module that needs updating

**Usage:**
```bash
./scripts/generate-android-dependencies.sh <artifact-name> <version>

# Examples:
./scripts/generate-android-dependencies.sh dd-sdk-android-core 3.5.0
./scripts/generate-android-dependencies.sh dd-sdk-android-rum 3.7.0
```

---

### `setup-android-bindings.sh`

**What it does:**
An automated assistant for setting up or fixing a single Android binding project. It:
1. Fetches the Maven POM to understand expected dependencies
2. Attempts to `dotnet build` the project
3. If the build fails, parses `XA4241`/`XA4242` errors to categorize missing dependencies into Maven, NuGet, and ignored lists
4. Outputs ready-to-paste XML for `.csproj`, `Directory.Packages.props`, and `Directory.Build.targets`

**When to use:**
- When a new Android binding project fails to build due to missing dependency declarations
- When an SDK upgrade breaks an existing binding project with new dependency errors
- Iteratively: run, apply suggested fixes, run again until build succeeds

**Usage:**
```bash
./scripts/setup-android-bindings.sh [SDK_VERSION] [PROJECT_PATH]

# Examples:
./scripts/setup-android-bindings.sh 3.5.0 ../Datadog.MAUI.Android.Binding/dd-sdk-android-core
./scripts/setup-android-bindings.sh 3.7.0 ../Datadog.MAUI.Android.Binding/dd-sdk-android-rum
```

---

### `create-android-binding-projects.sh`

**What it does:**
Scaffolds new `.csproj` binding projects and `Transforms/Metadata.xml` files for the core Datadog Android SDK modules. Creates projects for: `dd-sdk-android-internal`, `dd-sdk-android-core`, `dd-sdk-android-rum`, `dd-sdk-android-logs`, `dd-sdk-android-trace`, `dd-sdk-android-ndk`, `dd-sdk-android-session-replay`, `dd-sdk-android-webview`, and `dd-sdk-android-flags`.

**When to use:**
- If the Android binding projects need to be recreated from scratch (e.g., after a major restructure)
- When adding a new set of Android modules to the solution

> This script hard-codes SDK version `3.5.0` — update that variable before running if targeting a different version.

**Usage:**
```bash
./scripts/create-android-binding-projects.sh                              # Current directory
./scripts/create-android-binding-projects.sh ../Datadog.MAUI.Android.Binding
```

---

### `map-maven-to-nuget.sh`

**What it does:**
Looks up the NuGet package name that corresponds to a given Maven coordinate, and suggests an upgraded version when the Maven version is known to be incompatible with `net9.0`/`net10.0` targets (e.g., Kotlin stdlib `2.0.21` → `2.3.0.1`).

This is a lookup/reference utility — it makes no changes to the project.

**When to use:**
- When `generate-android-dependencies.sh` or `setup-android-bindings.sh` identifies an AndroidX or Kotlin dependency and you need to know the correct NuGet package name and version
- When troubleshooting target-framework compatibility issues with a third-party dependency

**Usage:**
```bash
./scripts/map-maven-to-nuget.sh <maven-coordinate> [maven-version] [--check-frameworks]

# Examples:
./scripts/map-maven-to-nuget.sh "org.jetbrains.kotlin:kotlin-stdlib" "2.0.21"
./scripts/map-maven-to-nuget.sh "androidx.core:core" "1.15.0" --check-frameworks
```

---

### `validate-android-artifacts.sh`

**What it does:**
Downloads `verification-metadata.xml` from a Datadog Android SDK GitHub release and reports the number of components and artifacts declared in it. Optionally attempts to validate SHA256 checksums of locally cached Maven artifacts (checksum validation is partially implemented — it locates the cache directory but does not yet compare hashes).

**When to use:**
- When you want to audit which artifact versions are declared in a specific SDK release
- As a starting point for supply-chain verification before upgrading the Android SDK version
- Note: Full checksum validation is not yet implemented; the script currently reports findings only

**Usage:**
```bash
./scripts/validate-android-artifacts.sh <version> [--download-metadata] [--validate-checksums]

# Examples:
./scripts/validate-android-artifacts.sh 3.5.0 --download-metadata
./scripts/validate-android-artifacts.sh 3.5.0 --download-metadata --validate-checksums
./scripts/validate-android-artifacts.sh 3.5.0    # Use existing verification-metadata.xml
```

---

## Notes

- All scripts use `set -e` (exit on error) unless otherwise noted — failures are explicit
- Scripts that output XML snippets (e.g., `generate-android-dependencies.sh`, `setup-android-bindings.sh`) are advisory; you must manually apply the suggested changes to the relevant project files
- iOS scripts require macOS and Xcode; they will be skipped or will error on other platforms
- The `scripts/artifacts/` folder contains a `Archive.zip` used during development — do not delete it
