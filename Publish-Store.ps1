#Requires -Version 5.1
<#
.SYNOPSIS
    Builds WMover and packages it as an MSIX for Microsoft Store submission.

.PARAMETER Version
    Package version in X.Y.Z.0 format (default: 1.0.0.0).

.PARAMETER PublisherCN
    Publisher identity CN from Microsoft Partner Center.
    Example: "CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
    Leave default for local testing (unsigned package, not for Store upload).

.EXAMPLE
    .\Publish-Store.ps1
    .\Publish-Store.ps1 -Version 1.2.0.0 -PublisherCN "CN=Your-Partner-Center-CN"
#>
param(
    [string]$Version = "1.0.0.0",
    [string]$PublisherCN = "CN=E6AE4792-1CF2-4206-9D79-A11A16CD8C6A"
)

$ErrorActionPreference = "Stop"

$Root    = $PSScriptRoot
$Layout  = Join-Path $Root "bin\StoreLayout"
$Output  = Join-Path $Root "bin\WMover_$($Version -replace '\.','_').msix"
$MakeAppx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe"

# ── 1. Validate tools ────────────────────────────────────────────────────────
if (-not (Test-Path $MakeAppx)) {
    throw "makeappx.exe not found at: $MakeAppx`nInstall Windows SDK 10.0.22621."
}

# ── 2. Publish .NET app ──────────────────────────────────────────────────────
Write-Host "`n[1/4] Publishing..." -ForegroundColor Cyan

if (Test-Path $Layout) { Remove-Item $Layout -Recurse -Force }

$env:PATH += ";C:\Program Files\dotnet"
dotnet publish "$Root\WMover.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=false `
    -o $Layout `
    --nologo

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Remove dev artifacts not needed in the package
Remove-Item (Join-Path $Layout "*.pdb") -ErrorAction SilentlyContinue

# ── 3. Generate assets if not customised ────────────────────────────────────
Write-Host "[2/4] Preparing assets..." -ForegroundColor Cyan

$customAssets = Join-Path $Root "Package\Assets"
$layoutAssets = Join-Path $Layout "Assets"
New-Item -ItemType Directory -Force $layoutAssets | Out-Null

$customFiles = Get-ChildItem $customAssets -Filter "*.png" -ErrorAction SilentlyContinue

if ($customFiles.Count -gt 0) {
    Copy-Item "$customAssets\*" $layoutAssets -Force
    Write-Host "  Using custom assets from Package\Assets\" -ForegroundColor Green
} else {
    Write-Host "  Generating placeholder assets (replace with your own in Package\Assets\)" -ForegroundColor Yellow
    Add-Type -AssemblyName System.Drawing

    function New-Asset([string]$path, [int]$w, [int]$h) {
        $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g   = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::FromArgb(0, 120, 215))
        $font = New-Object System.Drawing.Font("Segoe UI", [Math]::Max(8, $h * 0.35), [System.Drawing.FontStyle]::Bold)
        $sf = New-Object System.Drawing.StringFormat
        $sf.Alignment     = [System.Drawing.StringAlignment]::Center
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rect = [System.Drawing.RectangleF]::new(0, 0, $w, $h)
        $g.DrawString("M", $font, [System.Drawing.Brushes]::White, $rect, $sf)
        $g.Dispose(); $font.Dispose(); $sf.Dispose()
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    New-Asset (Join-Path $layoutAssets "Square44x44Logo.png")   44  44
    New-Asset (Join-Path $layoutAssets "Square150x150Logo.png") 150 150
    New-Asset (Join-Path $layoutAssets "Wide310x150Logo.png")   310 150
    New-Asset (Join-Path $layoutAssets "StoreLogo.png")          50  50
    New-Asset (Join-Path $layoutAssets "SplashScreen.png")      620 300
}

# ── 4. Patch and copy manifest ───────────────────────────────────────────────
Write-Host "[3/4] Writing manifest..." -ForegroundColor Cyan

$xml = Get-Content "$Root\Package\AppxManifest.xml" -Raw
$xml = $xml -replace 'Publisher="CN=Publisher"',   "Publisher=`"$PublisherCN`""
$xml = $xml -replace 'Version="1\.0\.0\.0"',        "Version=`"$Version`""
$xml | Set-Content (Join-Path $Layout "AppxManifest.xml") -Encoding UTF8

# ── 5. Pack MSIX ────────────────────────────────────────────────────────────
Write-Host "[4/4] Packing MSIX..." -ForegroundColor Cyan

if (Test-Path $Output) { Remove-Item $Output -Force }

& $MakeAppx pack /d $Layout /p $Output /nv /o 2>&1

if ($LASTEXITCODE -ne 0) { throw "makeappx failed." }

# ── Done ─────────────────────────────────────────────────────────────────────
$size = [Math]::Round((Get-Item $Output).Length / 1MB, 1)
Write-Host "`nDone!  $Output  ($size MB)" -ForegroundColor Green
Write-Host @"

Next steps for Microsoft Store submission:
  1. Register at https://partner.microsoft.com
  2. Reserve your app name and copy your Publisher identity CN
  3. Run:  .\Publish-Store.ps1 -PublisherCN "CN=<your-cn>"
  4. Upload the .msix to Partner Center

Note: replace placeholder icons in Package\Assets\ with your own before submission.
"@ -ForegroundColor DarkCyan
