<#
.SYNOPSIS
    AOT publish for Windows x64.

.DESCRIPTION
    Produces a self-contained native executable targeting Windows x64.
    Output lands in Db2ConnTest\bin\publish\win-x64\.

    Requirements:
      - .NET 10 SDK
      - Visual C++ build tools (installed with Visual Studio 2026)
      - Run on a Windows x64 machine
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectFile = Join-Path $PSScriptRoot "..\Db2ConnTest\Db2ConnTest.vbproj"
$outputDir   = Join-Path $PSScriptRoot "..\Db2ConnTest\bin\publish\win-x64"

Write-Host "Building AOT native binary for Windows x64..." -ForegroundColor Cyan
Write-Host "Project : $projectFile"
Write-Host "Output  : $outputDir"
Write-Host ""

dotnet publish $projectFile `
    --configuration $Configuration `
    --runtime win-x64 `
    --output $outputDir `
    --self-contained true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "AOT publish complete." -ForegroundColor Green
Write-Host "Executable: $(Join-Path $outputDir 'Db2ConnTest.exe')"
