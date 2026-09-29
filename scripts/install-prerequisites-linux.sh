#!/usr/bin/env bash
# install-prerequisites-linux.sh
#
# Installs all prerequisites required to build and run Db2ConnTest on Linux x64.
#
# Installs:
#   1. .NET 10 SDK              (via Microsoft package feed)
#   2. clang                    (AOT native compilation toolchain)
#   3. zlib development headers (required by the AOT linker)
#   4. git
#
# Supports:
#   - Debian / Ubuntu (apt)
#   - RHEL / Fedora / CentOS Stream (dnf)
#
# Usage:
#   chmod +x scripts/install-prerequisites-linux.sh
#   sudo ./scripts/install-prerequisites-linux.sh

set -euo pipefail

# ── Colour helpers ────────────────────────────────────────────────────────────
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

ok()   { echo -e "${GREEN}[OK]${NC}  $*"; }
info() { echo -e "${CYAN}[--]${NC}  $*"; }
warn() { echo -e "${YELLOW}[!!]${NC}  $*"; }

# ── Root check ────────────────────────────────────────────────────────────────
if [[ $EUID -ne 0 ]]; then
    echo "This script must be run as root (use sudo)." >&2
    exit 1
fi

echo ""
echo "DB2-VBNET Prerequisite Installer (Linux x64)"
echo "=================================================="
echo ""

# ── Detect package manager ────────────────────────────────────────────────────
if command -v apt-get &>/dev/null; then
    PKG_MGR="apt"
elif command -v dnf &>/dev/null; then
    PKG_MGR="dnf"
elif command -v yum &>/dev/null; then
    PKG_MGR="yum"
else
    echo "Unsupported Linux distribution. Install manually:" >&2
    echo "  .NET 10 SDK, clang, zlib-dev, git" >&2
    exit 1
fi

info "Detected package manager: $PKG_MGR"
echo ""

# ── 1. .NET 10 SDK ────────────────────────────────────────────────────────────
echo "Checking .NET 10 SDK..."

if command -v dotnet &>/dev/null && dotnet --list-sdks 2>/dev/null | grep -q "^10\."; then
    ok ".NET 10 SDK already installed"
else
    info ".NET 10 SDK not found — installing via Microsoft package feed"

    if [[ "$PKG_MGR" == "apt" ]]; then
        # Microsoft feed for Debian/Ubuntu
        apt-get update -qq
        apt-get install -y wget apt-transport-https
        wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb \
            -O /tmp/packages-microsoft-prod.deb
        dpkg -i /tmp/packages-microsoft-prod.deb
        rm /tmp/packages-microsoft-prod.deb
        apt-get update -qq
        apt-get install -y dotnet-sdk-10.0
    else
        # Microsoft feed for RHEL/Fedora
        rpm --import https://packages.microsoft.com/keys/microsoft.asc
        cat > /etc/yum.repos.d/microsoft-prod.repo << 'EOF'
[packages-microsoft-com-prod]
name=packages-microsoft-com-prod
baseurl=https://packages.microsoft.com/rhel/9/prod/
enabled=1
gpgcheck=1
gpgkey=https://packages.microsoft.com/keys/microsoft.asc
EOF
        dnf install -y dotnet-sdk-10.0
    fi
    ok ".NET 10 SDK installed"
fi

# ── 2. clang (AOT native compilation toolchain) ───────────────────────────────
echo ""
echo "Checking clang..."

if command -v clang &>/dev/null; then
    ok "clang already installed ($(clang --version | head -1))"
else
    info "clang not found — installing"
    if [[ "$PKG_MGR" == "apt" ]]; then
        apt-get install -y clang
    else
        dnf install -y clang
    fi
    ok "clang installed"
fi

# ── 3. zlib development headers (required by AOT linker) ─────────────────────
echo ""
echo "Checking zlib development headers..."

if [[ "$PKG_MGR" == "apt" ]]; then
    ZLIB_PKG="zlib1g-dev"
    ZLIB_CHECK() { dpkg -l zlib1g-dev 2>/dev/null | grep -q "^ii"; }
else
    ZLIB_PKG="zlib-devel"
    ZLIB_CHECK() { rpm -q zlib-devel &>/dev/null; }
fi

if ZLIB_CHECK; then
    ok "zlib development headers already installed"
else
    info "zlib dev headers not found — installing $ZLIB_PKG"
    if [[ "$PKG_MGR" == "apt" ]]; then
        apt-get install -y zlib1g-dev
    else
        dnf install -y zlib-devel
    fi
    ok "zlib development headers installed"
fi

# ── 4. Git ────────────────────────────────────────────────────────────────────
echo ""
echo "Checking Git..."

if command -v git &>/dev/null; then
    ok "Git already installed ($(git --version))"
else
    info "Git not found — installing"
    if [[ "$PKG_MGR" == "apt" ]]; then
        apt-get install -y git
    else
        dnf install -y git
    fi
    ok "Git installed"
fi

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "=================================================="
echo -e "${GREEN}All prerequisites satisfied.${NC}"
echo ""
echo "Next steps:"
echo "  1. Clone the repo (if not already done):"
echo "       git clone https://github.com/SComps/DB2-VBNET.git"
echo ""
echo "  2. Build and run:"
echo "       dotnet run --project Db2ConnTest/Db2ConnTest.vbproj"
echo ""
echo "  3. AOT publish (Linux native binary):"
echo "       chmod +x scripts/publish-linux-x64.sh"
echo "       ./scripts/publish-linux-x64.sh"
echo ""
