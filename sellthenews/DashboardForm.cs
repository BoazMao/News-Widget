using System.Diagnostics;
using sellthenews.Models;
using sellthenews.Services;

namespace sellthenews;

public partial class DashboardForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(10, 15, 25);
    private static readonly Color Surface = Color.FromArgb(17, 24, 39);
    private static readonly Color SurfaceRaised = Color.FromArgb(26, 36, 52);
    private static readonly Color Muted = Color.FromArgb(148, 163, 184);
    private static readonly Color Accent = Color.FromArgb(59, 130, 246);
    private static readonly Color Divider = Color.FromArgb(43, 55, 72);

    private readonly HttpClient httpClient = new();
    private readonly NewsApiService newsService;
    private readonly SellTheNewsService wsbService;
    private readonly ApiKeyStore keyStore = new();
    private readonly CancellationTokenSource lifetime = new();

    private readonly Panel workspace = new();
    private readonly Panel newsView = new();
    private readonly Panel wsbView = new();
    private readonly FlowLayoutPanel newsCards = new();
    private readonly RichTextBox articleDetail = new();
    private readonly WebBrowser wsbReport = new();
    private readonly Label pageTitle = new();
    private readonly Label status = new();
    private readonly Label newsEmptyState = new();
    private readonly Button newsTab;
    private readonly Button wsbTab;
    private readonly Dictionary<NewsCategory, Button> categoryButtons = new();
    private readonly System.Windows.Forms.Timer newsTimer = new() { Interval = 10 * 60 * 1000 };
    private readonly System.Windows.Forms.Timer wsbTimer = new() { Interval = 60 * 60 * 1000 };

    private string? apiKey;
    private string wsbLanguage;
    private NewsCategory selectedCategory = NewsCategory.General;
    private bool newsRefreshInProgress;
    private bool wsbRefreshInProgress;

    public DashboardForm()
    {
        InitializeComponent();
        newsService = new NewsApiService(httpClient);
        wsbService = new SellTheNewsService(httpClient);
        apiKey = keyStore.Load();
        wsbLanguage = keyStore.LoadWsbLanguage();

        Text = "News Widget";
        MinimumSize = new Size(900, 600);
        Size = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Canvas;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        DoubleBuffered = true;

        var sidebar = BuildSidebar();
        var header = BuildHeader();
        workspace.Dock = DockStyle.Fill;
        workspace.Padding = new Padding(24, 16, 24, 24);
        workspace.BackColor = Canvas;

        BuildNewsView();
        BuildWsbView();
        workspace.Controls.Add(newsView);
        workspace.Controls.Add(wsbView);

        Controls.Add(workspace);
        Controls.Add(header);
        Controls.Add(sidebar);

        newsTab = (Button)sidebar.Controls["newsTab"]!;
        wsbTab = (Button)sidebar.Controls["wsbTab"]!;

        newsTimer.Tick += async (_, _) => await RefreshNewsAsync();
        wsbTimer.Tick += async (_, _) => await RefreshWsbAsync();
        newsTimer.Start();
        wsbTimer.Start();

        Shown += async (_, _) =>
        {
            ShowNews();
            await RefreshNewsAsync();
            await RefreshWsbAsync();
        };
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 188, BackColor = Surface, Padding = new Padding(16, 22, 16, 16) };

        var brand = new Label
        {
            Text = "PULSE",
            ForeColor = Color.FromArgb(96, 165, 250),
            Font = new Font("Segoe UI Semibold", 18F),
            Dock = DockStyle.Top,
            Height = 54,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var subtitle = new Label
        {
            Text = "NEWS WORKSPACE",
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 8F),
            Dock = DockStyle.Top,
            Height = 28
        };

        var news = NavigationButton("News", "newsTab");
        news.Dock = DockStyle.Top;
        news.Click += (_, _) => ShowNews();

        var wsb = NavigationButton("WSB", "wsbTab");
        wsb.Dock = DockStyle.Top;
        wsb.Click += (_, _) => ShowWsb();

        var settings = NavigationButton("Settings", "settingsTab");
        settings.Dock = DockStyle.Bottom;
        settings.Click += (_, _) => ShowApiKeyPrompt();

        sidebar.Controls.Add(settings);
        sidebar.Controls.Add(wsb);
        sidebar.Controls.Add(news);
        sidebar.Controls.Add(subtitle);
        sidebar.Controls.Add(brand);
        return sidebar;
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Canvas, Padding = new Padding(24, 16, 24, 10) };
        pageTitle.Text = "News";
        pageTitle.Font = new Font("Segoe UI Semibold", 23F);
        pageTitle.AutoSize = true;
        pageTitle.Location = new Point(24, 15);

        status.Text = "Ready";
        status.ForeColor = Muted;
        status.AutoSize = true;
        status.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        status.Location = new Point(header.Width - 320, 28);
        header.Resize += (_, _) => status.Left = Math.Max(300, header.ClientSize.Width - status.Width - 155);

        var refresh = ActionButton("Refresh", Accent);
        refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refresh.Location = new Point(header.Width - 124, 19);
        refresh.Click += async (_, _) =>
        {
            if (newsView.Visible) await RefreshNewsAsync();
            else await RefreshWsbAsync();
        };
        header.Resize += (_, _) => refresh.Left = header.ClientSize.Width - refresh.Width - 24;

        header.Controls.Add(pageTitle);
        header.Controls.Add(status);
        header.Controls.Add(refresh);
        return header;
    }

    private void BuildNewsView()
    {
        newsView.Dock = DockStyle.Fill;
        newsView.BackColor = Canvas;

        var categoryBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = Canvas,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 6)
        };

        foreach (NewsCategory category in Enum.GetValues<NewsCategory>())
        {
            var button = CategoryButton(category.ToString());
            button.Click += async (_, _) =>
            {
                selectedCategory = category;
                UpdateCategorySelection();
                await RefreshNewsAsync();
            };
            categoryButtons[category] = button;
            categoryBar.Controls.Add(button);
        }

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 8,
            BackColor = Divider
        };
        split.Panel1.BackColor = Canvas;
        split.Panel2.BackColor = Surface;
        split.Panel2.Padding = new Padding(24);

        bool splitterInitialized = false;
        split.Resize += (_, _) =>
        {
            int availableWidth = split.ClientSize.Width - split.SplitterWidth;
            if (availableWidth <= 0)
                return;

            int minimumPanelWidth = Math.Min(240, availableWidth / 2);
            int desiredDistance = splitterInitialized
                ? split.SplitterDistance
                : (int)(availableWidth * 0.6);

            split.SplitterDistance = Math.Clamp(
                desiredDistance,
                minimumPanelWidth,
                availableWidth - minimumPanelWidth);
            splitterInitialized = true;
        };

        newsCards.Dock = DockStyle.Fill;
        newsCards.FlowDirection = FlowDirection.TopDown;
        newsCards.WrapContents = false;
        newsCards.AutoScroll = true;
        newsCards.BackColor = Canvas;
        newsCards.Padding = new Padding(0, 0, 8, 0);
        newsCards.SizeChanged += (_, _) => ResizeNewsCards();

        newsEmptyState.Text = "Loading headlines…";
        newsEmptyState.ForeColor = Muted;
        newsEmptyState.TextAlign = ContentAlignment.MiddleCenter;
        newsEmptyState.Dock = DockStyle.Fill;

        articleDetail.Dock = DockStyle.Fill;
        articleDetail.ReadOnly = true;
        articleDetail.BorderStyle = BorderStyle.None;
        articleDetail.BackColor = Surface;
        articleDetail.ForeColor = Color.FromArgb(226, 232, 240);
        articleDetail.Font = new Font("Segoe UI", 11F);
        articleDetail.Text = "Select a headline to see its details.";

        split.Panel1.Controls.Add(newsCards);
        split.Panel1.Controls.Add(newsEmptyState);
        split.Panel2.Controls.Add(articleDetail);
        newsView.Controls.Add(split);
        newsView.Controls.Add(categoryBar);
        UpdateCategorySelection();
    }

    private void BuildWsbView()
    {
        wsbView.Dock = DockStyle.Fill;
        wsbView.BackColor = Surface;
        wsbView.Padding = new Padding(30);
        wsbReport.Dock = DockStyle.Fill;
        wsbReport.AllowWebBrowserDrop = false;
        wsbReport.IsWebBrowserContextMenuEnabled = false;
        wsbReport.ScriptErrorsSuppressed = true;
        wsbReport.WebBrowserShortcutsEnabled = true;
        wsbReport.Navigating += (_, args) =>
        {
            if (args.Url.Scheme is not ("http" or "https"))
                return;

            args.Cancel = true;
            Process.Start(new ProcessStartInfo(args.Url.AbsoluteUri) { UseShellExecute = true });
        };
        wsbReport.DocumentText = "<html><body style='background:#111827;color:#cbd5e1;font-family:Segoe UI;padding:30px'>Loading WSB analysis…</body></html>";
        wsbView.Controls.Add(wsbReport);
    }

    private async Task RefreshNewsAsync()
    {
        if (newsRefreshInProgress)
            return;

        newsRefreshInProgress = true;
        status.Text = $"Loading {selectedCategory}…";
        try
        {
            var articles = await newsService.GetTopHeadlinesAsync(apiKey, selectedCategory, lifetime.Token);
            RenderNews(articles);
            status.Text = $"Updated {DateTime.Now:h:mm tt} · {articles.Count} stories";
        }
        catch (NewsApiException ex)
        {
            status.Text = ex.Message;
            newsEmptyState.Text = ex.Message;
            newsEmptyState.Visible = newsCards.Controls.Count == 0;
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        finally
        {
            newsRefreshInProgress = false;
        }
    }

    private async Task RefreshWsbAsync()
    {
        if (wsbRefreshInProgress)
            return;

        wsbRefreshInProgress = true;
        if (wsbView.Visible) status.Text = "Loading WSB analysis…";
        try
        {
            SellTheNewsSummary summary = await wsbService.FetchLatestSummaryAsync(wsbLanguage);
            wsbReport.DocumentText = WsbHtmlRenderer.Render(
                summary.Markdown,
                summary.Title,
                summary.AnalysisLabel,
                summary.UpdatedAt);
            if (wsbView.Visible) status.Text = $"WSB updated {DateTime.Now:h:mm tt}";
        }
        finally
        {
            wsbRefreshInProgress = false;
        }
    }

    private void RenderNews(IReadOnlyList<NewsArticle> articles)
    {
        newsCards.SuspendLayout();
        try
        {
            newsCards.Controls.Clear();
            foreach (NewsArticle article in articles)
                newsCards.Controls.Add(BuildArticleCard(article));

            newsEmptyState.Text = articles.Count == 0 ? "No headlines in this category." : string.Empty;
            newsEmptyState.Visible = articles.Count == 0;
            if (articles.Count > 0)
                ShowArticle(articles[0]);
        }
        finally
        {
            newsCards.ResumeLayout();
            ResizeNewsCards();
        }
    }

    private Panel BuildArticleCard(NewsArticle article)
    {
        var card = new Panel
        {
            Height = 118,
            Width = Math.Max(320, newsCards.ClientSize.Width - 30),
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(16, 13, 16, 10),
            BackColor = SurfaceRaised,
            Cursor = Cursors.Hand,
            Tag = article
        };
        var source = new Label
        {
            Text = $"{article.SourceName.ToUpperInvariant()}  ·  {article.DisplayTime}",
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(96, 165, 250),
            Font = new Font("Segoe UI Semibold", 8F),
            AutoEllipsis = true
        };
        var title = new Label
        {
            Text = article.Title,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F),
            AutoEllipsis = true
        };
        card.Controls.Add(title);
        card.Controls.Add(source);
        void select(object? _, EventArgs __) => ShowArticle(article);
        card.Click += select;
        source.Click += select;
        title.Click += select;
        return card;
    }

    private void ShowArticle(NewsArticle article)
    {
        articleDetail.Clear();
        articleDetail.SelectionFont = new Font("Segoe UI Semibold", 18F);
        articleDetail.SelectionColor = Color.White;
        articleDetail.AppendText(article.Title + "\n\n");
        articleDetail.SelectionFont = new Font("Segoe UI Semibold", 9F);
        articleDetail.SelectionColor = Color.FromArgb(96, 165, 250);
        articleDetail.AppendText($"{article.SourceName.ToUpperInvariant()}  ·  {article.DisplayTime}  ·  {article.Category}\n\n");
        articleDetail.SelectionFont = new Font("Segoe UI", 11F);
        articleDetail.SelectionColor = Color.FromArgb(203, 213, 225);
        articleDetail.AppendText(article.Description ?? article.ContentPreview ?? "No preview is available for this article.");
        articleDetail.AppendText("\n\nOpen original article →");
        articleDetail.Select(articleDetail.TextLength - 23, 23);
        articleDetail.SelectionColor = Color.FromArgb(96, 165, 250);
        articleDetail.SelectionProtected = false;
        articleDetail.Tag = article.Url;
        articleDetail.MouseUp -= OpenArticleFromDetail;
        articleDetail.MouseUp += OpenArticleFromDetail;
    }

    private void OpenArticleFromDetail(object? sender, MouseEventArgs e)
    {
        if (articleDetail.Tag is not Uri url)
            return;
        int index = articleDetail.GetCharIndexFromPosition(e.Location);
        if (index >= Math.Max(0, articleDetail.TextLength - 23))
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    private void ShowNews()
    {
        pageTitle.Text = "News";
        newsView.Visible = true;
        newsView.BringToFront();
        wsbView.Visible = false;
        SetNavigation(newsTab);
        status.Text = "Ready";
    }

    private void ShowWsb()
    {
        pageTitle.Text = "WSB";
        wsbView.Visible = true;
        wsbView.BringToFront();
        newsView.Visible = false;
        SetNavigation(wsbTab);
        status.Text = "WSB analysis";
    }

    private void ShowApiKeyPrompt()
    {
        using var dialog = new ApiKeyDialog(apiKey, wsbLanguage);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string previousLanguage = wsbLanguage;
        apiKey = dialog.ApiKey;
        wsbLanguage = dialog.WsbLanguage;
        keyStore.Save(apiKey);
        keyStore.SaveWsbLanguage(wsbLanguage);
        status.Text = string.IsNullOrWhiteSpace(apiKey)
            ? "Settings saved · anonymous NewsAPI mode"
            : "Settings saved · NewsAPI key enabled";

        if (newsView.Visible)
            _ = RefreshNewsAsync();
        if (previousLanguage != wsbLanguage)
            _ = RefreshWsbAsync();
    }

    private void UpdateCategorySelection()
    {
        foreach (var pair in categoryButtons)
        {
            bool selected = pair.Key == selectedCategory;
            pair.Value.BackColor = selected ? Accent : SurfaceRaised;
            pair.Value.ForeColor = selected ? Color.White : Muted;
        }
    }

    private void SetNavigation(Button selected)
    {
        foreach (var button in new[] { newsTab, wsbTab })
        {
            button.BackColor = button == selected ? SurfaceRaised : Surface;
            button.ForeColor = button == selected ? Color.White : Muted;
        }
    }

    private void ResizeNewsCards()
    {
        int width = Math.Max(320, newsCards.ClientSize.Width - 30);
        foreach (Control control in newsCards.Controls)
            control.Width = width;
    }

    private static Button NavigationButton(string text, string name) => new()
    {
        Name = name,
        Text = text,
        Height = 50,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        BackColor = Surface,
        ForeColor = Muted,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(14, 0, 0, 0),
        Font = new Font("Segoe UI Semibold", 10F),
        Cursor = Cursors.Hand
    };

    private static Button CategoryButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Height = 38,
        Padding = new Padding(12, 0, 12, 0),
        Margin = new Padding(0, 0, 8, 0),
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        Cursor = Cursors.Hand
    };

    private static Button ActionButton(string text, Color color) => new()
    {
        Text = text,
        Size = new Size(100, 38),
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        Font = new Font("Segoe UI Semibold", 9F),
        Cursor = Cursors.Hand
    };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        newsTimer.Stop();
        wsbTimer.Stop();
        lifetime.Cancel();
        httpClient.Dispose();
        lifetime.Dispose();
        base.OnFormClosing(e);
    }
}
