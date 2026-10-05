#!/usr/bin/env bash
# ==============================================================================
# Publish script for Db2Spufi.Core Library on Linux x64
# Builds only the class library, LINQ dependencies, XML docs, and NuGet package
# for import into external applications (excluding GUI / sample apps).
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

CORE_PROJECT="${ROOT_DIR}/Db2Spufi.Core/Db2Spufi.Core.vbproj"
OUTPUT_DIR="${ROOT_DIR}/publish/library/linux-x64"
NUPKG_DIR="${ROOT_DIR}/publish/library/nupkg"
CONFIGURATION="${1:-Release}"

echo "=========================================================="
echo " Publishing Db2Spufi.Core Library for Linux x64"
echo "=========================================================="
echo "Project : ${CORE_PROJECT}"
echo "Output  : ${OUTPUT_DIR}"
echo "Nupkg   : ${NUPKG_DIR}"
echo ""

rm -rf "${OUTPUT_DIR}"
mkdir -p "${OUTPUT_DIR}"
mkdir -p "${NUPKG_DIR}"

# 1. Publish library binaries & runtime assets for linux-x64
echo "Step 1: Publishing binaries and Db2 runtime dependencies for Linux..."
dotnet publish "${CORE_PROJECT}" \
    --configuration "${CONFIGURATION}" \
    --runtime linux-x64 \
    --output "${OUTPUT_DIR}" \
    --self-contained false \
    -p:GenerateDocumentationFile=true

# 2. Build NuGet package for external project references
echo "Step 2: Building NuGet package (.nupkg)..."
dotnet pack "${CORE_PROJECT}" \
    --configuration "${CONFIGURATION}" \
    --output "${NUPKG_DIR}" \
    -p:IncludeSymbols=true \
    -p:SymbolPackageFormat=snupkg

echo ""
echo "=========================================================="
echo " Db2Spufi.Core Library Published Successfully (Linux)!"
echo "=========================================================="
echo "Binary Location : ${OUTPUT_DIR}"
echo "  - Db2Spufi.Core.dll (Core Engine & LINQ to Db2)"
echo "  - Db2Spufi.Core.xml (Developer Intellisense Docs)"
echo "  - Net.IBM.Data.Db2-lnx.dll (IBM ADO.NET Provider)"
echo "NuGet Location  : ${NUPKG_DIR}"
echo ""
