<#
.SYNOPSIS
    Binds the IBM Db2 CLI driver packages against a Db2 for z/OS server.

.DESCRIPTION
    The Net.IBM.Data.Db2 NuGet package ships as a thin client driver. Before
    any .NET application using this driver can execute queries against a Db2
    for z/OS server, the driver's system packages must be bound to that server
    once. This script performs that one-time bind operation.

    Run this script once per target server. It does not need to be re-run
    unless the driver version changes or the packages are dropped on the server.

.PARAMETER AppBinDir
    Full path to the application's net10.0 output directory that contains the
    clidriver folder. Defaults to the Db2ConnTest Release output alongside
    this script.

.PARAMETER Database
    Db2 location name (e.g. DBD1LOC).

.PARAMETER Server
    Hostname or IP address of the z/OS server (e.g. 10.10.13.2).

.PARAMETER Port
    DRDA listener port on the z/OS server (e.g. 8103).

.PARAMETER User
    Db2 user ID with BINDADD authority on the target server.

.PARAMETER Password
    Password for the Db2 user. If omitted, the script will prompt securely.

.EXAMPLE
    .\Bind-Db2Packages.ps1 -Database DBD1LOC -Server 10.10.13.2 -Port 8103 -User SCOTT

.EXAMPLE
    .\Bind-Db2Packages.ps1 -Database DBD1LOC -Server 10.10.13.2 -Port 8103 `
        -User SCOTT -Password mlkhbu `
        -AppBinDir "C:\MyApp\bin\Release\net10.0"
#>

[CmdletBinding()]
param(
    [string]$AppBinDir = (Join-Path $PSScriptRoot "Db2ConnTest\bin\Release\net10.0"),

    [Parameter(Mandatory)]
    [string]$Database,

    [Parameter(Mandatory)]
    [string]$Server,

    [Parameter(Mandatory)]
    [int]$Port,

    [Parameter(Mandatory)]
    [string]$User,

    [string]$Password
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Resolve paths ────────────────────────────────────────────────────────────
$cliDriverBin = Join-Path $AppBinDir "clidriver\bin\db2cli.exe"
$bndDir        = Join-Path $AppBinDir "clidriver\bnd"

if (-not (Test-Path $cliDriverBin)) {
    Write-Error "db2cli.exe not found at: $cliDriverBin`nEnsure AppBinDir points to the net10.0 output folder that contains the clidriver directory."
}

if (-not (Test-Path $bndDir)) {
    Write-Error "Bind file directory not found at: $bndDir"
}

# ── Prompt for password if not supplied ──────────────────────────────────────
if (-not $Password) {
    $secPwd = Read-Host "Enter password for $User" -AsSecureString
    $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secPwd))
}

# ── Bind each .bnd file ──────────────────────────────────────────────────────
$bndFiles = Get-ChildItem "$bndDir\*.bnd" | Sort-Object Name
if ($bndFiles.Count -eq 0) {
    Write-Error "No .bnd files found in: $bndDir"
}

Write-Host ""
Write-Host "IBM Db2 CLI Package Bind Utility" -ForegroundColor Cyan
Write-Host "Driver : $cliDriverBin"
Write-Host "Target : $Database @ $Server`:$Port"
Write-Host "User   : $User"
Write-Host "Packages: $($bndFiles.Count) bind file(s) found"
Write-Host ("-" * 60)

$overallErrors   = 0
$overallWarnings = 0

foreach ($bnd in $bndFiles) {
    Write-Host ""
    Write-Host "Binding $($bnd.Name) ..." -ForegroundColor Yellow

    $output = & $cliDriverBin bind $bnd.FullName `
        -database "$Database`:$Server`:$Port" `
        -user $User `
        -passwd $Password `
        -options "GRANT PUBLIC" 2>&1

    $output | ForEach-Object { Write-Host "  $_" }

    # Parse the summary line for errors/warnings
    $summary = $output | Where-Object { $_ -match 'SQL0091N' }
    if ($summary -match '"(\d+)" errors and "(\d+)" warnings') {
        $errs  = [int]$Matches[1]
        $warns = [int]$Matches[2]
        $overallErrors   += $errs
        $overallWarnings += $warns

        if ($errs -gt 0) {
            Write-Host "  ^^^ $errs error(s) — see output above" -ForegroundColor Red
        } elseif ($warns -gt 0) {
            Write-Host "  Completed with $warns warning(s) (typically harmless on z/OS)" -ForegroundColor DarkYellow
        } else {
            Write-Host "  OK" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host ("-" * 60)
if ($overallErrors -eq 0) {
    Write-Host "Bind complete. Total warnings: $overallWarnings. No errors." -ForegroundColor Green
    Write-Host "The application is ready to connect to $Database."
} else {
    Write-Host "Bind finished with $overallErrors total error(s). Review output above." -ForegroundColor Red
    exit 1
}
