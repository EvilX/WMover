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
    public static void Apply(WindowAction action)
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero || IsFullscreen(hWnd))
            return;

        if (NativeMethods.IsZoomed(hWnd))
            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

        var work = GetWorkArea(hWnd);
        int w = work.Width, h = work.Height, x = work.Left, y = work.Top;
        int newX, newY, newW, newH;

        switch (action)
        {
            case WindowAction.TwoThirdsLeft:
                (newW, newH, newX, newY) = (w * 2 / 3, h, x, y);
                break;
            case WindowAction.TwoThirdsRight:
                newW = w * 2 / 3;
                (newH, newX, newY) = (h, x + w - newW, y);
                break;
            case WindowAction.TwoThirdsCenter:
                newW = w * 2 / 3;
                (newH, newX, newY) = (h, x + (w - newW) / 2, y);
                break;
            case WindowAction.OneThirdLeft:
                (newW, newH, newX, newY) = (w / 3, h, x, y);
                break;
            case WindowAction.OneThirdRight:
                newW = w / 3;
                (newH, newX, newY) = (h, x + w - newW, y);
                break;
            case WindowAction.OneThirdCenter:
                newW = w / 3;
                (newH, newX, newY) = (h, x + (w - newW) / 2, y);
                break;
            case WindowAction.HalfLeft:
                (newW, newH, newX, newY) = (w / 2, h, x, y);
                break;
            case WindowAction.HalfRight:
                newW = w / 2;
                (newH, newX, newY) = (h, x + w - newW, y);
                break;
            case WindowAction.CenterOnly:
                NativeMethods.GetWindowRect(hWnd, out var rect);
                (newW, newH) = (Math.Min(rect.Width, w), Math.Min(rect.Height, h));
                (newX, newY) = (x + (w - newW) / 2, y + (h - newH) / 2);
                break;
            default:
                return;
        }

        NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, newX, newY, newW, newH,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
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
