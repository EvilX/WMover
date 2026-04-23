namespace WMover;

internal sealed class HelpForm : Form
{
    public HelpForm()
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

        (string key, string desc)[] shortcuts =
        [
            ("Win + ←",          "1/2 width — snap left"),
            ("Win + →",          "1/2 width — snap right"),
            ("Ctrl + Win + E",  "2/3 width — snap left"),
            ("Ctrl + Win + T",  "2/3 width — snap right"),
            ("Ctrl + Win + R",  "2/3 width — center"),
            ("Ctrl + Win + D",  "1/3 width — snap left"),
            ("Ctrl + Win + G",  "1/3 width — snap right"),
            ("Ctrl + Win + F",  "1/3 width — center"),
            ("Ctrl + Win + C",  "Center (keep size)"),
        ];

        foreach (var (key, desc) in shortcuts)
        {
            var item = new ListViewItem(key);
            item.SubItems.Add(desc);
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

        ClientSize = new Size(460, shortcuts.Length * 19 + 66);
    }
}
