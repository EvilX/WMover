#Requires -Version 5.1
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$Root   = $PSScriptRoot
$Layout = Join-Path $Root "bin\SetupLayout"
$ISCC   = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$env:PATH += ";C:\Program Files\dotnet"

if (-not (Test-Path $ISCC)) { throw "Inno Setup not found: $ISCC" }

# ── 1. Publish ───────────────────────────────────────────────────────────────
Write-Host "`n[1/3] Publishing..." -ForegroundColor Cyan

if (Test-Path $Layout) { Remove-Item $Layout -Recurse -Force }

dotnet publish "$Root\src\WMover.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -o $Layout --nologo

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
Remove-Item (Join-Path $Layout "*.pdb") -ErrorAction SilentlyContinue

# ── 2. Patch version in .iss ─────────────────────────────────────────────────
Write-Host "[2/3] Compiling installer..." -ForegroundColor Cyan

$iss = Get-Content "$Root\Setup.iss" -Raw
$iss = $iss -replace 'AppVersion=[\d.]+',       "AppVersion=$Version"
$iss = $iss -replace 'OutputBaseFilename=\S+',  "OutputBaseFilename=WMover_Setup_$($Version -replace '\.','_')"
$tmp = Join-Path $Root "_Setup_tmp.iss"
$iss | Set-Content $tmp -Encoding UTF8

# ── 3. Build installer ───────────────────────────────────────────────────────
Push-Location $Root
& $ISCC $tmp
$code = $LASTEXITCODE
Pop-Location
Remove-Item $tmp -ErrorAction SilentlyContinue
if ($code -ne 0) { throw "ISCC failed." }

$out = Get-ChildItem "$Root\bin\WMover_Setup_*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "`nDone!  $($out.FullName)  ($([Math]::Round($out.Length/1MB,1)) MB)" -ForegroundColor Green
