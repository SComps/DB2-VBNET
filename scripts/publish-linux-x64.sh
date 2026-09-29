#!/usr/bin/env bash
# publish-linux-x64.sh
# Self-contained publish for Db2ConnTest on Linux x64 (AMD64).
#
# Produces a self-contained executable targeting Linux x64.
# Output lands in Db2ConnTest/bin/publish/linux-x64/
#
# NOTE: PublishSingleFile is explicitly disabled because IBM.Data.Db2
# uses Assembly.CodeBase internally, which throws NotSupportedException
# when loaded from a single-file bundle.
#
# Requirements:
#   - .NET 10 SDK
#   - Run on a Linux x64 machine

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/../Db2ConnTest/Db2ConnTest.vbproj"
OUTPUT="$SCRIPT_DIR/../Db2ConnTest/bin/publish/linux-x64"
CONFIGURATION="${1:-Release}"

echo "Publishing Db2ConnTest for Linux x64 (self-contained)..."
echo "Project : $PROJECT"
echo "Output  : $OUTPUT"
echo ""

dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime linux-x64 \
    --output "$OUTPUT" \
    --self-contained true \
    -p:PublishSingleFile=false

# Write a launcher script that sets LD_LIBRARY_PATH for the IBM clidriver
LAUNCHER="$OUTPUT/run-Db2ConnTest.sh"
cat > "$LAUNCHER" << 'LAUNCHER_EOF'
#!/usr/bin/env bash
# Launcher for Db2ConnTest — sets LD_LIBRARY_PATH so the IBM clidriver
# native libdb2.so can be found at runtime on Linux.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export LD_LIBRARY_PATH="$SCRIPT_DIR/clidriver/lib:${LD_LIBRARY_PATH:-}"
exec "$SCRIPT_DIR/Db2ConnTest" "$@"
LAUNCHER_EOF
chmod +x "$LAUNCHER"

echo ""
echo "Publish complete."
echo "Executable : $OUTPUT/Db2ConnTest"
echo "Launcher   : $LAUNCHER  (use this to run — sets LD_LIBRARY_PATH)"
