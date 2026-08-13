using sellthenews.Services;

namespace sellthenews;

internal sealed class ApiKeyDialog : Form
{
    private readonly TextBox keyBox = new();

    public ApiKeyDialog(string? currentKey)
    {
        Text = "NewsAPI settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 220);
        BackColor = Color.FromArgb(17, 24, 39);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var title = new Label
        {
            Text = "Connect NewsAPI",
            Font = new Font("Segoe UI Semibold", 17F),
            AutoSize = true,
            Location = new Point(24, 22)
        };
        var hint = new Label
        {
            Text = "Paste your personal key. It is encrypted for this Windows user and never added to the repository.",
            ForeColor = Color.FromArgb(156, 163, 175),
            AutoSize = false,
            Size = new Size(450, 42),
            Location = new Point(26, 62)
        };
        keyBox.Location = new Point(28, 108);
        keyBox.Size = new Size(440, 30);
        keyBox.UseSystemPasswordChar = true;
        keyBox.Text = currentKey ?? string.Empty;

        var cancel = Button("Cancel", Color.FromArgb(55, 65, 81));
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(268, 160);

        var save = Button("Save key", Color.FromArgb(37, 99, 235));
        save.DialogResult = DialogResult.OK;
        save.Location = new Point(370, 160);

        Controls.AddRange([title, hint, keyBox, cancel, save]);
        AcceptButton = save;
        CancelButton = cancel;
    }

    public string ApiKey => keyBox.Text.Trim();

    private static Button Button(string text, Color color) => new()
    {
        Text = text,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        Size = new Size(94, 36),
        Cursor = Cursors.Hand
    };
}
