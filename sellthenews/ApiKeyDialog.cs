namespace sellthenews;

internal sealed class ApiKeyDialog : Form
{
    private readonly TextBox keyBox = new();
    private readonly ComboBox languageBox = new();

    public ApiKeyDialog(string? currentKey, string currentWsbLanguage)
    {
        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 320);
        BackColor = Color.FromArgb(17, 24, 39);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var title = new Label
        {
            Text = "Application settings",
            Font = new Font("Segoe UI Semibold", 17F),
            AutoSize = true,
            Location = new Point(24, 20)
        };
        var keyLabel = new Label
        {
            Text = "NewsAPI key (optional)",
            AutoSize = true,
            Location = new Point(28, 67)
        };
        var hint = new Label
        {
            Text = "Anonymous requests are attempted when this is blank. Saved keys are encrypted for this Windows user.",
            ForeColor = Color.FromArgb(156, 163, 175),
            AutoSize = false,
            Size = new Size(440, 42),
            Location = new Point(28, 92)
        };
        keyBox.Location = new Point(28, 137);
        keyBox.Size = new Size(440, 30);
        keyBox.UseSystemPasswordChar = true;
        keyBox.Text = currentKey ?? string.Empty;

        var languageLabel = new Label
        {
            Text = "WSB report language",
            AutoSize = true,
            Location = new Point(28, 188)
        };
        languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageBox.Items.AddRange(["English", "中文"]);
        languageBox.Location = new Point(28, 216);
        languageBox.Size = new Size(210, 32);
        languageBox.SelectedIndex = currentWsbLanguage == "zh" ? 1 : 0;

        var cancel = Button("Cancel", Color.FromArgb(55, 65, 81));
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(268, 267);

        var save = Button("Save", Color.FromArgb(37, 99, 235));
        save.DialogResult = DialogResult.OK;
        save.Location = new Point(370, 267);

        Controls.AddRange([title, keyLabel, hint, keyBox, languageLabel, languageBox, cancel, save]);
        AcceptButton = save;
        CancelButton = cancel;
    }

    public string ApiKey => keyBox.Text.Trim();
    public string WsbLanguage => languageBox.SelectedIndex == 1 ? "zh" : "en";

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
