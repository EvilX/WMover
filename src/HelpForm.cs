namespace WMover;

internal sealed class HelpForm : Form
{
    public HelpForm(IReadOnlyList<HotkeyBinding> bindings)
    {
        Text = "WMover — Keyboard Shortcuts";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        var list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            MultiSelect = false,
            Dock = DockStyle.Fill,
        };

        list.Columns.Add("Shortcut", 185);
        list.Columns.Add("Action", 255);

        foreach (var b in bindings)
        {
            var item = new ListViewItem(b.ShortcutText);
            item.SubItems.Add(b.Label);
            list.Items.Add(item);
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
        };

        var ok = new Button { Text = "OK", Width = 80, Height = 26 };
        ok.Click += (_, _) => Close();
        AcceptButton = ok;

        footer.Controls.Add(ok);
        Controls.Add(list);
        Controls.Add(footer);

        ClientSize = new Size(460, bindings.Count * 19 + 66);
    }
}
