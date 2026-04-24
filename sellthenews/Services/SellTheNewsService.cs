using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using sellthenews.Models;

namespace sellthenews.Services
{
    public class SellTheNewsService
    {
        private static readonly Regex AnalysisLabelRegex = new Regex(@"Analysis\s*#\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly HttpClient client;

        public SellTheNewsService(HttpClient httpClient = null)
        {
            client = httpClient ?? new HttpClient();
        }

        public async Task<SellTheNewsSummary> FetchLatestSummaryAsync(string language = "zh")
        {
            var summary = new SellTheNewsSummary();

            try
            {
                string normalizedLanguage = (language ?? "zh").Trim().ToLowerInvariant();
                if (normalizedLanguage != "zh" && normalizedLanguage != "en")
                {
                    normalizedLanguage = "zh";
                }

                string url = $"https://sellthenews.org/api/wsb/latest?lang={normalizedLanguage}";
                string json = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                JsonElement payload = root;
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    payload = dataElement;
                }

                summary.Title = payload.TryGetProperty("title", out JsonElement t)
                    ? t.GetString() ?? "No title"
                    : "No title";

                summary.UpdatedAt = payload.TryGetProperty("updatedAt", out JsonElement u)
                    ? DateTime.TryParse(u.GetString(), out var dt) ? dt : DateTime.Now
                    : DateTime.Now;

                summary.Markdown = payload.TryGetProperty("markdown", out JsonElement m)
                    ? m.GetString() ?? ""
                    : "";

                if (payload.TryGetProperty("html", out JsonElement h))
                {
                    summary.AnalysisLabel = ExtractAnalysisLabel(h.GetString());
                }
            }
            catch (Exception ex)
            {
                summary.Title = "Fetch failed";
                summary.Markdown = ex.Message;
            }

            return summary;
        }

        private string ExtractAnalysisLabel(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            Match match = AnalysisLabelRegex.Match(html);
            return match.Success ? match.Value.Trim() : "";
        }

        public string ExtractMainSection(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "";

            int start = markdown.IndexOf("## 1.", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return ShortenText(markdown, 1200);

            int end = markdown.IndexOf("## 2.", start + 5, StringComparison.OrdinalIgnoreCase);

            string section = end > start
                ? markdown.Substring(start, end - start)
                : markdown.Substring(start);

            return ShortenText(section, 1200);
        }

        public string GetFullReport(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "";

            int start = markdown.IndexOf("## 1.", StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return FormatMarkdown(markdown);

            // Extract from "## 1." to the end, capturing all numbered sections
            string fullReport = markdown.Substring(start);
            return FormatMarkdown(fullReport);
        }

        private string FormatMarkdown(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var output = new System.Text.StringBuilder();
            bool inTable = false;
            bool lastWasEmpty = false;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // Skip empty lines but add spacing intelligently
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    if (!lastWasEmpty && output.Length > 0)
                    {
                        output.AppendLine();
                        lastWasEmpty = true;
                    }
                    continue;
                }

                lastWasEmpty = false;

                // Detect and format headers (## 1., ## 2., etc.)
                if (trimmed.StartsWith("##"))
                {
                    // Add extra spacing before section headers
                    if (output.Length > 0)
                        output.AppendLine();

                    // Remove markdown symbols but keep the text
                    string headerText = trimmed.Replace("#", "").Trim();
                    output.AppendLine("▼ " + headerText);
                    output.AppendLine(new string('═', Math.Min(50, headerText.Length + 4)));
                    continue;
                }

                // Detect table rows (contain |)
                if (trimmed.Contains("|"))
                {
                    if (!inTable)
                    {
                        output.AppendLine();
                        inTable = true;
                    }

                    // Keep table structure for RichTextBox formatting
                    output.AppendLine(trimmed);
                    continue;
                }

                // Exit table mode
                if (inTable && !trimmed.Contains("|"))
                {
                    inTable = false;
                    output.AppendLine();
                }

                // Regular text - preserve markdown formatting symbols for RichTextBox display
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    output.AppendLine(trimmed);
                }
            }

            return output.ToString().Trim();
        }

        private string ShortenText(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            // For RichTextBox display, preserve markdown formatting symbols
            string text = input.Trim();

            if (text.Length <= maxLength)
                return text;

            // Truncate while preserving markdown formatting for display
            return text.Substring(0, maxLength).TrimEnd() + "...";
        }
    }
}
