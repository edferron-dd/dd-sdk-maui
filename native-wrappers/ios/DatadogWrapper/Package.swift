// swift-tools-version: 5.9
import PackageDescription

// Relative paths are resolved from this Package.swift location:
// native-wrappers/ios/DatadogWrapper/ → root is ../../../
let artifactsPath = "../../../Datadog.MAUI.iOS.Binding/artifacts"

let package = Package(
    name: "DatadogWrapper",
    platforms: [.iOS(.v17)],
    products: [
        .library(name: "DatadogWrapper", type: .static, targets: ["DatadogWrapper"]),
    ],
    targets: [
        // Local binary xcframework targets pointing to the already-downloaded dd-sdk-ios XCFrameworks.
        // These avoid any network access and reuse what Datadog.MAUI.iOS.Binding already ships.
        .binaryTarget(
            name: "DatadogCore",
            path: "\(artifactsPath)/DatadogCore.xcframework"
        ),
        .binaryTarget(
            name: "DatadogInternal",
            path: "\(artifactsPath)/DatadogInternal.xcframework"
        ),
        .binaryTarget(
            name: "DatadogRUM",
            path: "\(artifactsPath)/DatadogRUM.xcframework"
        ),
        .binaryTarget(
            name: "DatadogLogs",
            path: "\(artifactsPath)/DatadogLogs.xcframework"
        ),
        .binaryTarget(
            name: "DatadogTrace",
            path: "\(artifactsPath)/DatadogTrace.xcframework"
        ),
        .binaryTarget(
            name: "DatadogSessionReplay",
            path: "\(artifactsPath)/DatadogSessionReplay.xcframework"
        ),
        .binaryTarget(
            name: "OpenTelemetryApi",
            path: "\(artifactsPath)/OpenTelemetryApi.xcframework"
        ),
        .target(
            name: "DatadogWrapper",
            dependencies: [
                .target(name: "DatadogCore"),
                .target(name: "DatadogInternal"),
                .target(name: "DatadogRUM"),
                .target(name: "DatadogLogs"),
                .target(name: "DatadogTrace"),
                .target(name: "DatadogSessionReplay"),
                .target(name: "OpenTelemetryApi"),
            ],
            path: "Sources/DatadogWrapper",
            swiftSettings: [
                // Do NOT enable library evolution — keeps our @objc classes in __objc_classlist
                // so C# bgen can resolve them via objc_getClass() on real devices.
            ]
        ),
    ]
)
