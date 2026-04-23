using Microsoft.Win32;

namespace WMover;

internal static class StartupManager
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WMover";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }

    public static void Toggle()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;

        if (IsEnabled())
            key.DeleteValue(AppName, throwOnMissingValue: false);
        else
            key.SetValue(AppName, Application.ExecutablePath);
    }
}
