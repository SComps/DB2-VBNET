#!/usr/bin/env bash
# publish-linux-x64.sh
# AOT publish for Linux x64 (AMD64).
#
# Produces a self-contained native executable targeting Linux x64.
# Output lands in Db2ConnTest/bin/publish/linux-x64/
#
# Requirements:
#   - .NET 10 SDK
#   - clang or gcc (AOT native compilation toolchain)
#   - zlib1g-dev (or equivalent) for the linker
#   - Run on a Linux x64 machine
#
# Install build deps on Debian/Ubuntu:
#   sudo apt-get install -y clang zlib1g-dev
#
# Install build deps on RHEL/Fedora:
#   sudo dnf install -y clang zlib-devel

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/../Db2ConnTest/Db2ConnTest.vbproj"
OUTPUT="$SCRIPT_DIR/../Db2ConnTest/bin/publish/linux-x64"
CONFIGURATION="${1:-Release}"

echo "Building AOT native binary for Linux x64..."
echo "Project : $PROJECT"
echo "Output  : $OUTPUT"
echo ""

dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime linux-x64 \
    --output "$OUTPUT" \
    --self-contained true

echo ""
echo "AOT publish complete."
echo "Executable: $OUTPUT/Db2ConnTest"
