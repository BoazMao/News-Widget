using System.Text.RegularExpressions;

namespace sellthenews.Services;

internal static partial class MarkdownRichTextRenderer
{
    private static readonly Color BodyColor = Color.FromArgb(203, 213, 225);
    private static readonly Color HeadingColor = Color.FromArgb(147, 197, 253);
    private static readonly Color AccentColor = Color.FromArgb(96, 165, 250);
    private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);
    private static readonly Color QuoteColor = Color.FromArgb(196, 181, 253);
    private static readonly Color CodeColor = Color.FromArgb(253, 186, 116);
    private static readonly Color DividerColor = Color.FromArgb(55, 65, 81);
    private static readonly Color PositiveColor = Color.FromArgb(110, 231, 183);

    public static void Render(
        RichTextBox target,
        string markdown,
        string title,
        string analysisLabel,
        DateTime updatedAt)
    {
        target.SuspendLayout();
        target.Clear();
        target.DetectUrls = true;

        Append(target, string.IsNullOrWhiteSpace(analysisLabel) ? "WSB ANALYSIS" : analysisLabel.ToUpperInvariant(),
            9F, FontStyle.Bold, AccentColor);
        Append(target, "\n");
        Append(target, title, 22F, FontStyle.Bold, Color.White);
        Append(target, $"\nUpdated {updatedAt:g}\n", 9F, FontStyle.Regular, MutedColor);
        AppendDivider(target, heavy: true);

        string[] lines = Normalize(markdown).Split('\n');
        bool inCodeBlock = false;
        bool previousBlank = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                if (!inCodeBlock)
                    Append(target, "\n");
                continue;
            }

            if (inCodeBlock)
            {
                Append(target, line + "\n", 10F, FontStyle.Regular, CodeColor, "Cascadia Mono");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (!previousBlank)
                    Append(target, "\n");
                previousBlank = true;
                continue;
            }

            previousBlank = false;

            if (TryRenderStructuredTable(target, lines, ref index))
                continue;

            if (TryRenderMarkdownHeading(target, trimmed))
                continue;

            if (IsMajorSection(lines, index))
            {
                RenderSectionBanner(target, trimmed);
                index++;
                continue;
            }

            if (IsDivider(trimmed))
            {
                AppendDivider(target, heavy: false);
                continue;
            }

            if (trimmed.StartsWith("■", StringComparison.Ordinal))
            {
                RenderQuoteCard(target, trimmed.TrimStart('■', ' ', '-'));
                continue;
            }

            if (trimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                RenderQuoteCard(target, trimmed[2..]);
                continue;
            }

            Match bullet = BulletRegex().Match(trimmed);
            if (bullet.Success)
            {
                Append(target, "  ●  ", 9F, FontStyle.Bold, AccentColor);
                RenderInline(target, bullet.Groups[1].Value, 11F, FontStyle.Regular, BodyColor);
                Append(target, "\n");
                continue;
            }

            Match numbered = NumberedRegex().Match(trimmed);
            if (numbered.Success)
            {
                Append(target, $"  {numbered.Groups[1].Value}.  ", 11F, FontStyle.Bold, AccentColor);
                RenderInline(target, numbered.Groups[2].Value, 11F, FontStyle.Regular, BodyColor);
                Append(target, "\n");
                continue;
            }

            if (IsLabel(trimmed))
            {
                RenderInline(target, trimmed, 11.5F, FontStyle.Bold, HeadingColor);
                Append(target, "\n");
                continue;
            }

            if (IsPipeTableRow(trimmed))
            {
                RenderPipeTableRow(target, trimmed);
                continue;
            }

            RenderInline(target, trimmed, 11F, FontStyle.Regular, BodyColor);
            Append(target, "\n");
        }

        target.Select(0, 0);
        target.ScrollToCaret();
        target.ResumeLayout();
    }

    private static bool TryRenderStructuredTable(RichTextBox target, string[] lines, ref int index)
    {
        string header = lines[index].Trim();
        TableKind kind = GetTableKind(header);
        if (kind == TableKind.None)
            return false;

        string[] headers = SplitColumns(header);
        var rows = new List<string[]>();
        int cursor = index + 1;

        while (cursor < lines.Length && !string.IsNullOrWhiteSpace(lines[cursor]))
        {
            string candidate = lines[cursor].Trim();
            if (IsDivider(candidate) || HeadingRegex().IsMatch(candidate))
                break;

            string[] cells = SplitColumns(candidate);
            if (cells.Length < 2)
                break;

            rows.Add(cells);
            cursor++;
        }

        if (rows.Count == 0)
            return false;

        Append(target, "\n");
        if (kind == TableKind.Stock)
            RenderStockCards(target, headers, rows);
        else
            RenderGrid(target, headers, rows);

        index = cursor - 1;
        return true;
    }

    private static void RenderGrid(RichTextBox target, string[] headers, List<string[]> rows)
    {
        Append(target, "┌─ " + string.Join("  ·  ", headers) + "\n", 9F, FontStyle.Bold, AccentColor, "Cascadia Mono");
        Append(target, "├────────────────────────────────────────────────────────\n",
            9F, FontStyle.Regular, DividerColor, "Cascadia Mono");

        foreach (string[] cells in rows)
        {
            Append(target, "│ ", 9F, FontStyle.Regular, AccentColor, "Cascadia Mono");
            Append(target, cells[0], 10F, FontStyle.Bold, Color.White);
            Append(target, "\n");

            for (int cellIndex = 1; cellIndex < cells.Length; cellIndex++)
            {
                string label = cellIndex < headers.Length ? headers[cellIndex] : $"Column {cellIndex + 1}";
                Append(target, $"│   {label}: ", 9F, FontStyle.Bold, MutedColor);
                Color valueColor = label.Contains("Heat", StringComparison.OrdinalIgnoreCase)
                    ? PositiveColor : BodyColor;
                RenderInline(target, cells[cellIndex], 10F, FontStyle.Regular, valueColor);
                Append(target, "\n");
            }

            Append(target, "├────────────────────────────────────────────────────────\n",
                9F, FontStyle.Regular, DividerColor, "Cascadia Mono");
        }

        Append(target, "└────────────────────────────────────────────────────────\n\n",
            9F, FontStyle.Regular, DividerColor, "Cascadia Mono");
    }

    private static void RenderStockCards(RichTextBox target, string[] headers, List<string[]> rows)
    {
        foreach (string[] cells in rows)
        {
            Append(target, "┌─ ", 9F, FontStyle.Regular, AccentColor, "Cascadia Mono");
            Append(target, cells[0], 14F, FontStyle.Bold, Color.White);
            if (cells.Length > 1)
                Append(target, $"   {headers.ElementAtOrDefault(1) ?? "Mentions"}: {cells[1]}", 9F, FontStyle.Bold, AccentColor);
            Append(target, "\n");

            if (cells.Length > 2)
            {
                Append(target, "│  Sentiment  ", 9F, FontStyle.Bold, MutedColor);
                RenderInline(target, cells[2], 10F, FontStyle.Bold, PositiveColor);
                Append(target, "\n");
            }

            if (cells.Length > 3)
            {
                Append(target, "│  View       ", 9F, FontStyle.Bold, MutedColor);
                RenderInline(target, string.Join(" ", cells.Skip(3)), 10.5F, FontStyle.Regular, BodyColor);
                Append(target, "\n");
            }

            Append(target, "└────────────────────────────────────────────────────────\n\n",
                9F, FontStyle.Regular, DividerColor, "Cascadia Mono");
        }
    }

    private static void RenderSectionBanner(RichTextBox target, string heading)
    {
        Append(target, "\n");
        Append(target, heading, 17F, FontStyle.Bold, HeadingColor);
        Append(target, "\n");
        Append(target, "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n",
            8F, FontStyle.Regular, Color.FromArgb(37, 99, 235));
    }

    private static void RenderQuoteCard(RichTextBox target, string text)
    {
        Append(target, "▌ ", 13F, FontStyle.Bold, QuoteColor);
        RenderInline(target, text, 11F, FontStyle.Italic, QuoteColor);
        Append(target, "\n");
    }

    private static bool TryRenderMarkdownHeading(RichTextBox target, string line)
    {
        Match match = HeadingRegex().Match(line);
        if (!match.Success)
            return false;

        int level = match.Groups[1].Value.Length;
        float size = level switch { 1 => 20F, 2 => 16F, 3 => 13F, _ => 11.5F };
        Append(target, level <= 2 ? "\n" : string.Empty);
        RenderInline(target, match.Groups[2].Value, size, FontStyle.Bold, HeadingColor);
        Append(target, "\n");
        if (level <= 2)
            AppendDivider(target, heavy: true);
        return true;
    }

    private static void RenderInline(
        RichTextBox target,
        string text,
        float size,
        FontStyle baseStyle,
        Color baseColor)
    {
        int position = 0;
        foreach (Match match in InlineRegex().Matches(text))
        {
            if (match.Index > position)
                Append(target, text[position..match.Index], size, baseStyle, baseColor);

            string token = match.Value;
            Match link = LinkRegex().Match(token);
            if (link.Success)
            {
                Append(target, link.Groups[1].Value, size, baseStyle | FontStyle.Underline, AccentColor);
                Append(target, $"  {link.Groups[2].Value}", 8.5F, FontStyle.Regular, MutedColor);
            }
            else if (token.StartsWith("**", StringComparison.Ordinal) || token.StartsWith("__", StringComparison.Ordinal))
                Append(target, token[2..^2], size, baseStyle | FontStyle.Bold, Color.White);
            else if (token.StartsWith("`", StringComparison.Ordinal))
                Append(target, token[1..^1], size - 0.5F, FontStyle.Regular, CodeColor, "Cascadia Mono");
            else
                Append(target, token[1..^1], size, baseStyle | FontStyle.Italic, baseColor);

            position = match.Index + match.Length;
        }

        if (position < text.Length)
            Append(target, text[position..], size, baseStyle, baseColor);
    }

    private static void RenderPipeTableRow(RichTextBox target, string line)
    {
        if (TableSeparatorRegex().IsMatch(line))
            return;

        string text = string.Join("   ", line.Trim('|').Split('|').Select(cell => cell.Trim()));
        Append(target, "│ " + text + "\n", 9.5F, FontStyle.Regular, BodyColor, "Cascadia Mono");
    }

    private static void AppendDivider(RichTextBox target, bool heavy)
    {
        string divider = heavy
            ? "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
            : "────────────────────────────────────────────────────────";
        Append(target, divider + "\n\n", 8F, FontStyle.Regular, DividerColor);
    }

    private static bool IsMajorSection(string[] lines, int index) =>
        MajorSectionRegex().IsMatch(lines[index].Trim()) &&
        index + 1 < lines.Length &&
        IsDivider(lines[index + 1].Trim());

    private static bool IsDivider(string line) =>
        HorizontalRuleRegex().IsMatch(line) ||
        line.Length >= 8 && line.All(character => character is '━' or '─' or '═' or '-');

    private static bool IsLabel(string line) =>
        line.EndsWith(':') && line.Length <= 80;

    private static bool IsPipeTableRow(string line) =>
        line.Count(character => character == '|') >= 2;

    private static TableKind GetTableKind(string header)
    {
        if (header.StartsWith("Ticker", StringComparison.OrdinalIgnoreCase))
            return TableKind.Stock;
        if (header.StartsWith("Sector/Theme", StringComparison.OrdinalIgnoreCase) ||
            header.StartsWith("Date", StringComparison.OrdinalIgnoreCase))
            return TableKind.Grid;
        return TableKind.None;
    }

    private static string[] SplitColumns(string line) =>
        ColumnSeparatorRegex().Split(line.Trim()).Where(cell => cell.Length > 0).ToArray();

    private static string Normalize(string markdown) =>
        (markdown ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    private enum TableKind
    {
        None,
        Grid,
        Stock
    }

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\d+\.\s+.+$")]
    private static partial Regex MajorSectionRegex();

    [GeneratedRegex(@"^[-*_]{3,}\s*$")]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^(?:[-*+•])\s+(.+)$")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"^(\d+)[.)]\s+(.+)$")]
    private static partial Regex NumberedRegex();

    [GeneratedRegex(@"^\|?\s*:?-{3,}")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"\s{3,}")]
    private static partial Regex ColumnSeparatorRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^)]+)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(\[[^\]]+\]\(https?://[^)]+\)|\*\*.+?\*\*|__.+?__|`.+?`|(?<!\*)\*[^*]+?\*)")]
    private static partial Regex InlineRegex();
}
