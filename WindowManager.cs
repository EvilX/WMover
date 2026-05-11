using System.Runtime.InteropServices;

namespace WMover;

internal enum WindowAction
{
    TwoThirdsLeft,
    TwoThirdsRight,
    TwoThirdsCenter,
    OneThirdLeft,
    OneThirdRight,
    OneThirdCenter,
    CenterOnly,
    HalfLeft,
    HalfRight,
}

internal static class WindowManager
{
    public static bool Apply(WindowAction action)
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero || IsFullscreen(hWnd) || IsTopmost(hWnd) || IsElevated(hWnd))
            return false;

        // Capture work area before restoring: IsZoomed puts the window on the target
        // monitor, but SW_RESTORE would move it back to its pre-maximize position,
        // possibly on a different monitor.
        var work = GetWorkArea(hWnd);

        if (NativeMethods.IsZoomed(hWnd))
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
        int w = work.Width, h = work.Height, x = work.Left, y = work.Top;

        // Invisible DWM border: SetWindowPos uses the outer rect, but the visible
        // edge is inset by bL/bT/bR/bB pixels. Work in visual coordinates here,
        // then compensate once before calling SetWindowPos.
        var (bL, bT, bR, bB) = GetBorderInsets(hWnd);

        int vX, vY, vW, vH; // visual (visible) bounds

        switch (action)
        {
            case WindowAction.TwoThirdsLeft:
                (vW, vH, vX, vY) = (w * 2 / 3, h, x, y);
                break;
            case WindowAction.TwoThirdsRight:
                vW = w * 2 / 3;
                (vH, vX, vY) = (h, x + w - vW, y);
                break;
            case WindowAction.TwoThirdsCenter:
                vW = w * 2 / 3;
                (vH, vX, vY) = (h, x + (w - vW) / 2, y);
                break;
            case WindowAction.OneThirdLeft:
                (vW, vH, vX, vY) = (w / 3, h, x, y);
                break;
            case WindowAction.OneThirdRight:
                vW = w / 3;
                (vH, vX, vY) = (h, x + w - vW, y);
                break;
            case WindowAction.OneThirdCenter:
                vW = w / 3;
                (vH, vX, vY) = (h, x + (w - vW) / 2, y);
                break;
            case WindowAction.HalfLeft:
                (vW, vH, vX, vY) = (w / 2, h, x, y);
                break;
            case WindowAction.HalfRight:
                vW = w / 2;
                (vH, vX, vY) = (h, x + w - vW, y);
                break;
            case WindowAction.CenterOnly:
                NativeMethods.GetWindowRect(hWnd, out var rect);
                vW = Math.Min(rect.Width  - bL - bR, w);
                vH = Math.Min(rect.Height - bT - bB, h);
                (vX, vY) = (x + (w - vW) / 2, y + (h - vH) / 2);
                break;
            default:
                return false;
        }

        // Convert visual bounds → outer window rect by adding back the border insets
        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero,
            vX - bL, vY - bT, vW + bL + bR, vH + bT + bB,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        return true;
    }

    private static bool IsTopmost(IntPtr hWnd) =>
        ((uint)NativeMethods.GetWindowLongPtr(hWnd, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOPMOST) != 0;

    private static bool IsElevated(IntPtr hWnd)
    {
        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0) return false;

        var hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProc == IntPtr.Zero) return true; // access denied → elevated

        try
        {
            if (!NativeMethods.OpenProcessToken(hProc, NativeMethods.TOKEN_QUERY, out var hToken))
                return false;
            try
            {
                uint elevated = 0, sz = sizeof(uint);
                NativeMethods.GetTokenInformation(hToken, NativeMethods.TOKEN_ELEVATION_CLASS,
                    ref elevated, sz, out _);
                return elevated != 0;
            }
            finally { NativeMethods.CloseHandle(hToken); }
        }
        finally { NativeMethods.CloseHandle(hProc); }
    }

    private static bool IsFullscreen(IntPtr hWnd)
    {
        if (!NativeMethods.GetWindowRect(hWnd, out var winRect))
            return false;

        var monitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref mi))
            return false;

        var m = mi.rcMonitor;
        return winRect.Left <= m.Left && winRect.Top <= m.Top &&
               winRect.Right >= m.Right && winRect.Bottom >= m.Bottom;
    }

    // Returns the invisible DWM border widths: (left, top, right, bottom).
    // GetWindowRect includes these; DWMWA_EXTENDED_FRAME_BOUNDS excludes them.
    private static (int bL, int bT, int bR, int bB) GetBorderInsets(IntPtr hWnd)
    {
        if (NativeMethods.GetWindowRect(hWnd, out var wr) &&
            NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                out NativeMethods.RECT fr, Marshal.SizeOf<NativeMethods.RECT>()) == 0)
        {
            return (fr.Left - wr.Left, fr.Top - wr.Top, wr.Right - fr.Right, wr.Bottom - fr.Bottom);
        }
        return (0, 0, 0, 0);
    }

    private static Rectangle GetWorkArea(IntPtr hWnd)
    {
        var monitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var mi = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (NativeMethods.GetMonitorInfo(monitor, ref mi))
        {
            var r = mi.rcWork;
            return new Rectangle(r.Left, r.Top, r.Width, r.Height);
        }
        return Screen.PrimaryScreen!.WorkingArea;
    }
}
