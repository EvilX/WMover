namespace WMover;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => LogCrash(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);

        using var mutex = new Mutex(true, "WMoverWindowManager-{A3F2C1D0}", out bool created);
        if (!created) return;

        try { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); } catch { }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            MessageBox.Show(
                $"WMover failed to start:\n\n{ex.GetType().Name}: {ex.Message}",
                "WMover", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void LogCrash(Exception? ex)
    {
        try
        {
            var dir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WMover");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch { }
    }
}
