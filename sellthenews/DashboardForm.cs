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
    public partial class DashboardForm : Form
    {
        private readonly HttpClient client = new HttpClient();
        private readonly SellTheNewsService sellTheNewsService;
        private readonly FinancialJuiceService financialJuiceService;
        private readonly SellTheNewsLiveService sellTheNewsLiveService;

        // UI Controls
        private Button closeButton;
        private Button refreshButton;
        private Panel tabButtonPanel;
        private Button sellTheNewsTab;
        private Button financialJuiceTab;
        private Button liveNewsTab;

        private Panel sellTheNewsPanel;
        private Panel financialJuicePanel;
        private Panel liveNewsPanel;

        // Content controls for each panel
        private ListBox financialJuiceListBox;

        private RichTextBox stnFullContentBox;
        private Label stnTitleLabel;
        private Label stnUpdatedLabel;

        private RichTextBox liveNewsContentBox;
        private Label liveNewsTitleLabel;
        private Label liveNewsStatusLabel;

        // State
        private FormsTimer stnRefreshTimer;
        private FormsTimer fjRefreshTimer;
        private FormsTimer liveRefreshTimer;
        private SellTheNewsSummary currentSummary;
        private List<FinancialJuiceHeadline> currentHeadlines;
        private SellTheNewsLiveResponse currentLiveNews;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        private int currentTabIndex = 0;

        public DashboardForm()
        {
            InitializeComponent();

            sellTheNewsService = new SellTheNewsService(client);
            financialJuiceService = new FinancialJuiceService(client);
            sellTheNewsLiveService = new SellTheNewsLiveService(client);
            currentSummary = new SellTheNewsSummary();
            currentHeadlines = new List<FinancialJuiceHeadline>();
            currentLiveNews = new SellTheNewsLiveResponse();

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
            Size = new Size(600, 380); // Increased width to accommodate 4th tab

            // Enable DPI awareness for multi-monitor support
            AutoScaleMode = AutoScaleMode.None;
            AutoScale = false;

            int x = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
            int y = 10;
            Location = new Point(x, y);

            KeyPreview = true;
            KeyDown += DashboardForm_KeyDown;

            MouseDown += Drag_MouseDown;
            MouseMove += Drag_MouseMove;
            MouseUp += Drag_MouseUp;

            // Validate position on each move to keep window on screen
            LocationChanged += ValidateWindowPosition;
        }

        private void SetupTabs()
        {
            tabButtonPanel = new Panel
            {
                Left = 0,
                Top = 0,
                Width = 600, // Updated to match new window width
                Height = 36,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            // Enable dragging on the tab panel
            tabButtonPanel.MouseDown += Drag_MouseDown;
            tabButtonPanel.MouseMove += Drag_MouseMove;
            tabButtonPanel.MouseUp += Drag_MouseUp;

            closeButton = new Button
            {
                Text = "×",
                Left = 565,
                Top = 0,
                Width = 30,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => Close();
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(60, 60, 60);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.FromArgb(40, 40, 40);

            // Add refresh button
            refreshButton = new Button
            {
                Text = "↻",
                Left = 530,
                Top = 0,
                Width = 30,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                TabStop = false
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += async (s, e) =>
            {
                refreshButton.Enabled = false;
                _ = RefreshSellTheNews();
                _ = RefreshFinancialJuice();
                _ = RefreshLiveNews();
                await Task.Delay(500);
                refreshButton.Enabled = true;
            };
            refreshButton.MouseEnter += (s, e) => refreshButton.BackColor = Color.FromArgb(60, 60, 60);
            refreshButton.MouseLeave += (s, e) => refreshButton.BackColor = Color.FromArgb(40, 40, 40);

            sellTheNewsTab = CreateTabButton(8, 0, "STN", 95);
            sellTheNewsTab.Click += (s, e) => ShowTab(0);

            financialJuiceTab = CreateTabButton(107, 0, "FJ", 95);
            financialJuiceTab.Click += (s, e) => ShowTab(1);

            liveNewsTab = CreateTabButton(206, 0, "Live News", 95);
            liveNewsTab.Click += (s, e) => ShowTab(2);

            tabButtonPanel.Controls.Add(sellTheNewsTab);
            tabButtonPanel.Controls.Add(financialJuiceTab);
            tabButtonPanel.Controls.Add(liveNewsTab);
            tabButtonPanel.Controls.Add(refreshButton);
            tabButtonPanel.Controls.Add(closeButton);

            Controls.Add(tabButtonPanel);
        }

        private Button CreateTabButton(int left, int top, string text, int width = 115)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
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
            // Sell The News Panel
            sellTheNewsPanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 600,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            stnTitleLabel = new Label
            {
                Left = 12,
                Top = 12,
                Width = 576,
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
                Width = 576,
                Height = 16,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Text = ""
            };
            stnUpdatedLabel.BackColor = Color.Transparent;

            stnFullContentBox = new RichTextBox
            {
                Left = 12,
                Top = 60,
                Width = 576,
                Height = 270,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                TabStop = false
            };

            // Disable text selection and enable dragging
            stnFullContentBox.MouseDown += Drag_MouseDown;
            stnFullContentBox.MouseMove += Drag_MouseMove;
            stnFullContentBox.MouseUp += Drag_MouseUp;

            sellTheNewsPanel.Controls.Add(stnTitleLabel);
            sellTheNewsPanel.Controls.Add(stnUpdatedLabel);
            sellTheNewsPanel.Controls.Add(stnFullContentBox);

            // Financial Juice Panel
            financialJuicePanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 600,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            financialJuiceListBox = new ListBox
            {
                Left = 12,
                Top = 12,
                Width = 576,
                Height = 318,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                TabStop = false
            };

            // Enable dragging on listbox
            financialJuiceListBox.MouseDown += Drag_MouseDown;
            financialJuiceListBox.MouseMove += Drag_MouseMove;
            financialJuiceListBox.MouseUp += Drag_MouseUp;

            financialJuicePanel.Controls.Add(financialJuiceListBox);

            // Live News Panel
            liveNewsPanel = new Panel
            {
                Left = 0,
                Top = 36,
                Width = 600,
                Height = 344,
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            liveNewsTitleLabel = new Label
            {
                Left = 12,
                Top = 12,
                Width = 576,
                Height = 24,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.RoyalBlue,
                Text = "Live News"
            };
            liveNewsTitleLabel.BackColor = Color.Transparent;

            liveNewsStatusLabel = new Label
            {
                Left = 12,
                Top = 38,
                Width = 576,
                Height = 16,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.LightGray,
                Text = "Loading..."
            };
            liveNewsStatusLabel.BackColor = Color.Transparent;

            liveNewsContentBox = new RichTextBox
            {
                Left = 12,
                Top = 60,
                Width = 576,
                Height = 270,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                TabStop = false
            };

            // Enable dragging through live news content box
            liveNewsContentBox.MouseDown += Drag_MouseDown;
            liveNewsContentBox.MouseMove += Drag_MouseMove;
            liveNewsContentBox.MouseUp += Drag_MouseUp;

            liveNewsPanel.Controls.Add(liveNewsTitleLabel);
            liveNewsPanel.Controls.Add(liveNewsStatusLabel);
            liveNewsPanel.Controls.Add(liveNewsContentBox);

            Controls.Add(sellTheNewsPanel);
            Controls.Add(financialJuicePanel);
            Controls.Add(liveNewsPanel);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // Add drag handlers to all panels for full-window dragging
            sellTheNewsPanel.MouseDown += Drag_MouseDown;
            sellTheNewsPanel.MouseMove += Drag_MouseMove;
            sellTheNewsPanel.MouseUp += Drag_MouseUp;

            financialJuicePanel.MouseDown += Drag_MouseDown;
            financialJuicePanel.MouseMove += Drag_MouseMove;
            financialJuicePanel.MouseUp += Drag_MouseUp;

            // Add drag handlers to labels
            stnTitleLabel.MouseDown += Drag_MouseDown;
            stnTitleLabel.MouseMove += Drag_MouseMove;
            stnTitleLabel.MouseUp += Drag_MouseUp;
        }

        private void ShowTab(int tabIndex)
        {
            currentTabIndex = tabIndex;

            sellTheNewsPanel.Visible = (tabIndex == 0);
            financialJuicePanel.Visible = (tabIndex == 1);
            liveNewsPanel.Visible = (tabIndex == 2);

            // Update tab button styling
            sellTheNewsTab.BackColor = (tabIndex == 0) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
            financialJuiceTab.BackColor = (tabIndex == 1) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
            liveNewsTab.BackColor = (tabIndex == 2) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35);
        }

        private void SetupTimers()
        {
            // Sell The News refresh timer (1 hour - WSB reports don't update frequently)
            stnRefreshTimer = new FormsTimer();
            stnRefreshTimer.Interval = 3600000; // 1 hour in milliseconds
            stnRefreshTimer.Tick += async (s, e) => await RefreshSellTheNews();
            stnRefreshTimer.Start();

            // Financial Juice refresh timer (45 seconds - news updates more frequently)
            fjRefreshTimer = new FormsTimer();
            fjRefreshTimer.Interval = 45000;
            fjRefreshTimer.Tick += async (s, e) => await RefreshFinancialJuice();
            fjRefreshTimer.Start();

            // Live News refresh timer
            liveRefreshTimer = new FormsTimer();
            liveRefreshTimer.Interval = 45000; // 5 seconds
            liveRefreshTimer.Tick += async (s, e) => await RefreshLiveNews();
            liveRefreshTimer.Start();

            // Initial load
            _ = RefreshSellTheNews();
            _ = RefreshFinancialJuice();
            _ = RefreshLiveNews();
        }

        private async Task RefreshSellTheNews()
        {
            try
            {
                currentSummary = await sellTheNewsService.FetchLatestSummaryAsync();

                stnTitleLabel.Text = "每日分析报告";
                stnUpdatedLabel.Text = $"Updated: {currentSummary.UpdatedAt:yyyy-MM-dd HH:mm:ss}";

                string formattedText = sellTheNewsService.GetFullReport(currentSummary.Markdown);
                SetRichTextBoxWithFormatting(stnFullContentBox, formattedText);
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
            }
            catch (Exception ex)
            {
                financialJuiceListBox.Items.Clear();
                financialJuiceListBox.Items.Add("Error loading Financial Juice:");
                financialJuiceListBox.Items.Add(ex.Message);
            }
        }

        private async Task RefreshLiveNews()
        {
            try
            {
                currentLiveNews = await sellTheNewsLiveService.FetchLiveNewsAsync();
                UpdateLiveNewsDisplay();
                liveNewsStatusLabel.Text = $"Updated: {currentLiveNews.FetchedAt:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                liveNewsStatusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateLiveNewsDisplay()
        {
            string content = FormatLiveNewsContent();
            liveNewsContentBox.Clear();
            liveNewsContentBox.Text = content;
        }

        private string FormatLiveNewsContent()
        {
            var sb = new StringBuilder();

            if (currentLiveNews?.Data == null || currentLiveNews.Data.Count == 0)
            {
                return "No live news available";
            }

            foreach (var item in currentLiveNews.Data)
            {
                sb.AppendLine($"[{item.Source}]  {item.Time:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(item.Title);

                if (!string.IsNullOrWhiteSpace(item.Body))
                {
                    sb.AppendLine();
                    sb.AppendLine(item.Body);
                }

                sb.AppendLine();
                sb.AppendLine(new string('═', 50));
                sb.AppendLine();
            }

            return sb.ToString();
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

        private string ShortenContent(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength).TrimEnd() + "...";
        }

        private void SetRichTextBoxWithFormatting(RichTextBox rtb, string text, bool scrollToTop = true)
        {
            rtb.Clear();
            if (string.IsNullOrWhiteSpace(text))
            {
                System.Diagnostics.Debug.WriteLine($"SetRichTextBoxWithFormatting: Text is null/whitespace!");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"SetRichTextBoxWithFormatting: Text length={text.Length}, first 100 chars: {text.Substring(0, Math.Min(100, text.Length))}");

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // Headers (▼ ...) - make bold and larger
                if (trimmed.StartsWith("▼"))
                {
                    rtb.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
                    rtb.SelectionColor = Color.RoyalBlue;
                    rtb.AppendText(trimmed + "\n");
                    rtb.SelectionFont = new Font("Segoe UI", 9);
                    rtb.SelectionColor = Color.White;
                }
                // Separator lines (═) - lighter gray
                else if (trimmed.StartsWith("═"))
                {
                    rtb.SelectionColor = Color.Gray;
                    rtb.AppendText(trimmed + "\n");
                    rtb.SelectionColor = Color.White;
                }
                // Section headers (===...===) - bold white
                else if (trimmed.StartsWith("==="))
                {
                    rtb.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
                    rtb.AppendText(trimmed + "\n");
                    rtb.SelectionFont = new Font("Segoe UI", 9);
                }
                // Regular text with markdown-like formatting
                else if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    ProcessLineWithFormatting(rtb, trimmed);
                    rtb.AppendText("\n");
                }
                else
                {
                    // Empty line
                    rtb.AppendText("\n");
                }
            }

            // Handle scroll behavior
            if (scrollToTop)
            {
                // Scroll to top for most tabs
                rtb.Select(0, 0);
                rtb.ScrollToCaret();
            }
            else
            {
                // For live news: don't scroll, let user stay where they are
                // Content updates in the background
            }
        }

        private void ProcessLineWithFormatting(RichTextBox rtb, string line)
        {
            int pos = 0;

            while (pos < line.Length)
            {
                // Look for **bold** pattern
                int boldStart = line.IndexOf("**", pos);
                if (boldStart >= 0)
                {
                    // Add text before bold
                    if (boldStart > pos)
                    {
                        rtb.SelectionFont = new Font("Segoe UI", 9);
                        rtb.AppendText(line.Substring(pos, boldStart - pos));
                    }

                    // Find end of bold
                    int boldEnd = line.IndexOf("**", boldStart + 2);
                    if (boldEnd >= 0)
                    {
                        // Add bold text
                        rtb.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
                        rtb.AppendText(line.Substring(boldStart + 2, boldEnd - boldStart - 2));
                        pos = boldEnd + 2;
                    }
                    else
                    {
                        // Unclosed bold, add as-is
                        rtb.SelectionFont = new Font("Segoe UI", 9);
                        rtb.AppendText(line.Substring(boldStart));
                        pos = line.Length;
                    }
                }
                else
                {
                    // No more bold, add remaining text
                    rtb.SelectionFont = new Font("Segoe UI", 9);
                    rtb.AppendText(line.Substring(pos));
                    pos = line.Length;
                }
            }

            rtb.SelectionFont = new Font("Segoe UI", 9);
            rtb.SelectionColor = Color.White;
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
                // Don't start drag if clicking in scrollable area's scrollbar
                if (sender is TextBox textBox && e.X > textBox.Width - SystemInformation.VerticalScrollBarWidth)
                    return;

                dragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = Location;
            }
        }

        private void Drag_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point currentCursorPos = Cursor.Position;

                // Calculate the delta with proper multi-monitor support
                int deltaX = currentCursorPos.X - dragCursorPoint.X;
                int deltaY = currentCursorPos.Y - dragCursorPoint.Y;

                // Calculate new position
                int newX = dragFormPoint.X + deltaX;
                int newY = dragFormPoint.Y + deltaY;

                // Validate position across multiple monitors
                Point newLocation = ValidatePositionForMultiMonitor(new Point(newX, newY));
                Location = newLocation;
            }
        }

        private Point ValidatePositionForMultiMonitor(Point proposedLocation)
        {
            // Get all available screens
            Screen[] screens = Screen.AllScreens;

            // Find the monitor that will contain the center of the window
            Screen targetScreen = Screen.FromPoint(new Point(
                proposedLocation.X + Width / 2,
                proposedLocation.Y + Height / 2
            ));

            // Ensure window doesn't go too far off-screen
            int minX = targetScreen.WorkingArea.Left - Width + 50;
            int maxX = targetScreen.WorkingArea.Right - 50;
            int minY = targetScreen.WorkingArea.Top - Height + 20;
            int maxY = targetScreen.WorkingArea.Bottom - 20;

            return new Point(
                Math.Max(minX, Math.Min(maxX, proposedLocation.X)),
                Math.Max(minY, Math.Min(maxY, proposedLocation.Y))
            );
        }

        private void ValidateWindowPosition(object sender, EventArgs e)
        {
            // Ensure window stays on a valid screen when Location changes
            Point validPosition = ValidatePositionForMultiMonitor(Location);
            if (validPosition != Location)
            {
                Location = validPosition;
            }
        }

        private void Drag_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void DashboardForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }
}
