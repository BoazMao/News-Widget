using System;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FormsTimer = System.Windows.Forms.Timer;

namespace sellthenews
{
    public partial class Form1 : Form
    {
        private readonly HttpClient client = new HttpClient();

        private Label titleLabel;
        private Label updatedLabel;
        private TextBox contentBox;
        private Button closeButton;
        private FormsTimer refreshTimer;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Form1()
        {
            InitializeComponent();
            SetupWindow();
            SetupControls();
            SetupTimer();
            SetupStyling();
        }

        private void SetupWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = Color.FromArgb(24, 24, 24);
            ForeColor = Color.White;
            Size = new Size(460, 320);

            int x = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
            int y = 10;
            Location = new Point(x, y);

            KeyPreview = true;
            KeyDown += Form1_KeyDown;

            MouseDown += Drag_MouseDown;
            MouseMove += Drag_MouseMove;
            MouseUp += Drag_MouseUp;
        }

        private void SetupControls()
        {
            closeButton = new Button
            {
                Text = "×",
                Left = 420,
                Top = 6,
                Width = 30,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => Close();

            titleLabel = new Label
            {
                Left = 12,
                Top = 10,
                Width = 395,
                Height = 28,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.RoyalBlue,
                Text = "Loading..."
            };
            titleLabel.BackColor = Color.Transparent;

            updatedLabel = new Label
            {
                Left = 12,
                Top = 40,
                Width = 395,
                Height = 18,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Text = ""
            };
            updatedLabel.BackColor = Color.Transparent;

            contentBox = new TextBox
            {
                Left = 12,
                Top = 68,
                Width = 436,
                Height = 238,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                // keep a dark content background so text remains readable over desktop
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                TabStop = false
            };

            Controls.Add(closeButton);
            Controls.Add(titleLabel);
            Controls.Add(updatedLabel);
            Controls.Add(contentBox);

            // Ensure controls with transparent backgrounds are truly transparent
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            titleLabel.MouseDown += Drag_MouseDown;
            titleLabel.MouseMove += Drag_MouseMove;
            titleLabel.MouseUp += Drag_MouseUp;
            updatedLabel.MouseDown += Drag_MouseDown;
            updatedLabel.MouseMove += Drag_MouseMove;
            updatedLabel.MouseUp += Drag_MouseUp;
        }

        private void SetupTimer()
        {
            refreshTimer = new FormsTimer();
            refreshTimer.Interval = 30000;
            refreshTimer.Tick += async (s, e) => await RefreshData();
            refreshTimer.Start();

            _ = RefreshData();
        }

        private void SetupStyling()
        {
            // Rounded corners and drop shadow via region and form properties
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var gp = new GraphicsPath();
                int radius = 12;
                gp.AddArc(new Rectangle(0, 0, radius, radius), 180, 90);
                gp.AddArc(new Rectangle(Width - radius - 1, 0, radius, radius), 270, 90);
                gp.AddArc(new Rectangle(Width - radius - 1, Height - radius - 1, radius, radius), 0, 90);
                gp.AddArc(new Rectangle(0, Height - radius - 1, radius, radius), 90, 90);
                gp.CloseFigure();
                this.Region = new Region(gp);
            };

            // Slight transparency for widget effect
            this.Opacity = 0.92;
        }

        private async Task RefreshData()
        {
            try
            {
                string url = "https://sellthenews.org/api/wsb/latest?lang=zh";
                string json = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                JsonElement payload = root;
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    payload = dataElement;
                }

                string title = payload.TryGetProperty("title", out JsonElement t)
                    ? t.GetString() ?? "No title"
                    : "No title";

                string updatedAt = payload.TryGetProperty("updatedAt", out JsonElement u)
                    ? u.GetString() ?? ""
                    : "";

                string markdown = payload.TryGetProperty("markdown", out JsonElement m)
                    ? m.GetString() ?? ""
                    : "";

                titleLabel.Text = title;
                updatedLabel.Text = $"Updated: {updatedAt}";
                contentBox.Text = ExtractSection(markdown, "## 1.", "## 2.");
            }
            catch (Exception ex)
            {
                titleLabel.Text = "Update failed";
                updatedLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                contentBox.Text = ex.ToString();
            }
        }

        private string ShortenMarkdown(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            string text = input
                .Replace("#", "")
                .Replace("*", "")
                .Replace("|", " ")
                .Replace("---", "")
                .Trim();

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        private string ExtractSection(string markdown, string startMarker, string endMarker)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "";

            int start = markdown.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return ShortenMarkdown(markdown, 1200);

            int end = markdown.IndexOf(endMarker, start + startMarker.Length, StringComparison.OrdinalIgnoreCase);

            string section = end > start
                ? markdown.Substring(start, end - start)
                : markdown.Substring(start);

            return ShortenMarkdown(section, 1200);
        }

        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = Location;
            }
        }

        private void Drag_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Drag_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}