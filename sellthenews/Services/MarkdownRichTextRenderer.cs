using System.Text.RegularExpressions;

namespace sellthenews.Services;

internal static partial class MarkdownRichTextRenderer
{
    private static readonly Color BodyColor = Color.FromArgb(203, 213, 225);
    private static readonly Color HeadingColor = Color.FromArgb(147, 197, 253);
    private static readonly Color AccentColor = Color.FromArgb(96, 165, 250);
    private static readonly Color MutedColor = Color.FromArgb(148, 163, 184);
    private static readonly Color QuoteColor = Color.FromArgb(165, 180, 252);
    private static readonly Color CodeColor = Color.FromArgb(253, 186, 116);

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
        Append(target, "────────────────────────────────────────\n\n", 9F, FontStyle.Regular, Color.FromArgb(55, 65, 81));

        string[] lines = Normalize(markdown).Split('\n');
        bool inCodeBlock = false;
        bool previousBlank = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
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
            string trimmed = line.TrimStart();

            if (TryRenderHeading(target, trimmed))
                continue;

            if (HorizontalRuleRegex().IsMatch(trimmed))
            {
                Append(target, "────────────────────────────────────────\n", 9F, FontStyle.Regular, Color.FromArgb(55, 65, 81));
                continue;
            }

            if (trimmed.StartsWith("> ", StringComparison.Ordinal))
            {
                Append(target, "▌ ", 11F, FontStyle.Bold, AccentColor);
                RenderInline(target, trimmed[2..], 11F, FontStyle.Italic, QuoteColor);
                Append(target, "\n");
                continue;
            }

            Match bullet = BulletRegex().Match(trimmed);
            if (bullet.Success)
            {
                Append(target, "  •  ", 11F, FontStyle.Bold, AccentColor);
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

            if (IsTableRow(trimmed))
            {
                string tableText = string.Join("   ", trimmed.Trim('|').Split('|').Select(cell => cell.Trim()));
                if (!TableSeparatorRegex().IsMatch(trimmed))
                {
                    Append(target, tableText + "\n", 9.5F, FontStyle.Regular, BodyColor, "Cascadia Mono");
                }
                continue;
            }

            RenderInline(target, trimmed, 11F, FontStyle.Regular, BodyColor);
            Append(target, "\n");
        }

        target.Select(0, 0);
        target.ScrollToCaret();
        target.ResumeLayout();
    }

    private static bool TryRenderHeading(RichTextBox target, string line)
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
            Append(target, "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n", 8F, FontStyle.Regular, Color.FromArgb(37, 99, 235));
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
            if (token.StartsWith("**", StringComparison.Ordinal))
                Append(target, token[2..^2], size, baseStyle | FontStyle.Bold, Color.White);
            else if (token.StartsWith("__", StringComparison.Ordinal))
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

    private static void Append(
        RichTextBox target,
        string text,
        float size = 11F,
        FontStyle style = FontStyle.Regular,
        Color? color = null,
        string family = "Segoe UI")
    {
        target.SelectionFont = new Font(family, size, style);
        target.SelectionColor = color ?? BodyColor;
        target.AppendText(text);
    }

    private static bool IsTableRow(string line) =>
        line.Count(character => character == '|') >= 2;

    private static string Normalize(string markdown) =>
        (markdown ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    [GeneratedRegex(@"^(#{1,6})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^[-*_]{3,}\s*$")]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^(?:[-*+])\s+(.+)$")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"^(\d+)[.)]\s+(.+)$")]
    private static partial Regex NumberedRegex();

    [GeneratedRegex(@"^\|?\s*:?-{3,}")]
    private static partial Regex TableSeparatorRegex();

    [GeneratedRegex(@"(\*\*.+?\*\*|__.+?__|`.+?`|(?<!\*)\*[^*]+?\*)")]
    private static partial Regex InlineRegex();
}
