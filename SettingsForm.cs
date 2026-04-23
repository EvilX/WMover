namespace WMover;

internal sealed class SettingsForm : Form
{
    private readonly List<HotkeyBinding> _bindings;
    private readonly ListView _list;
    private readonly Label _statusLabel;
    private int _capturingIndex = -1;

    internal static Action<bool, bool, bool, bool, uint>? KeyCaptureCallback; // (ctrl, shift, alt, win, vk)
    internal static Action? EscCaptureCallback;

    internal event Action<IReadOnlyList<HotkeyBinding>>? BindingsSaved;

    public SettingsForm(IReadOnlyList<HotkeyBinding> current)
    {
        _bindings = current.Select(b => new HotkeyBinding
        {
            Action = b.Action, Label = b.Label, Ctrl = b.Ctrl, Win = b.Win, VkCode = b.VkCode,
        }).ToList();

        Text = "WMover — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        _list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            MultiSelect = false,
            Dock = DockStyle.Fill,
        };
        _list.Columns.Add("Action", 255);
        _list.Columns.Add("Shortcut", 185);
        PopulateList();
        _list.MouseClick += OnListClick;

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Text = "Click a row to reassign its shortcut.",
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
        };

        var cancel = new Button { Text = "Cancel", Width = 80, Height = 26 };
        cancel.Click += (_, _) => { CancelCapture(); Close(); };
        CancelButton = cancel;

        var ok = new Button { Text = "OK", Width = 80, Height = 26 };
        ok.Click += OnOK;
        AcceptButton = ok;

        var reset = new Button { Text = "Reset defaults", Width = 110, Height = 26 };
        reset.Click += OnReset;

        footer.Controls.Add(cancel);
        footer.Controls.Add(ok);
        footer.Controls.Add(reset);

        Controls.Add(_list);
        Controls.Add(_statusLabel);
        Controls.Add(footer);

        ClientSize = new Size(460, _bindings.Count * 20 + 110);
        FormClosing += (_, _) => CancelCapture();
    }

    private void PopulateList()
    {
        _list.Items.Clear();
        foreach (var b in _bindings)
        {
            var item = new ListViewItem(b.Label);
            item.SubItems.Add(b.ShortcutText);
            _list.Items.Add(item);
        }
    }

    private void OnListClick(object? sender, MouseEventArgs e)
    {
        var hit = _list.HitTest(e.Location);
        if (hit.Item == null) return;
        CancelCapture();
        StartCapture(hit.Item.Index);
    }

    private void StartCapture(int index)
    {
        _capturingIndex = index;
        _list.Items[index].BackColor = SystemColors.Highlight;
        _list.Items[index].ForeColor = SystemColors.HighlightText;
        _list.Items[index].SubItems[1].Text = "...";
        _statusLabel.Text = $"Press new shortcut for \"{_bindings[index].Label}\"… (Esc to cancel)";

        EscCaptureCallback = CancelCapture;
        KeyCaptureCallback = (ctrl, shift, alt, win, vk) =>
        {
            if (!ctrl && !shift && !alt && !win) return;
            _bindings[index].Ctrl   = ctrl;
            _bindings[index].Shift  = shift;
            _bindings[index].Alt    = alt;
            _bindings[index].Win    = win;
            _bindings[index].VkCode = vk;
            BeginInvoke(FinishCapture);
        };
    }

    private void FinishCapture()
    {
        if (_capturingIndex < 0) return;
        int idx = _capturingIndex;
        KeyCaptureCallback  = null;
        EscCaptureCallback  = null;
        _capturingIndex = -1;

        _list.Items[idx].BackColor = _list.BackColor;
        _list.Items[idx].ForeColor = _list.ForeColor;
        _list.Items[idx].SubItems[1].Text = _bindings[idx].ShortcutText;
        _statusLabel.Text = "Click a row to reassign its shortcut.";
    }

    private void CancelCapture()
    {
        if (_capturingIndex < 0) return;
        int idx = _capturingIndex;
        KeyCaptureCallback  = null;
        EscCaptureCallback  = null;
        _capturingIndex = -1;

        _list.Items[idx].BackColor = _list.BackColor;
        _list.Items[idx].ForeColor = _list.ForeColor;
        _list.Items[idx].SubItems[1].Text = _bindings[idx].ShortcutText;
        _statusLabel.Text = "Click a row to reassign its shortcut.";
    }

    private void OnReset(object? sender, EventArgs e)
    {
        CancelCapture();
        var defaults = HotkeyBinding.Defaults();
        for (int i = 0; i < _bindings.Count; i++)
        {
            _bindings[i].Ctrl   = defaults[i].Ctrl;
            _bindings[i].Win    = defaults[i].Win;
            _bindings[i].VkCode = defaults[i].VkCode;
        }
        PopulateList();
    }

    private void OnOK(object? sender, EventArgs e)
    {
        CancelCapture();
        BindingsSaved?.Invoke(_bindings.AsReadOnly());
        Close();
    }
}
