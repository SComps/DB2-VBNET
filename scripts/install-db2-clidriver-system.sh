#!/usr/bin/env bash
# install-db2-clidriver-system.sh
#
# Installs the IBM Db2 clidriver native libraries system-wide so that any
# application using IBM.Data.Db2 can find libdb2.so without needing
# LD_LIBRARY_PATH or a wrapper script.
#
# What it does:
#   1. Locates the clidriver shipped inside the Net.IBM.Data.Db2-lnx NuGet
#      package in the current user's NuGet cache.
#   2. Copies the full clidriver tree to /opt/ibm/db2clidriver/
#   3. Writes /etc/ld.so.conf.d/ibm-db2-clidriver.conf pointing at its lib/
#   4. Runs ldconfig so the linker cache is updated immediately.
#
# After this runs, libdb2.so is a first-class system library — no wrapper
# scripts, no LD_LIBRARY_PATH needed for any project on this machine.
#
# Requirements:
#   - Must be run as root (sudo)
#   - Net.IBM.Data.Db2-lnx NuGet package must already be restored at least
#     once (run `dotnet restore` in any project that references it first).
#     The NuGet cache is searched in: $HOME/.nuget/packages and
#     /root/.nuget/packages (when running under sudo).
#
# Usage:
#   chmod +x scripts/install-db2-clidriver-system.sh
#   sudo -E ./scripts/install-db2-clidriver-system.sh
#
# The -E flag preserves the invoking user's $HOME so the NuGet cache
# under ~/.nuget can be found when running as root.

set -euo pipefail

GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m'

ok()   { echo -e "${GREEN}[OK]${NC}  $*"; }
info() { echo -e "${CYAN}[--]${NC}  $*"; }
warn() { echo -e "${YELLOW}[!!]${NC}  $*"; }

if [[ $EUID -ne 0 ]]; then
    echo "This script must be run as root (use: sudo -E $0)" >&2
    exit 1
fi

INSTALL_DIR="/usr/lib/ibm/db2clidriver"
LDCONF_FILE="/etc/ld.so.conf.d/ibm-db2-clidriver.conf"

echo ""
echo "IBM Db2 CLI Driver — System-wide Installer"
echo "============================================"
echo ""

# ── Locate the clidriver in the NuGet cache ───────────────────────────────────
info "Searching NuGet cache for Net.IBM.Data.Db2-lnx clidriver..."

# Search the invoking user's cache (preserved via sudo -E) and root's cache
NUGET_SEARCH_DIRS=(
    "${HOME}/.nuget/packages/net.ibm.data.db2-lnx"
    "/root/.nuget/packages/net.ibm.data.db2-lnx"
)

CLIDRIVER_SRC=""
for BASE in "${NUGET_SEARCH_DIRS[@]}"; do
    FOUND=$(find "$BASE" -maxdepth 3 -type d -name "clidriver" 2>/dev/null | head -1)
    if [[ -n "$FOUND" ]]; then
        CLIDRIVER_SRC="$FOUND"
        break
    fi
done

if [[ -z "$CLIDRIVER_SRC" ]]; then
    echo "" >&2
    echo "ERROR: Could not find clidriver in NuGet cache." >&2
    echo "Run 'dotnet restore' in a project that references Net.IBM.Data.Db2-lnx first." >&2
    echo "Then re-run this script with: sudo -E $0" >&2
    exit 1
fi

ok "Found clidriver at: $CLIDRIVER_SRC"

# ── Copy clidriver to system location ─────────────────────────────────────────
info "Installing clidriver to $INSTALL_DIR ..."
mkdir -p "$INSTALL_DIR"
cp -a "$CLIDRIVER_SRC/." "$INSTALL_DIR/"
ok "Clidriver installed to $INSTALL_DIR"

# ── Register lib/ with ldconfig ───────────────────────────────────────────────
info "Registering $INSTALL_DIR/lib with ldconfig..."
echo "$INSTALL_DIR/lib" > "$LDCONF_FILE"
# ldconfig lives in /sbin on Debian — not always in PATH even as root
LDCONFIG_BIN=$(command -v ldconfig 2>/dev/null || echo "/sbin/ldconfig")
"$LDCONFIG_BIN"
ok "ldconfig updated — $INSTALL_DIR/lib is now in the linker cache"

# ── Set DB2_CLI_DRIVER_INSTALL_PATH system-wide ───────────────────────────────
info "Setting DB2_CLI_DRIVER_INSTALL_PATH in /etc/environment..."
# Remove any previous entry then append the current value
sed -i '/^DB2_CLI_DRIVER_INSTALL_PATH=/d' /etc/environment
echo "DB2_CLI_DRIVER_INSTALL_PATH=$INSTALL_DIR" >> /etc/environment
# Also export for the current shell session
export DB2_CLI_DRIVER_INSTALL_PATH="$INSTALL_DIR"
ok "DB2_CLI_DRIVER_INSTALL_PATH=$INSTALL_DIR"

echo ""
echo "============================================"
echo -e "${GREEN}IBM Db2 clidriver installed system-wide.${NC}"
echo ""
echo "  Install path : $INSTALL_DIR"
echo "  ldconfig     : $INSTALL_DIR/lib registered"
echo "  Environment  : DB2_CLI_DRIVER_INSTALL_PATH set in /etc/environment"
echo ""
echo "libdb2.so is now available to all applications on this machine."
echo "No LD_LIBRARY_PATH, no wrapper scripts, no per-app clidriver folder needed."
echo ""
echo "Re-login (or run: source /etc/environment) to pick up the env var"
echo "in interactive shells."
echo ""
