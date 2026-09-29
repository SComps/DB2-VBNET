#!/usr/bin/env bash
# publish-linux-arm64.sh
# Self-contained publish for Db2ConnTest on Linux ARM64 (aarch64).
#
# Produces a self-contained executable targeting Linux arm64.
# Output lands in Db2ConnTest/bin/publish/linux-arm64/
#
# NOTE: PublishSingleFile is explicitly disabled because IBM.Data.Db2
# uses Assembly.CodeBase internally, which throws NotSupportedException
# when loaded from a single-file bundle.
#
# Requirements:
#   - .NET 10 SDK
#   - Run on a Linux arm64 machine (e.g. Apple Silicon, Raspberry Pi 4,
#     AWS Graviton) — OR cross-publish from any machine with the .NET
#     arm64 runtime pack installed.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/../Db2ConnTest/Db2ConnTest.vbproj"
OUTPUT="$SCRIPT_DIR/../Db2ConnTest/bin/publish/linux-arm64"
CONFIGURATION="${1:-Release}"

echo "Publishing Db2ConnTest for Linux arm64 (self-contained)..."
echo "Project : $PROJECT"
echo "Output  : $OUTPUT"
echo ""

dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime linux-arm64 \
    --output "$OUTPUT" \
    --self-contained true \
    -p:PublishSingleFile=false

echo ""
echo "Publish complete."
echo "Executable: $OUTPUT/Db2ConnTest"
