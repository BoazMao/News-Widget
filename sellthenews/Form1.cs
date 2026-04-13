using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FormsTimer = System.Windows.Forms.Timer;
using sellthenews.Models;
using sellthenews.Services;

namespace sellthenews
{
    public partial class Form1 : Form
    {
        private readonly HttpClient client = new HttpClient();
        private readonly SellTheNewsService sellTheNewsService;
        private readonly FinancialJuiceService financialJuiceService;

        // UI Controls
        private Button closeButton;
        private Panel tabButtonPanel;
        private Button overviewTab;
        private Button sellTheNewsTab;
        private Button financialJuiceTab;

        private Panel overviewPanel;
        private Panel sellTheNewsPanel;
        private Panel financialJuicePanel;

        // Content controls for each panel
        private Label overviewTitleLabel;
        private Label overviewUpdatedLabel;
        private TextBox overviewContentBox;
        private ListBox financialJuiceListBox;

        private TextBox stnFullContentBox;
        private Label stnTitleLabel;
        private Label stnUpdatedLabel;

        // State
        private FormsTimer stnRefreshTimer;
        private FormsTimer fjRefreshTimer;
        private SellTheNewsSummary currentSummary;
        private List<FinancialJuiceHeadline> currentHeadlines;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private int currentTabIndex = 0;

        public Form1()
        {
            InitializeComponent();

            sellTheNewsService = new SellTheNewsService(client);
            financialJuiceService = new FinancialJuiceService(client);
            currentSummary = new SellTheNewsSummary();
            currentHeadlines = new List<FinancialJuiceHeadline>();

            SetupWindow();
            SetupTabs();
            SetupPanels();
            SetupTimers();
            SetupStyling();

            ShowTab(0);
        }

        private void SetupWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = Color.FromArgb(24, 24, 24);
            ForeColor = Color.White;
            Size = new Size(500, 380);

            int x = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
            int y = 10;
            Location = new Point(x, y);

            KeyPreview = true;
            KeyDown += Form1_KeyDown;

            MouseDown += Drag_MouseDown;
            MouseMove += Drag_MouseMove;
            MouseUp += Drag_MouseUp;
        }

        private void SetupTabs()
        {
            tabButtonPanel = new Panel
            {
                Left = 0,
                Top = 0,
                Width = 500,
                Height = 36,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            closeButton = new Button
            {
                Text = "×",
                Left = 460,
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
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(60, 60, 60);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.FromArgb(40, 40, 40);

            overviewTab = CreateTabButton(8, 0, "Overview");
            overviewTab.Click += (s, e) => ShowTab(0);

            sellTheNewsTab = CreateTabButton(130, 0, "Sell The News");
            sellTheNewsTab.Click += (s, e) => ShowTab(1);

            financialJuiceTab = CreateTabButton(280, 0, "Financial Juice");
            financialJuiceTab.Click += (s, e) => ShowTab(2);

            tabButtonPanel.Controls.Add(overviewTab);
            tabButtonPanel.Controls.Add(sellTheNewsTab);
            tabButtonPanel.Controls.Add(financialJuiceTab);
            tabButtonPanel.Controls.Add(closeButton);

            Controls.Add(tabButtonPanel);
        }

        private Button CreateTabButton(int left, int top, string text)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 115,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                TabStop = false
            };
        }

        private void SetupPanels()
        {
            // Overview Panel
            overviewPanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 500,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            overviewTitleLabel = new Label
            {
                Left = 12,
                Top = 12,
                Width = 476,
                Height = 24,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.RoyalBlue,
                Text = "Loading..."
            };
            overviewTitleLabel.BackColor = Color.Transparent;

            overviewUpdatedLabel = new Label
            {
                Left = 12,
                Top = 38,
                Width = 476,
                Height = 16,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Text = ""
            };
            overviewUpdatedLabel.BackColor = Color.Transparent;

            overviewContentBox = new TextBox
            {
                Left = 12,
                Top = 60,
                Width = 476,
                Height = 270,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                TabStop = false
            };

            overviewPanel.Controls.Add(overviewTitleLabel);
            overviewPanel.Controls.Add(overviewUpdatedLabel);
            overviewPanel.Controls.Add(overviewContentBox);

            // Sell The News Panel
            sellTheNewsPanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 500,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            stnTitleLabel = new Label
            {
                Left = 12,
                Top = 12,
                Width = 476,
                Height = 24,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.RoyalBlue,
                Text = "Loading..."
            };
            stnTitleLabel.BackColor = Color.Transparent;

            stnUpdatedLabel = new Label
            {
                Left = 12,
                Top = 38,
                Width = 476,
                Height = 16,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Text = ""
            };
            stnUpdatedLabel.BackColor = Color.Transparent;

            stnFullContentBox = new TextBox
            {
                Left = 12,
                Top = 60,
                Width = 476,
                Height = 270,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                TabStop = false
            };

            sellTheNewsPanel.Controls.Add(stnTitleLabel);
            sellTheNewsPanel.Controls.Add(stnUpdatedLabel);
            sellTheNewsPanel.Controls.Add(stnFullContentBox);

            // Financial Juice Panel
            financialJuicePanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 500,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            financialJuiceListBox = new ListBox
            {
                Left = 12,
                Top = 12,
                Width = 476,
                Height = 318,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                TabStop = false
            };

            financialJuicePanel.Controls.Add(financialJuiceListBox);

            Controls.Add(overviewPanel);
            Controls.Add(sellTheNewsPanel);
            Controls.Add(financialJuicePanel);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // Add drag handlers to labels
            overviewTitleLabel.MouseDown += Drag_MouseDown;
            overviewTitleLabel.MouseMove += Drag_MouseMove;
            overviewTitleLabel.MouseUp += Drag_MouseUp;

            stnTitleLabel.MouseDown += Drag_MouseDown;
            stnTitleLabel.MouseMove += Drag_MouseMove;
            stnTitleLabel.MouseUp += Drag_MouseUp;
        }

        private void ShowTab(int tabIndex)
        {
            currentTabIndex = tabIndex;

            overviewPanel.Visible = (tabIndex == 0);
            sellTheNewsPanel.Visible = (tabIndex == 1);
            financialJuicePanel.Visible = (tabIndex == 2);

            // Update tab button styling
            overviewTab.BackColor = (tabIndex == 0) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
            sellTheNewsTab.BackColor = (tabIndex == 1) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
            financialJuiceTab.BackColor = (tabIndex == 2) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
        }

        private void SetupTimers()
        {
            // Sell The News refresh timer (30 seconds)
            stnRefreshTimer = new FormsTimer();
            stnRefreshTimer.Interval = 30000;
            stnRefreshTimer.Tick += async (s, e) => await RefreshSellTheNews();
            stnRefreshTimer.Start();

            // Financial Juice refresh timer (45 seconds)
            fjRefreshTimer = new FormsTimer();
            fjRefreshTimer.Interval = 45000;
            fjRefreshTimer.Tick += async (s, e) => await RefreshFinancialJuice();
            fjRefreshTimer.Start();

            // Initial load
            _ = RefreshSellTheNews();
            _ = RefreshFinancialJuice();
        }

        private async Task RefreshSellTheNews()
        {
            try
            {
                currentSummary = await sellTheNewsService.FetchLatestSummaryAsync();

                stnTitleLabel.Text = currentSummary.Title;
                stnUpdatedLabel.Text = $"Updated: {currentSummary.UpdatedAt:yyyy-MM-dd HH:mm:ss}";
                stnFullContentBox.Text = sellTheNewsService.GetFullReport(currentSummary.Markdown);

                // Update overview as well
                overviewTitleLabel.Text = currentSummary.Title;
                overviewUpdatedLabel.Text = $"Updated: {currentSummary.UpdatedAt:yyyy-MM-dd HH:mm:ss}";

                // Show STN content + FJ headlines in overview
                UpdateOverviewDisplay();
            }
            catch (Exception ex)
            {
                stnTitleLabel.Text = "Refresh failed";
                stnUpdatedLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                stnFullContentBox.Text = ex.Message;
            }
        }

        private async Task RefreshFinancialJuice()
        {
            try
            {
                currentHeadlines = await financialJuiceService.FetchLatestHeadlinesAsync();
                UpdateFinancialJuiceDisplay();

                // Also update the overview panel
                UpdateOverviewDisplay();
            }
            catch (Exception ex)
            {
                financialJuiceListBox.Items.Clear();
                financialJuiceListBox.Items.Add("Error loading Financial Juice:");
                financialJuiceListBox.Items.Add(ex.Message);
            }
        }

        private void UpdateFinancialJuiceDisplay()
        {
            financialJuiceListBox.Items.Clear();

            if (currentHeadlines.Count == 0)
            {
                financialJuiceListBox.Items.Add("No headlines available");
                return;
            }

            foreach (var headline in currentHeadlines)
            {
                string item = $"[{headline.PublishedAt:HH:mm}] {headline.Title}";
                if (!string.IsNullOrWhiteSpace(headline.Category))
                    item += $" ({headline.Category})";

                financialJuiceListBox.Items.Add(item);

                if (!string.IsNullOrWhiteSpace(headline.Summary))
                    financialJuiceListBox.Items.Add($"  → {ShortenContent(headline.Summary, 80)}");

                financialJuiceListBox.Items.Add("");
            }
        }

        private void UpdateOverviewDisplay()
        {
            var overviewText = new StringBuilder();

            // Add Sell The News section
            var stnContent = sellTheNewsService.ExtractMainSection(currentSummary.Markdown);
            overviewText.AppendLine("=== SELL THE NEWS ===");
            overviewText.AppendLine(ShortenContent(stnContent, 250));
            overviewText.AppendLine();

            // Add Financial Juice section
            overviewText.AppendLine("=== FINANCIAL JUICE ===");
            if (currentHeadlines.Count == 0)
            {
                overviewText.AppendLine("No headlines available");
            }
            else
            {
                int headlineCount = Math.Min(5, currentHeadlines.Count);
                for (int i = 0; i < headlineCount; i++)
                {
                    var headline = currentHeadlines[i];
                    overviewText.AppendLine($"• {headline.Title}");
                    if (!string.IsNullOrWhiteSpace(headline.Summary))
                        overviewText.AppendLine($"  {ShortenContent(headline.Summary, 100)}");
                    overviewText.AppendLine();
                }
            }

            overviewContentBox.Text = overviewText.ToString();
        }

        private string ShortenContent(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength).TrimEnd() + "...";
        }

        private void SetupStyling()
        {
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

            this.Opacity = 0.92;
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
