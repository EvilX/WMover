# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**WMover** — a Windows 11 system-tray utility that snaps windows to fixed size/position presets via global hotkeys. Single-instance WinForms app (net8.0-windows, WinExe, x64 only).

## Common Commands

```powershell
# Build (debug)
dotnet build

# Run locally
dotnet run

# Build Inno Setup installer → bin\WMover_Setup_X_Y_Z.exe
.\Publish-Setup.ps1 -Version "1.1.1"

# Build MSIX package → bin\WMover_X.Y.Z.0.msix
.\Publish-Store.ps1 -Version "1.1.1.0" -PublisherCN "CN=..."
```

`Publish-Setup.ps1` requires Inno Setup 6 at `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`. It patches version/filename into a temp `.iss` before calling ISCC.

There are no automated tests in this project.

## Architecture

**Single hidden form + global hook.** `MainForm` is a 1×1, opacity-0 window that never shows; it owns the tray icon and installs a `WH_KEYBOARD_LL` hook. The hook callback runs on the UI thread, checks modifier state with `GetAsyncKeyState`, matches bindings stored in `_bindings`, and calls `WindowManager.Apply()`.

**Data flow:**

```
WH_KEYBOARD_LL hook
  └─ MainForm.HookCallback
       └─ WindowManager.Apply(action)
            ├─ GetForegroundWindow / IsFullscreen / IsZoomed
            ├─ GetWorkArea (monitor work area, excludes taskbar)
            ├─ GetBorderInsets (DWM invisible frame compensation)
            └─ SetWindowPos (visual coords → outer rect)
```

**DWM border math** (`WindowManager.cs`): `GetWindowRect` includes ~7 px invisible DWM frame on sides/bottom; `DWMWA_EXTENDED_FRAME_BOUNDS` (=9) gives the visual rect. `GetBorderInsets` computes the delta. All snap positions are calculated in visual coordinates, then compensated before `SetWindowPos` (`vX - bL, vY - bT, vW + bL + bR, vH + bT + bB`).

**Key files:**

| File | Role |
|---|---|
| `MainForm.cs` | Tray icon, keyboard hook, theme detection, settings/about dialogs |
| `WindowManager.cs` | All SetWindowPos logic; DWM border compensation |
| `NativeMethods.cs` | All P/Invoke — do not scatter Win32 calls elsewhere |
| `HotkeyBinding.cs` | Binding data model + `Defaults()` static factory |
| `AppSettings.cs` | JSON persistence to `%LOCALAPPDATA%\WMover\settings.json` |
| `SettingsForm.cs` | Interactive key-capture ListView for reassigning shortcuts |
| `StartupManager.cs` | HKCU Run registry entry toggle |

**Tray icon** is rendered at runtime via GDI+ (white on dark mode, black on light mode). `MainForm` listens for `WM_SETTINGCHANGE` "ImmersiveColorSet" to regenerate it.

**Alt+Tab suppression**: `MainForm` overrides `CreateParams` to set `WS_EX_TOOLWINDOW` (0x80) in `ExStyle`. `ShowInTaskbar = false` alone is insufficient.

## ICO File Requirements

`Icon.ico` must use **BMP/DIB format frames** (BITMAPINFOHEADER, `biHeight = h * 2` for XOR+AND masks), NOT PNG-embedded frames. Inno Setup embeds the icon as a Windows `RT_ICON` resource, which only accepts the DIB format — a PNG-embedded ICO silently drops the icon in the compiled Setup.exe. Explorer accepts both formats; Inno Setup does not.

## Packaging Notes

- **Version** lives in two places: `Setup.iss` (`AppVersion=`) and `Package\AppxManifest.xml` (`Version=`). `Publish-Setup.ps1` patches `Setup.iss` automatically; `Publish-Store.ps1` patches the manifest. Bump both manually when incrementing the version.
- MSIX requires `runFullTrust` capability (in `AppxManifest.xml`) for the global keyboard hook to work from the Store.
- Self-contained publish (`--self-contained true`) means no .NET runtime install needed on target machine.
