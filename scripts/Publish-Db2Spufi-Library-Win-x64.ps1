<#
.SYNOPSIS
    Publishes the Db2Spufi.Core library and LINQ dependencies for Windows x64.

.DESCRIPTION
    Builds and publishes only the Db2Spufi.Core class library, ADO.NET dependencies,
    XML documentation, and NuGet package for import into external applications,
    excluding sample/GUI applications (WinForms, Avalonia, Price3DManager).

    Output directories:
      - DLLs & Native Assets: publish\library\win-x64\
      - NuGet Package (.nupkg): publish\library\nupkg\

.PARAMETER Configuration
    Build configuration (Debug/Release). Defaults to Release.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\Publish-Db2Spufi-Library-Win-x64.ps1
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$coreProject = Join-Path $PSScriptRoot "..\Db2Spufi.Core\Db2Spufi.Core.vbproj"
$outputDir   = Join-Path $PSScriptRoot "..\publish\library\win-x64"
$nupkgDir    = Join-Path $PSScriptRoot "..\publish\library\nupkg"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Publishing Db2Spufi.Core Library for Windows x64" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Project : $coreProject"
Write-Host "Output  : $outputDir"
Write-Host "Nupkg   : $nupkgDir"
Write-Host ""

if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
New-Item -ItemType Directory -Path $nupkgDir -Force | Out-Null

# 1. Publish library binaries & runtime assets for win-x64
Write-Host "Step 1: Publishing binaries and Db2 runtime dependencies..." -ForegroundColor Yellow
dotnet publish $coreProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --output $outputDir `
    --self-contained false `
    -p:GenerateDocumentationFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Library publish failed with exit code $LASTEXITCODE"
}

# 2. Build NuGet package for external project references
Write-Host "Step 2: Building NuGet package (.nupkg)..." -ForegroundColor Yellow
dotnet pack $coreProject `
    --configuration $Configuration `
    --output $nupkgDir `
    -p:IncludeSymbols=true `
    -p:SymbolPackageFormat=snupkg

if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet pack failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " Db2Spufi.Core Library Published Successfully!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Binary Location : $outputDir"
Write-Host "  - Db2Spufi.Core.dll (Core Engine & LINQ to Db2)"
Write-Host "  - Db2Spufi.Core.xml (Developer Intellisense Docs)"
Write-Host "  - Net.IBM.Data.Db2.dll (IBM ADO.NET Provider)"
Write-Host "NuGet Location  : $nupkgDir"
Write-Host ""
