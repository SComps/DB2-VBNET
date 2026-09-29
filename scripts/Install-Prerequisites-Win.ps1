<#
.SYNOPSIS
    Installs all prerequisites required to build and run Db2ConnTest on Windows.

.DESCRIPTION
    Checks for and installs the following:
      1. .NET 10 SDK          (via winget)
      2. Visual Studio 2026 Build Tools with the Desktop/Native workload
         required for AOT native compilation  (via winget)
      3. Git                  (via winget)

    NuGet packages (Net.IBM.Data.Db2) are restored automatically by
    'dotnet restore' - no manual step needed.

    The Db2 Connect license is server-side on this ADCD environment and
    requires no client-side installation.

.NOTES
    Run this script as Administrator for winget installs to apply system-wide.
    If winget is not available, install it from:
      https://aka.ms/getwinget

.EXAMPLE
    # From an elevated PowerShell prompt in the repo root:
    powershell -ExecutionPolicy Bypass -File scripts\Install-Prerequisites-Win.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# -- Helpers ------------------------------------------------------------------

function Test-Command($cmd) {
    return [bool](Get-Command $cmd -ErrorAction SilentlyContinue)
}

function Write-Status($msg, $ok) {
    $symbol = if ($ok) { "[OK]" } else { "[--]" }
    $color  = if ($ok) { "Green" } else { "Yellow" }
    Write-Host "$symbol  $msg" -ForegroundColor $color
}

function Install-Winget($displayName, $wingetId) {
    Write-Host "  Installing $displayName via winget..." -ForegroundColor Cyan
    winget install --id $wingetId --silent --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -notin @(0, -1978335212)) {  # -1978335212 = already installed
        Write-Error "winget install of $displayName failed (exit $LASTEXITCODE)."
    }
}

# -- Check winget itself ------------------------------------------------------─

Write-Host ""
Write-Host "DB2-VBNET Prerequisite Installer (Windows)" -ForegroundColor Cyan
Write-Host ("=" * 50)
Write-Host ""

if (-not (Test-Command "winget")) {
    Write-Error @"
winget (Windows Package Manager) is not available.
Install it from the Microsoft Store or:
  https://aka.ms/getwinget
Then re-run this script.
"@
}

# -- 1. .NET 10 SDK ------------------------------------------------------------

Write-Host "Checking .NET 10 SDK..."
$dotnetOk = $false
if (Test-Command "dotnet") {
    $sdks = dotnet --list-sdks 2>&1
    if ($sdks -match "^10\.") {
        $dotnetOk = $true
    }
}

if ($dotnetOk) {
    Write-Status ".NET 10 SDK already installed" $true
} else {
    Write-Status ".NET 10 SDK not found - installing" $false
    Install-Winget ".NET 10 SDK" "Microsoft.DotNet.SDK.10"
    Write-Status ".NET 10 SDK installed" $true
}

# -- 2. Visual C++ Build Tools (required for AOT native compilation) ----------─

Write-Host ""
Write-Host "Checking Visual C++ Build Tools / Visual Studio 2026..."

$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vcOk    = $false

if (Test-Path $vsWhere) {
    $vsJson = & $vsWhere -latest -products * -requires Microsoft.VisualCpp.Tools.HostX64.TargetX64 -format json 2>&1
    $vsInfo = $vsJson | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($vsInfo) { $vcOk = $true }
}

if ($vcOk) {
    Write-Status "Visual C++ build tools already installed" $true
} else {
    Write-Status "Visual C++ build tools not found - installing VS 2026 Build Tools" $false
    Write-Host "  NOTE: This is a large download (~2 GB) and may take several minutes." -ForegroundColor DarkYellow
    Install-Winget "Visual Studio 2026 Build Tools" "Microsoft.VisualStudio.2026.BuildTools"
    # Install the required workload components after the base install
    $vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe"
    if (Test-Path $vsInstaller) {
        Write-Host "  Adding VC++ workload components..."
        & $vsInstaller modify `
            --installPath "C:\Program Files\Microsoft Visual Studio\BuildTools" `
            --add Microsoft.VisualStudio.Workload.VCTools `
            --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
            --quiet --norestart
    }
    Write-Status "Visual C++ build tools installed" $true
}

# -- 3. Git --------------------------------------------------------------------

Write-Host ""
Write-Host "Checking Git..."
if (Test-Command "git") {
    $gitVer = git --version
    Write-Status "Git already installed ($gitVer)" $true
} else {
    Write-Status "Git not found - installing" $false
    Install-Winget "Git" "Git.Git"
    Write-Status "Git installed" $true
}

# -- Summary ------------------------------------------------------------------─

Write-Host ""
Write-Host ("=" * 50)
Write-Host "All prerequisites satisfied." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Clone the repo (if not already done):"
Write-Host "       git clone https://github.com/SComps/DB2-VBNET.git"
Write-Host ""
Write-Host "  2. Build and run:"
Write-Host "       dotnet run --project Db2ConnTest\Db2ConnTest.vbproj"
Write-Host ""
Write-Host "  3. AOT publish (Windows native binary):"
Write-Host "       powershell -ExecutionPolicy Bypass -File scripts\Publish-Win-x64.ps1"
Write-Host ""
