namespace WMover;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "WMoverWindowManager-{A3F2C1D0}", out bool created);
        if (!created) return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
