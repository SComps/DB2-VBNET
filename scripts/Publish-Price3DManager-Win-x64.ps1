<#
.SYNOPSIS
    Self-contained publish for Price3DManager on Windows x64.

.DESCRIPTION
    Produces a self-contained executable targeting Windows x64.
    Output lands in Price3DManager\bin\publish\win-x64\.

    NOTE: PublishSingleFile is explicitly disabled because IBM.Data.Db2
    uses Assembly.CodeBase internally, which throws NotSupportedException
    when loaded from a single-file bundle.

    Requirements:
      - .NET 10 SDK
      - Run on a Windows x64 machine

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Publish-Price3DManager-Win-x64.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectFile = Join-Path $PSScriptRoot "..\Price3DManager\Price3DManager.vbproj"
$outputDir   = Join-Path $PSScriptRoot "..\Price3DManager\bin\publish\win-x64"

Write-Host "Publishing Price3DManager for Windows x64 (self-contained)..." -ForegroundColor Cyan
Write-Host "Project : $projectFile"
Write-Host "Output  : $outputDir"
Write-Host ""

dotnet publish $projectFile `
    --configuration $Configuration `
    --runtime win-x64 `
    --output $outputDir `
    --self-contained true `
    -p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Publish complete." -ForegroundColor Green
Write-Host "Executable: $(Join-Path $outputDir 'Price3DManager.exe')"
