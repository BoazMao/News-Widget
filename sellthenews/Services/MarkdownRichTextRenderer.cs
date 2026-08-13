using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace sellthenews.Services;

internal static partial class WsbHtmlRenderer
{
    public static string Render(string markdown, string title, string analysisLabel, DateTime updatedAt)
    {
        var html = new StringBuilder();
        html.Append("""
<!doctype html>
<html>
<head>
<meta http-equiv="X-UA-Compatible" content="IE=edge" />
<style>
html,body{margin:0;padding:0;background:#111827;color:#cbd5e1;font-family:'Segoe UI',sans-serif;font-size:16px;line-height:1.65}
body{padding:30px 34px 60px}
.report-label{color:#60a5fa;font-size:12px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase}
h1{color:#fff;font-size:30px;line-height:1.2;margin:5px 0 4px}
.meta{color:#94a3b8;font-size:13px;margin-bottom:25px}
h2{color:#93c5fd;font-size:23px;line-height:1.3;margin:38px 0 18px;padding-bottom:10px;border-bottom:2px solid #2563eb}
h3{color:#bfdbfe;font-size:18px;margin:26px 0 10px}
p{margin:0 0 16px}
ul,ol{margin:8px 0 20px;padding-left:28px}
li{padding:3px 0 5px 5px}
li::marker{color:#60a5fa}
hr{height:1px;border:0;background:#374151;margin:26px 0}
blockquote{margin:18px 0;padding:13px 18px;border-left:4px solid #a78bfa;background:#1a2333;color:#ddd6fe;font-style:italic}
code{font-family:Consolas,monospace;color:#fdba74;background:#0f172a;padding:2px 5px;border-radius:4px}
pre{white-space:pre-wrap;background:#0f172a;border:1px solid #334155;padding:15px;border-radius:7px;color:#fdba74}
a{color:#60a5fa;text-decoration:none}a:hover{text-decoration:underline}
.table-wrap{overflow-x:auto;margin:18px 0 30px;border:1px solid #334155;border-radius:8px}
table{width:100%;border-collapse:collapse;table-layout:auto;background:#111827}
th{padding:11px 13px;background:#1e3a5f;color:#dbeafe;text-align:left;font-size:13px;white-space:nowrap;border-right:1px solid #36506f}
td{padding:11px 13px;vertical-align:top;border-top:1px solid #334155;border-right:1px solid #293548}
th:last-child,td:last-child{border-right:0}
tr:nth-child(even) td{background:#172033}
tr:hover td{background:#1e293b}
.stock-table td:first-child{color:#fff;font-weight:700;font-size:17px;white-space:nowrap}
.stock-table td:nth-child(2){color:#60a5fa;white-space:nowrap}
.stock-table td:nth-child(3){color:#6ee7b7;font-weight:600;min-width:170px}
.sector-table td:first-child{color:#fff;font-weight:600;min-width:190px}
.sector-table td:nth-child(2){color:#6ee7b7;font-weight:700;white-space:nowrap}
.event-table td:first-child{color:#fff;font-weight:600;white-space:nowrap}
.source-list{font-size:14px}
</style>
</head>
<body>
""");
        html.Append("<div class='report-label'>").Append(Encode(string.IsNullOrWhiteSpace(analysisLabel) ? "WSB Analysis" : analysisLabel)).Append("</div>");
        html.Append("<h1>").Append(Encode(title)).Append("</h1>");
        html.Append("<div class='meta'>Updated ").Append(Encode(updatedAt.ToString("g"))).Append("</div>");

        string[] lines = Normalize(markdown).Split('\n');
        bool inList = false;
        bool inCode = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();

            if (TryRenderTable(html, lines, ref index))
            {
                CloseList(html, ref inList);
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                CloseList(html, ref inList);
                html.Append(inCode ? "</code></pre>" : "<pre><code>");
                inCode = !inCode;
                continue;
            }

            if (inCode)
            {
                html.Append(Encode(lines[index])).Append('\n');
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                CloseList(html, ref inList);
                continue;
            }

            if (IsDivider(trimmed))
                continue;

            Match markdownHeading = HeadingRegex().Match(trimmed);
            if (markdownHeading.Success)
            {
                CloseList(html, ref inList);
                int level = Math.Min(3, markdownHeading.Groups[1].Value.Length + 1);
                html.Append("<h").Append(level).Append('>')
                    .Append(Inline(markdownHeading.Groups[2].Value))
                    .Append("</h").Append(level).Append('>');
                continue;
            }

            if (IsMajorSection(lines, index))
            {
                CloseList(html, ref inList);
                html.Append("<h2>").Append(Inline(trimmed)).Append("</h2>");
                index++;
                continue;
            }

            Match bullet = BulletRegex().Match(trimmed);
            if (bullet.Success)
            {
                if (!inList)
                {
                    html.Append("<ul>");
                    inList = true;
                }
                html.Append("<li>").Append(Inline(bullet.Groups[1].Value)).Append("</li>");
                continue;
            }

            CloseList(html, ref inList);

            if (trimmed.StartsWith("■", StringComparison.Ordinal) || trimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                string quote = trimmed.TrimStart('■', '>', ' ', '-');
                html.Append("<blockquote>").Append(Inline(quote)).Append("</blockquote>");
            }
            else if (trimmed.EndsWith(':') && trimmed.Length < 90)
            {
                html.Append("<h3>").Append(Inline(trimmed.TrimEnd(':'))).Append("</h3>");
            }
            else
            {
                html.Append("<p>").Append(Inline(trimmed)).Append("</p>");
            }
        }

        CloseList(html, ref inList);
        if (inCode)
            html.Append("</code></pre>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static bool TryRenderTable(StringBuilder html, string[] lines, ref int index)
    {
        string header = lines[index].Trim();
        string cssClass = header.StartsWith("Sector/Theme", StringComparison.OrdinalIgnoreCase) ? "sector-table"
            : header.StartsWith("Ticker", StringComparison.OrdinalIgnoreCase) ? "stock-table"
            : header.StartsWith("Date", StringComparison.OrdinalIgnoreCase) ? "event-table"
            : string.Empty;

        if (cssClass.Length == 0)
            return false;

        string[] headers = SplitColumns(header);
        var rows = new List<string[]>();
        int cursor = index + 1;

        while (cursor < lines.Length && !string.IsNullOrWhiteSpace(lines[cursor]))
        {
            string[] cells = SplitColumns(lines[cursor]);
            if (cells.Length < 2)
                break;
            rows.Add(cells);
            cursor++;
        }

        if (rows.Count == 0)
            return false;

        html.Append("<div class='table-wrap'><table class='").Append(cssClass).Append("'><thead><tr>");
        foreach (string column in headers)
            html.Append("<th>").Append(Inline(column)).Append("</th>");
        html.Append("</tr></thead><tbody>");

        foreach (string[] sourceCells in rows)
        {
            string[] cells = NormalizeCells(sourceCells, headers.Length);
            html.Append("<tr>");
            foreach (string cell in cells)
                html.Append("<td>").Append(Inline(cell)).Append("</td>");
            html.Append("</tr>");
        }

        html.Append("</tbody></table></div>");
        index = cursor - 1;
        return true;
    }

    private static string[] NormalizeCells(string[] cells, int expectedCount)
    {
        if (cells.Length <= expectedCount)
            return cells.Concat(Enumerable.Repeat(string.Empty, expectedCount - cells.Length)).ToArray();

        var normalized = cells.Take(expectedCount - 1).ToList();
        normalized.Add(string.Join(" ", cells.Skip(expectedCount - 1)));
        return normalized.ToArray();
    }

    private static string Inline(string value)
    {
        string encoded = Encode(value);
        encoded = LinkRegex().Replace(encoded, "<a href='$2'>$1</a>");
        encoded = BoldRegex().Replace(encoded, "<strong>$1</strong>");
        encoded = ItalicRegex().Replace(encoded, "<em>$1</em>");
        encoded = CodeRegex().Replace(encoded, "<code>$1</code>");
        return encoded;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static void CloseList(StringBuilder html, ref bool inList)
    {
        if (!inList)
            return;
        html.Append("</ul>");
        inList = false;
    }

    private static bool IsMajorSection(string[] lines, int index) =>
        MajorSectionRegex().IsMatch(lines[index].Trim()) &&
        index + 1 < lines.Length &&
        IsDivider(lines[index + 1].Trim());

    private static bool IsDivider(string line) =>
        line.Length >= 8 && line.All(character => character is '━' or '─' or '═' or '-');

    private static string[] SplitColumns(string line) =>
        ColumnSeparatorRegex().Split(line.Trim()).Where(cell => cell.Length > 0).ToArray();

    private static string Normalize(string markdown) =>
        (markdown ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\d+\.\s+.+$")]
    private static partial Regex MajorSectionRegex();

    [GeneratedRegex(@"^(?:[-*+•])\s+(.+)$")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"\s{3,}")]
    private static partial Regex ColumnSeparatorRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^)]+)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*|__(.+?)__")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*([^*]+?)\*")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex CodeRegex();
}
