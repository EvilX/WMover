using Microsoft.Win32;
using Windows.ApplicationModel;

namespace WMover;

internal static class StartupManager
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WMover";
    private const string TaskId = "WMoverStartupTask";
    private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

    private static bool IsPackaged()
    {
        uint len = 0;
        return NativeMethods.GetCurrentPackageFullName(ref len, null) != APPMODEL_ERROR_NO_PACKAGE;
    }

    public static bool IsEnabled()
    {
        try
        {
            if (IsPackaged())
            {
                var task = Task.Run(async () => await StartupTask.GetAsync(TaskId)).GetAwaiter().GetResult();
                return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    public static void Toggle()
    {
        try
        {
            if (IsPackaged())
            {
                var task = Task.Run(async () => await StartupTask.GetAsync(TaskId)).GetAwaiter().GetResult();
                if (task.State == StartupTaskState.Enabled)
                    task.Disable();
                else
                    Task.Run(async () => await task.RequestEnableAsync()).GetAwaiter().GetResult();
                return;
            }
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null) return;
            if (IsEnabled())
                key.DeleteValue(AppName, throwOnMissingValue: false);
            else
                key.SetValue(AppName, Application.ExecutablePath);
        }
        catch { }
    }
}
