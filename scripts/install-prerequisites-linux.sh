#!/usr/bin/env bash
# install-prerequisites-linux.sh
#
# Installs all prerequisites required to build and run DB2-VBNET on Linux x64.
#
# Installs:
#   1. .NET 10 SDK  (via Microsoft package feed)
#   2. git
#
# Supports:
#   - Debian / Ubuntu (apt)
#   - RHEL / Fedora / CentOS Stream (dnf)
#
# Note: lsb_release is NOT required — distro info is read from /etc/os-release.
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
        # Read distro info without lsb_release
        . /etc/os-release
        DISTRO_ID="${ID}"          # e.g. debian, ubuntu
        DISTRO_VER="${VERSION_ID}" # e.g. 12, 22.04

        apt-get update -qq
        apt-get install -y wget apt-transport-https ca-certificates

        # Build the Microsoft packages URL based on distro
        MS_URL="https://packages.microsoft.com/config/${DISTRO_ID}/${DISTRO_VER}/packages-microsoft-prod.deb"
        info "Fetching Microsoft feed: $MS_URL"

        if wget -q --spider "$MS_URL" 2>/dev/null; then
            wget -q "$MS_URL" -O /tmp/packages-microsoft-prod.deb
            dpkg -i /tmp/packages-microsoft-prod.deb
            rm /tmp/packages-microsoft-prod.deb
            apt-get update -qq
            apt-get install -y dotnet-sdk-10.0
        else
            # Fallback: use the dotnet-install script (works on any distro/version)
            info "Microsoft .deb feed not available for ${DISTRO_ID} ${DISTRO_VER} — using dotnet-install.sh fallback"
            wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
            chmod +x /tmp/dotnet-install.sh
            /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/local/dotnet
            rm /tmp/dotnet-install.sh
            # Make dotnet available system-wide
            ln -sf /usr/local/dotnet/dotnet /usr/local/bin/dotnet
        fi
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

# ── 2. Git ────────────────────────────────────────────────────────────────────
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
echo "  2. Publish (Linux x64 self-contained):"
echo "       chmod +x scripts/publish-linux-x64.sh"
echo "       ./scripts/publish-linux-x64.sh"
echo ""
echo "  3. Run:"
echo "       ./Db2ConnTest/bin/publish/linux-x64/Db2ConnTest"
echo ""
