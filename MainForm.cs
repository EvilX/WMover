using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WMover;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _startupItem;
    private IntPtr _hIcon = IntPtr.Zero;
    private SettingsForm? _settingsForm;
    private AboutForm?    _aboutForm;

    private static List<HotkeyBinding> _bindings = HotkeyBinding.Defaults();

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

        _bindings = AppSettings.Load();

        _startupItem = new ToolStripMenuItem("Run at Windows startup", null, OnStartupToggle)
        {
            Checked = StartupManager.IsEnabled()
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings", null, OnSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About",    null, OnAbout);
        menu.Items.Add("Exit",     null, (_, _) => Application.Exit());

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

    private void OnSettings(object? sender, EventArgs e)
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_bindings);
            _settingsForm.BindingsSaved += saved =>
            {
                _bindings = new List<HotkeyBinding>(saved);
                AppSettings.Save(_bindings);
            };
        }
        _settingsForm.BringToFront();
        _settingsForm.Show();
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        if (_aboutForm == null || _aboutForm.IsDisposed)
            _aboutForm = new AboutForm();
        _aboutForm.BringToFront();
        _aboutForm.Show();
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            ((int)wParam == NativeMethods.WM_KEYDOWN || (int)wParam == NativeMethods.WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Settings key-capture mode — Esc cancels, any other key is recorded
            if (SettingsForm.EscCaptureCallback != null && kb.vkCode == 0x1B)
            {
                SettingsForm.EscCaptureCallback();
                return (IntPtr)1;
            }
            if (SettingsForm.KeyCaptureCallback != null)
            {
                uint vk = kb.vkCode;
                bool isMod = vk is 0x5B or 0x5C          // VK_LWIN / VK_RWIN
                                or 0x11 or 0xA2 or 0xA3  // VK_CONTROL / VK_LCONTROL / VK_RCONTROL
                                or 0x10 or 0xA0 or 0xA1  // VK_SHIFT / VK_LSHIFT / VK_RSHIFT
                                or 0x12 or 0xA4 or 0xA5; // VK_MENU / VK_LMENU / VK_RMENU
                if (!isMod)
                {
                    bool c = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                    bool sh= (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT)   & 0x8000) != 0;
                    bool a = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU)    & 0x8000) != 0;
                    bool w = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN)    & 0x8000) != 0 ||
                             (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN)    & 0x8000) != 0;
                    SettingsForm.KeyCaptureCallback(c, sh, a, w, vk);
                    return (IntPtr)1;
                }
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Normal hotkey processing
            bool ctrl  = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
            bool shift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT)   & 0x8000) != 0;
            bool alt   = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU)    & 0x8000) != 0;
            bool win   = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN)    & 0x8000) != 0 ||
                         (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN)    & 0x8000) != 0;

            if (ctrl || shift || alt || win)
            {
                var binding = _bindings.Find(b => b.Ctrl == ctrl && b.Shift == shift && b.Alt == alt && b.Win == win && b.VkCode == kb.vkCode);
                if (binding != null)
                {
                    WindowManager.Apply(binding.Action);
                    return (IntPtr)1;
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

            using var path = BuildRoundedRect(new Rectangle(0, 0, 15, 15), 2);
            using var brush = new SolidBrush(fore);
            g.FillPath(brush, path);

            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillRectangle(Brushes.Transparent, 10, 0, 1, 16);
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
