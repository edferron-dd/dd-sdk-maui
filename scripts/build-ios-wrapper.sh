#!/bin/bash
# Build DatadogWrapper.xcframework from the Swift package.
#
# Uses xcodebuild build (not archive) so the static library .a is placed in
# DerivedData where it can be found. xcodebuild archive doesn't work for SPM
# static library targets — it produces a bare .o instead of a .a.
#
# Uses xcodebuild (not swiftc directly) so Xcode's module loading infrastructure
# correctly handles the cross-Swift-version swiftinterface files from dd-sdk-ios XCFrameworks.
#
# This wrapper is compiled WITHOUT Swift Library Evolution (BUILD_LIBRARY_FOR_DISTRIBUTION=NO)
# so its @objc classes land in __objc_classlist (not __objc_stublist).
# The C# bgen binding can then resolve them via objc_getClass() at runtime,
# avoiding the "native class hasn't been loaded" crash on real iOS devices.
#
# Usage:
#   ./build-ios-wrapper.sh [--clean]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

PACKAGE_DIR="$ROOT_DIR/native-wrappers/ios/DatadogWrapper"
ARTIFACTS_DIR="$ROOT_DIR/Datadog.MAUI.iOS.Binding/artifacts"
BUILD_DIR="/tmp/DatadogWrapper-build"
OUTPUT_XCFW="$ARTIFACTS_DIR/DatadogWrapper.xcframework"

echo -e "${CYAN}=====================================${NC}"
echo -e "${CYAN}Building DatadogWrapper.xcframework${NC}"
echo -e "${CYAN}=====================================${NC}"

# ---------------------------------------------------------------------------
# Optional clean
# ---------------------------------------------------------------------------
if [[ "${1:-}" == "--clean" ]]; then
    echo -e "${YELLOW}Cleaning previous build...${NC}"
    rm -rf "$BUILD_DIR"
    rm -rf "$OUTPUT_XCFW"
fi

mkdir -p "$BUILD_DIR"

# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------
if [ ! -f "$PACKAGE_DIR/Package.swift" ]; then
    echo -e "${RED}Error: Package.swift not found at $PACKAGE_DIR${NC}"
    exit 1
fi

if [ ! -d "$ARTIFACTS_DIR/DatadogCore.xcframework" ]; then
    echo -e "${RED}Error: XCFrameworks not found in $ARTIFACTS_DIR${NC}"
    echo -e "${YELLOW}Run ./scripts/download-ios-frameworks.sh first${NC}"
    exit 1
fi

# ---------------------------------------------------------------------------
# Resolve package dependencies
# ---------------------------------------------------------------------------
echo -e "\n${CYAN}[1/5] Resolving package dependencies...${NC}"
(cd "$PACKAGE_DIR" && xcodebuild -resolvePackageDependencies -quiet 2>&1 | grep -v "^$" || true)
echo -e "${GREEN}  ✓ Dependencies resolved${NC}"

# ---------------------------------------------------------------------------
# Build for iOS device
# ---------------------------------------------------------------------------
echo -e "\n${CYAN}[2/5] Building for iOS device...${NC}"
DEVICE_DERIVED="$BUILD_DIR/DerivedData-ios"

(cd "$PACKAGE_DIR" && xcodebuild build \
    -scheme DatadogWrapper \
    -destination "generic/platform=iOS" \
    -derivedDataPath "$DEVICE_DERIVED" \
    BUILD_LIBRARY_FOR_DISTRIBUTION=NO \
    ONLY_ACTIVE_ARCH=NO \
    -quiet 2>&1 | grep -E "(error:|Build succeeded|BUILD FAILED)" | head -20 || true)

echo -e "${GREEN}  ✓ Device build complete${NC}"

# ---------------------------------------------------------------------------
# Build for iOS Simulator
# ---------------------------------------------------------------------------
echo -e "\n${CYAN}[3/5] Building for iOS Simulator...${NC}"
SIM_DERIVED="$BUILD_DIR/DerivedData-sim"

(cd "$PACKAGE_DIR" && xcodebuild build \
    -scheme DatadogWrapper \
    -destination "generic/platform=iOS Simulator" \
    -derivedDataPath "$SIM_DERIVED" \
    BUILD_LIBRARY_FOR_DISTRIBUTION=NO \
    ONLY_ACTIVE_ARCH=NO \
    -quiet 2>&1 | grep -E "(error:|Build succeeded|BUILD FAILED)" | head -20 || true)

echo -e "${GREEN}  ✓ Simulator build complete${NC}"

# ---------------------------------------------------------------------------
# Locate built artifacts and create static archives
# ---------------------------------------------------------------------------
echo -e "\n${CYAN}[4/5] Locating built artifacts...${NC}"

# xcodebuild build for an SPM static library target produces DatadogWrapper.o
# (a merged object file) in Products, not a .a. We archive it ourselves.
DEVICE_OBJ=$(find "$DEVICE_DERIVED/Build/Products" -name "DatadogWrapper.o" -maxdepth 3 2>/dev/null | head -1)
SIM_OBJ=$(find "$SIM_DERIVED/Build/Products" -name "DatadogWrapper.o" -maxdepth 3 2>/dev/null | head -1)

if [ -z "$DEVICE_OBJ" ] || [ -z "$SIM_OBJ" ]; then
    echo -e "${RED}  ✗ DatadogWrapper.o not found${NC}"
    echo "Device build products:"
    find "$DEVICE_DERIVED/Build/Products" -type f 2>/dev/null | head -30
    echo "Simulator build products:"
    find "$SIM_DERIVED/Build/Products" -type f 2>/dev/null | head -30

    # Verbose rebuild for diagnosis
    echo -e "\n${YELLOW}Re-running device build with full output for diagnosis:${NC}"
    (cd "$PACKAGE_DIR" && xcodebuild build \
        -scheme DatadogWrapper \
        -destination "generic/platform=iOS" \
        -derivedDataPath "$DEVICE_DERIVED" \
        BUILD_LIBRARY_FOR_DISTRIBUTION=NO \
        ONLY_ACTIVE_ARCH=NO 2>&1 | tail -80)
    exit 1
fi

echo -e "${GREEN}  ✓ Device object:     $DEVICE_OBJ${NC}"
echo -e "${GREEN}  ✓ Simulator object:  $SIM_OBJ${NC}"

# Create .a archives from the merged object files
DEVICE_LIB="$BUILD_DIR/ios-build/libDatadogWrapper.a"
SIM_LIB="$BUILD_DIR/sim-build/libDatadogWrapper.a"
mkdir -p "$BUILD_DIR/ios-build" "$BUILD_DIR/sim-build"
ar rcs "$DEVICE_LIB" "$DEVICE_OBJ"
ar rcs "$SIM_LIB" "$SIM_OBJ"
echo -e "${GREEN}  ✓ Device library:    $DEVICE_LIB${NC}"
echo -e "${GREEN}  ✓ Simulator library: $SIM_LIB${NC}"

# Find the generated ObjC header from Intermediates
DEVICE_HEADER=$(find "$DEVICE_DERIVED/Build/Intermediates.noindex" -name "DatadogWrapper-Swift.h" 2>/dev/null | head -1)
if [ -z "$DEVICE_HEADER" ]; then
    echo -e "${RED}  ✗ DatadogWrapper-Swift.h not found in Intermediates${NC}"
    find "$DEVICE_DERIVED/Build/Intermediates.noindex" -name "*.h" 2>/dev/null | head -20
    exit 1
fi
echo -e "${GREEN}  ✓ ObjC header:       $DEVICE_HEADER${NC}"

# Build a headers directory for XCFramework creation
HEADERS_DIR="$BUILD_DIR/headers"
rm -rf "$HEADERS_DIR"
mkdir -p "$HEADERS_DIR"
cp "$DEVICE_HEADER" "$HEADERS_DIR/DatadogWrapper-Swift.h"

cat > "$HEADERS_DIR/module.modulemap" <<'EOF'
module DatadogWrapper {
    header "DatadogWrapper-Swift.h"
    export *
}
EOF

# ---------------------------------------------------------------------------
# Create XCFramework
# ---------------------------------------------------------------------------
echo -e "\n${CYAN}[5/5] Creating XCFramework...${NC}"
rm -rf "$OUTPUT_XCFW"

xcodebuild -create-xcframework \
    -library "$DEVICE_LIB" -headers "$HEADERS_DIR" \
    -library "$SIM_LIB" -headers "$HEADERS_DIR" \
    -output "$OUTPUT_XCFW"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo -e "\n${GREEN}=====================================${NC}"
echo -e "${GREEN}Build complete!${NC}"
echo -e "${GREEN}=====================================${NC}"
echo -e "${CYAN}Output: $OUTPUT_XCFW${NC}"
echo -e "${CYAN}Contents:${NC}"
ls "$OUTPUT_XCFW"

echo -e "\n${CYAN}Next steps:${NC}"
echo "  1. Pack the NuGet packages: ./scripts/pack.sh"
echo "  2. Test on device to verify the DDConfiguration crash is resolved"
