#!/usr/bin/env bash
# Publish Db2Spufi.Avalonia for Linux x64 (self-contained)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_FILE="$SCRIPT_DIR/../Db2Spufi.Avalonia/Db2Spufi.Avalonia.csproj"
OUTPUT_DIR="$SCRIPT_DIR/../Db2Spufi.Avalonia/bin/publish/linux-x64"
CONFIG="${1:-Release}"

echo "=== Publishing Db2Spufi.Avalonia for Linux x64 (self-contained) ==="
echo "Project : $PROJECT_FILE"
echo "Output  : $OUTPUT_DIR"
echo "Config  : $CONFIG"

dotnet publish "$PROJECT_FILE" \
    --configuration "$CONFIG" \
    --runtime linux-x64 \
    --output "$OUTPUT_DIR" \
    --self-contained true \
    -p:PublishSingleFile=false

chmod +x "$OUTPUT_DIR/Db2Spufi.Avalonia"
echo ""
echo "=== Publish Complete ==="
echo "Executable: $OUTPUT_DIR/Db2Spufi.Avalonia"
