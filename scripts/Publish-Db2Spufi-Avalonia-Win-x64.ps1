<#
.SYNOPSIS
    Self-contained publish for Db2Spufi.Avalonia on Windows x64.

.DESCRIPTION
    Produces a self-contained executable targeting Windows x64.
    Output lands in Db2Spufi.Avalonia\bin\publish\win-x64\.

    NOTE: PublishSingleFile is explicitly disabled because IBM.Data.Db2
    uses Assembly.CodeBase internally, which throws NotSupportedException
    when loaded from a single-file bundle.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Publish-Db2Spufi-Avalonia-Win-x64.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectFile = Join-Path $PSScriptRoot "..\Db2Spufi.Avalonia\Db2Spufi.Avalonia.csproj"
$outputDir   = Join-Path $PSScriptRoot "..\Db2Spufi.Avalonia\bin\publish\win-x64"

Write-Host "Publishing Db2Spufi.Avalonia for Windows x64 (self-contained)..." -ForegroundColor Cyan
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
Write-Host "Executable: $(Join-Path $outputDir 'Db2Spufi.Avalonia.exe')"
