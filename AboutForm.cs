namespace WMover;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About WMover";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 160);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20, 20, 20, 0),
        };

        panel.Controls.Add(new Label
        {
            Text = "WMover",
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            AutoSize = true,
        });
        panel.Controls.Add(new Label
        {
            Text = "Version 1.1.1",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0),
        });
        panel.Controls.Add(new Label
        {
            Text = "Window mover and resizer for Windows 11",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
        });
        panel.Controls.Add(new Label
        {
            Text = "© 2026 Leonid Nasedkin",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0),
        });

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

        Controls.Add(panel);
        Controls.Add(footer);
    }
}
