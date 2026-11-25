#Requires -Version 5.1
<#
.SYNOPSIS
    Builds WMover and compiles the Inno Setup installer.

.PARAMETER Version
    App version in X.Y.Z format. Defaults to <Version> in src\WMover.csproj.

.EXAMPLE
    .\Publish-Setup.ps1
    .\Publish-Setup.ps1 -Version 1.2.0
#>
param([string]$Version)

$ErrorActionPreference = "Stop"
$Root    = $PSScriptRoot
$Project = Join-Path $Root "src\WMover.csproj"
$Layout  = Join-Path $Root "bin\SetupLayout"
$env:PATH += ";C:\Program Files\dotnet"

# ISCC.exe: machine-wide install, per-user install (winget --scope user), or PATH
$ISCC = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $ISCC) { $ISCC = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source }
if (-not $ISCC) { throw "Inno Setup 6 not found (ISCC.exe)." }

if (-not $Version) { $Version = ([xml](Get-Content $Project -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1 }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must be X.Y.Z, got '$Version'." }
Write-Host "Version: $Version" -ForegroundColor Cyan

# -- 1. Publish ---------------------------------------------------------------
Write-Host "`n[1/2] Publishing..." -ForegroundColor Cyan

if (Test-Path $Layout) { Remove-Item $Layout -Recurse -Force }

dotnet publish $Project `
    -c Release -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -p:Version=$Version `
    -o $Layout --nologo

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
Remove-Item (Join-Path $Layout "*.pdb") -ErrorAction SilentlyContinue

# -- 2. Build installer -------------------------------------------------------
Write-Host "[2/2] Compiling installer..." -ForegroundColor Cyan

& $ISCC "/DAppVer=$Version" (Join-Path $Root "Setup.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC failed." }

$out = Join-Path $Root "bin\WMover_Setup_$($Version -replace '\.','_').exe"
Write-Host "`nDone!  $out  ($([Math]::Round((Get-Item $out).Length/1MB,1)) MB)" -ForegroundColor Green
