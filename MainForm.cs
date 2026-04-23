using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WMover;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _startupItem;
    private IntPtr _hIcon = IntPtr.Zero;
    private HelpForm? _helpForm;

    // Static so the GC never collects the delegate while the hook is active
    private static NativeMethods.LowLevelKeyboardProc? _hookProc;
    private static IntPtr _hookId = IntPtr.Zero;

    private const int WM_SETTINGCHANGE = 0x001A;

    public MainForm()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        Size = new Size(1, 1);
        Opacity = 0;

        _startupItem = new ToolStripMenuItem("Run at Windows startup", null, OnStartupToggle)
        {
            Checked = StartupManager.IsEnabled()
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Help", null, OnHelp);
        menu.Items.Add("Exit", null, (_, _) => Application.Exit());

        _trayIcon = new NotifyIcon
        {
            Icon = BuildIcon(),
            Text = "WMover",
            ContextMenuStrip = menu,
            Visible = true,
        };

        _hookProc = HookCallback;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(null),
            0);
    }

    private void OnStartupToggle(object? sender, EventArgs e)
    {
        StartupManager.Toggle();
        _startupItem.Checked = StartupManager.IsEnabled();
    }

    private void OnHelp(object? sender, EventArgs e)
    {
        if (_helpForm == null || _helpForm.IsDisposed)
            _helpForm = new HelpForm();
        _helpForm.BringToFront();
        _helpForm.Show();
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            ((int)wParam == NativeMethods.WM_KEYDOWN || (int)wParam == NativeMethods.WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
            bool win  = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                        (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;

            if (win)
            {
                WindowAction? action = null;

                if (ctrl)
                {
                    action = kb.vkCode switch
                    {
                        0x45 => WindowAction.TwoThirdsLeft,   // E
                        0x54 => WindowAction.TwoThirdsRight,  // T
                        0x52 => WindowAction.TwoThirdsCenter, // R
                        0x44 => WindowAction.OneThirdLeft,    // D
                        0x47 => WindowAction.OneThirdRight,   // G
                        0x46 => WindowAction.OneThirdCenter,  // F
                        0x43 => WindowAction.CenterOnly,      // C
                        _    => null,
                    };
                }
                else
                {
                    action = kb.vkCode switch
                    {
                        0x25 => WindowAction.HalfLeft,  // Left arrow
                        0x27 => WindowAction.HalfRight, // Right arrow
                        _    => null,
                    };
                }

                if (action.HasValue)
                {
                    WindowManager.Apply(action.Value);
                    return (IntPtr)1; // suppress — prevents Win system shortcuts from firing
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SETTINGCHANGE)
        {
            var param = Marshal.PtrToStringUni(m.LParam);
            if (param == "ImmersiveColorSet")
                RefreshIcon();
        }
        base.WndProc(ref m);
    }

    private void RefreshIcon()
    {
        var newHIcon = CreateHIcon();
        var oldHIcon = _hIcon;
        _trayIcon.Icon = Icon.FromHandle(newHIcon);
        _hIcon = newHIcon;
        if (oldHIcon != IntPtr.Zero)
            NativeMethods.DestroyIcon(oldHIcon);
    }

    private Icon BuildIcon()
    {
        _hIcon = CreateHIcon();
        return Icon.FromHandle(_hIcon);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            _trayIcon.Dispose();
            if (_hIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(_hIcon);
        }
        base.Dispose(disposing);
    }

    private static IntPtr CreateHIcon()
    {
        bool isDark = IsDarkMode();
        var fore = isDark ? Color.White : Color.Black;

        using var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Rounded square filled with foreground colour
            using var path = BuildRoundedRect(new Rectangle(0, 0, 15, 15), 2);
            using var brush = new SolidBrush(fore);
            g.FillPath(brush, path);

            // Cut a 2 px transparent divider down the vertical centre
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillRectangle(Brushes.Transparent, 7, 0, 2, 16);
        }
        return bmp.GetHicon();
    }

    private static GraphicsPath BuildRoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static bool IsDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return (int)(key?.GetValue("SystemUsesLightTheme") ?? 1) == 0;
    }
}
