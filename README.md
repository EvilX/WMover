<p align="center">
  <img src="src/Package/Assets/Square44x44Logo.targetsize-256.png" width="96" height="96" alt="WMover icon">
</p>

# WMover

WMover is a small Windows 11 tray utility that instantly resizes and moves the active window
to fixed layouts (halves, thirds, two-thirds, center) using global keyboard shortcuts.

- Lives in the system tray; no main window, not shown in the taskbar or Alt+Tab.
- Works on the monitor the window is on and respects the taskbar (uses the monitor work area).
- Compensates for the invisible Windows 11 window border, so windows line up edge to edge.
- Ignores fullscreen apps/games, always-on-top windows and elevated (admin) windows.
- All shortcuts can be reassigned in **Settings**.
- Optional autostart with Windows.


## Install

- **Microsoft Store:** [WMover on the Microsoft Store](https://apps.microsoft.com/store/detail/9NKWN112GR1Q?cid=DevShareMCLPCS)
- **Installer:** download `WMover_Setup_X_Y_Z.exe` from [GitHub Releases](https://github.com/EvilX/WMover/releases)
  (per-user install, no admin rights required).

Requires Windows 11 (build 26200 or newer).


## Shortcuts (defaults)

| Shortcut | Action |
|---|---|
| Win + ← | 1/2 width — snap left |
| Win + → | 1/2 width — snap right |
| Ctrl + Win + E | 2/3 width — snap left |
| Ctrl + Win + T | 2/3 width — snap right |
| Ctrl + Win + R | 2/3 width — center |
| Ctrl + Win + D | 1/3 width — snap left |
| Ctrl + Win + G | 1/3 width — snap right |
| Ctrl + Win + F | 1/3 width — center |
| Ctrl + Win + C | Center (keep size) |

All layouts use the full height of the work area.


## Tray menu

- **Run at Windows startup** — toggles autostart (HKCU `Run` key for the classic install,
  `StartupTask` for the Store/MSIX version).
- **Settings** — reassign shortcuts: click a row, press the new key combination (`Esc` cancels),
  then **OK**. **Reset defaults** restores the built-in set.
- **About**
- **Exit**


## Data files

| Path | Contents |
|---|---|
| `%LOCALAPPDATA%\WMover\settings.json` | Custom shortcuts |
| `%LOCALAPPDATA%\WMover\crash.log` | Unhandled exceptions |


## Build

**Requirements:** Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet build src\WMover.csproj
dotnet run --project src\WMover.csproj
```

Self-contained single-file executable:

```powershell
dotnet publish src\WMover.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `src\bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\WMover.exe`.

The app is published for `win-x64`; on Windows on ARM it runs under x64 emulation.


## Installer (Inno Setup)

**Requirements:** [Inno Setup 6](https://jrsoftware.org/isinfo.php) installed to the default path, .NET 8 SDK

```powershell
.\Publish-Setup.ps1                  # version from src\WMover.csproj
.\Publish-Setup.ps1 -Version 1.2.0   # override
```

Output: `bin\WMover_Setup_1_1_10.exe` (per-user install to `%LOCALAPPDATA%\Programs\WMover`,
no admin rights required).


## Microsoft Store package (MSIX)

**Requirements:** Windows SDK 10.0.22621 or newer (provides `makeappx.exe`), .NET 8 SDK

```powershell
.\Publish-Store.ps1                                       # version from src\WMover.csproj + ".0"
.\Publish-Store.ps1 -Version 1.2.0.0 -PublisherCN "CN=<your-cn>"
```

Output: `bin\WMover_1_1_10_0.msix` (unsigned — sign it or upload it to Partner Center).

Store assets live in `src\Package\Assets\`, the manifest in `src\Package\AppxManifest.xml`.


## Releasing a new version

The version lives in one place — `<Version>` in `src\WMover.csproj`. The About dialog reads it
from the assembly, `Publish-Setup.ps1` passes it to Inno Setup, `Publish-Store.ps1` writes it into
the MSIX manifest.

1. Bump `<Version>` in `src\WMover.csproj` and add an entry to `CHANGELOG`.
2. Commit, tag and push:

   ```bash
   git tag v1.2.0
   git push origin main v1.2.0
   ```

3. GitHub Actions (`.github/workflows/build.yml`) builds `WMover_Setup_1_2_0.exe` and
   `WMover_1_2_0_0.msix` and publishes them as a GitHub Release. The tag must match the csproj
   version, otherwise the job fails.
4. Upload the `.msix` to Partner Center for the Store.


## CI/CD

| Trigger | What runs |
|---|---|
| push / PR to `main` | `dotnet build` (warnings are errors) |
| tag `vX.Y.Z` | Setup.exe + MSIX → GitHub Release |
| manual (*Run workflow*) | Setup.exe + MSIX → workflow artifacts only |

The installer and MSIX are not code-signed: Windows SmartScreen will warn on `Setup.exe`, and the
`.msix` is meant for Store submission (Partner Center signs it), not for sideloading.
