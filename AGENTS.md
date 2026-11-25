# AGENTS.md

Guidance for AI coding agents (Claude Code, Codex, etc.) working in this repository.
`CLAUDE.md` imports this file — keep the content here.

## Project

**WMover** — a Windows 11 system-tray utility that snaps the foreground window to fixed
size/position presets via global hotkeys. Single-instance WinForms app, `WinExe`,
target `net8.0-windows10.0.17763.0` (the Windows TFM is needed for the WinRT `StartupTask` API),
published as `win-x64` only.

Original (Russian) functional spec: `PROJECT.md`. It describes `Win+Alt+←/→` "fill to nearest
window" actions that are **not implemented**.

## Repository layout

```
Magnet.sln              solution (historical name) → src\WMover.csproj
Setup.iss               Inno Setup script (classic installer), version via /DAppVer
.github\workflows\build.yml   CI: build on push/PR, Setup.exe + MSIX release on tag vX.Y.Z
Publish-Setup.ps1       publish + Inno Setup → bin\WMover_Setup_X_Y_Z.exe
Publish-Store.ps1       publish + makeappx   → bin\WMover_X_Y_Z_0.msix
CHANGELOG
src\
  WMover.csproj
  *.cs                  all source (flat, namespace WMover)
  Icon.ico
  Package\AppxManifest.xml, Package\Assets\*.png   MSIX manifest and Store assets
```

`src\WMover.csproj.lscache` is a C# Dev Kit cache; don't edit it by hand.

## Common commands

```powershell
dotnet build src\WMover.csproj
dotnet run --project src\WMover.csproj

.\Publish-Setup.ps1     # needs Inno Setup 6 (Program Files, %LOCALAPPDATA%\Programs or PATH)
.\Publish-Store.ps1     # needs Windows SDK makeappx.exe (newest 10.* SDK is picked)
# both take an optional -Version (X.Y.Z / X.Y.Z.0); default comes from src\WMover.csproj
```

There are no automated tests. The project only builds on Windows (WinForms + Windows TFM);
from macOS/Linux build on a Windows machine/VM (see below).

## Architecture

**Single hidden form + global hook.** `MainForm` is a 1×1, opacity-0 window that never shows;
it owns the tray icon and installs a `WH_KEYBOARD_LL` hook. The hook callback runs on the UI
thread, checks modifier state with `GetAsyncKeyState`, matches bindings in `_bindings`, and calls
`WindowManager.Apply()`. If `Apply` returns `true` the key is swallowed (returns 1), so Windows'
own Win+←/→ snap does not fire; if the window was skipped, the key is passed on to the system.

```
WH_KEYBOARD_LL hook
  └─ MainForm.HookCallback
       ├─ SettingsForm.KeyCaptureCallback / EscCaptureCallback   (while reassigning a shortcut)
       └─ WindowManager.Apply(action)
            ├─ GetForegroundWindow; skip if IsFullscreen / IsTopmost / IsElevated
            ├─ GetWorkArea (monitor work area, excludes taskbar) — captured BEFORE SW_RESTORE
            ├─ IsZoomed → ShowWindow(SW_RESTORE)
            ├─ GetBorderInsets (DWM invisible frame compensation)
            └─ SetWindowPos (visual coords → outer rect)
```

**DWM border math** (`WindowManager.cs`): `GetWindowRect` includes the ~7 px invisible DWM frame
on sides/bottom; `DWMWA_EXTENDED_FRAME_BOUNDS` (=9) gives the visual rect. `GetBorderInsets`
computes the delta. All snap positions are calculated in visual coordinates, then compensated
before `SetWindowPos` (`vX - bL, vY - bT, vW + bL + bR, vH + bT + bB`).

**Elevated windows**: a non-elevated process can't move them (UIPI); `IsElevated` treats
`OpenProcess` access-denied as elevated and skips the window.

**Single instance**: named mutex `WMoverWindowManager-{A3F2C1D0}` in `Program.cs`.
Unhandled exceptions are appended to `%LOCALAPPDATA%\WMover\crash.log`.

### Key files

| File | Role |
|---|---|
| `src/Program.cs` | Entry point, single-instance mutex, crash logging, PerMonitorV2 DPI |
| `src/MainForm.cs` | Tray icon + menu, keyboard hook, theme detection |
| `src/WindowManager.cs` | `WindowAction` enum, all SetWindowPos logic, DWM border compensation |
| `src/NativeMethods.cs` | All P/Invoke — do not scatter Win32 calls elsewhere |
| `src/HotkeyBinding.cs` | Binding model + `Defaults()` factory |
| `src/AppSettings.cs` | JSON persistence to `%LOCALAPPDATA%\WMover\settings.json` |
| `src/SettingsForm.cs` | Key-capture ListView for reassigning shortcuts |
| `src/StartupManager.cs` | Autostart: HKCU `Run` key (unpackaged) or WinRT `StartupTask` (MSIX) |
| `src/AboutForm.cs` | About dialog (version read from the assembly) |
| `src/HelpForm.cs` | Shortcut list dialog — currently **not referenced** from the tray menu |

### Adding a new action

1. Add a value to `WindowAction` and a `case` in `WindowManager.Apply`.
2. Add a default binding in `HotkeyBinding.Defaults()`.
3. Note: `AppSettings.Load` discards `settings.json` and falls back to defaults if the saved
   count differs from `Defaults().Count` — adding an action resets users' custom shortcuts.
4. Update the shortcut table in `README.md`.

### UI details

- **Tray icon** is drawn at runtime via GDI+ (white on dark theme, black on light).
  `MainForm` listens for `WM_SETTINGCHANGE` "ImmersiveColorSet" to regenerate it.
- **Alt+Tab suppression**: `MainForm` overrides `CreateParams` to set `WS_EX_TOOLWINDOW` (0x80).
  `ShowInTaskbar = false` alone is insufficient.

### Autostart (packaged vs. unpackaged)

`StartupManager.IsPackaged()` calls `GetCurrentPackageFullName`. Under MSIX the app can't
use the Run key reliably, so it uses `StartupTask` with `TaskId="WMoverStartupTask"`, which must
match the `uap5:StartupTask` extension in `AppxManifest.xml`. The Inno Setup installer writes
the HKCU `Run` value itself (`[Registry]` section, `startup` task).

## ICO file requirements

`src/Icon.ico` must use **BMP/DIB frames** (BITMAPINFOHEADER, `biHeight = h * 2` for XOR+AND
masks), NOT PNG-embedded frames. Inno Setup embeds the icon as an `RT_ICON` resource, which only
accepts DIB — a PNG-embedded ICO silently drops the icon in the compiled Setup.exe.

## Versioning and packaging

**Single source of the version: `<Version>` in `src/WMover.csproj`** (X.Y.Z). Do not hard-code
it anywhere else.

- `AboutForm` shows `Assembly.GetName().Version.ToString(3)`.
- `Publish-Setup.ps1` reads the csproj (or `-Version`), passes `-p:Version=` to `dotnet publish`
  and `/DAppVer=` to ISCC. `Setup.iss` falls back to the published exe's version when compiled
  directly.
- `Publish-Store.ps1` uses csproj version + `.0` (Store requires revision 0), passes
  `-p:Version=` and patches `<Identity Version=` in the copied manifest. The repo manifest keeps the
  placeholder `Version="0.0.0.0"`.
- Release = bump csproj + `CHANGELOG`, push tag `vX.Y.Z`; CI (`.github/workflows/build.yml`)
  builds Setup.exe + MSIX and creates a GitHub Release; it fails if the tag ≠ csproj version.

Gotchas:

- `Publish-Store.ps1` replaces `Publisher="CN=Publisher"` with `-PublisherCN` (default is the
  Partner Center CN). Don't commit a real CN into the manifest.
- Manifest and `Setup.iss` require Windows build ≥ 10.0.26200 (`MinVersion`).
- MSIX needs the `runFullTrust` capability for the global keyboard hook.
- Setup.exe and MSIX are not code-signed (no certificate in CI).
- Publishing is self-contained (`--self-contained true`) — no .NET runtime needed on the target.

## Building on a Windows VM from macOS

A Windows 11 ARM64 VM (VMware Fusion) is reachable via SSH (`ssh 192.168.86.128`, user `leoni`,
`cmd.exe` shell). The .NET 8 SDK is installed per-user in `%LOCALAPPDATA%\Microsoft\dotnet`
(not on `PATH` by default). Inno Setup and the Windows SDK are not installed there.

```bash
git archive --format=zip -o /tmp/wmover.zip HEAD
ssh 192.168.86.128 "if exist wmover-build rmdir /s /q wmover-build & mkdir wmover-build"
scp /tmp/wmover.zip 192.168.86.128:C:/Users/leoni/wmover-build/wmover.zip
ssh 192.168.86.128 "cd wmover-build && tar -xf wmover.zip && set PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH% && dotnet publish src\WMover.csproj -c Release -r win-x64 --self-contained true -o bin\SetupLayout --nologo"
```

`git archive` packs only committed files — commit (or use a working-tree copy) to build local
changes. The x64 build runs on ARM64 Windows under emulation. GUI/hotkeys can't be tested
through SSH (it runs in a non-interactive session); test in the VM console.
