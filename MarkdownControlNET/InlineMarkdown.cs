using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MarkdownGdi;

[Flags]
public enum InlineStyle
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Strike = 4,
    Code = 8
}

public enum InlineRunKind
{
    Text,
    Image,
    Link,
    FootnoteReference
}

public readonly record struct InlineRun
{
    private const string ImagePlaceholderText = "\uFFFC";

    public InlineRun(string text, InlineStyle style, Color foregroundColor = default, Color backgroundColor = default)
    {
        Kind = InlineRunKind.Text;
        Text = text ?? string.Empty;
        Style = style;
        AltText = string.Empty;
        Source = string.Empty;
        Href = string.Empty;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
    }

    private InlineRun(
        InlineRunKind kind,
        string text,
        InlineStyle style,
        string altText,
        string source,
        string href,
        Color foregroundColor,
        Color backgroundColor)
    {
        Kind = kind;
        Text = text ?? string.Empty;
        Style = style;
        AltText = altText ?? string.Empty;
        Source = source ?? string.Empty;
        Href = href ?? string.Empty;
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
    }

    public InlineRunKind Kind { get; }
    public string Text { get; }
    public InlineStyle Style { get; }
    public string AltText { get; }
    public string Source { get; }
    public string Href { get; }
    public Color ForegroundColor { get; }
    public Color BackgroundColor { get; }

    public bool IsImage => Kind == InlineRunKind.Image;
    public bool IsLink => Kind == InlineRunKind.Link || Kind == InlineRunKind.FootnoteReference;
    public bool IsFootnoteReference => Kind == InlineRunKind.FootnoteReference;
    public bool HasForegroundColor => !ForegroundColor.IsEmpty;
    public bool HasBackgroundColor => !BackgroundColor.IsEmpty;
    public bool HasCustomColors => HasForegroundColor || HasBackgroundColor;
    public int VisualLength => Text.Length;

    public static InlineRun Image(string altText, string source, InlineStyle style, Color foregroundColor = default, Color backgroundColor = default)
        => new(InlineRunKind.Image, ImagePlaceholderText, style, altText, source, string.Empty, foregroundColor, backgroundColor);

    public static InlineRun Link(string text, string href, InlineStyle style, Color foregroundColor = default, Color backgroundColor = default)
        => new(InlineRunKind.Link, text, style, string.Empty, string.Empty, href, foregroundColor, backgroundColor);

    public static InlineRun FootnoteReference(string text, string href, InlineStyle style, Color foregroundColor = default, Color backgroundColor = default)
        => new(InlineRunKind.FootnoteReference, text, style, string.Empty, string.Empty, href, foregroundColor, backgroundColor);
}

public sealed record InlineParseResult(
    string Text,
    IReadOnlyList<InlineRun> Runs,
    int[] VisualToSource, // output vis-col -> input src-col
    int[] SourceToVisual  // input src-col -> output vis-col
);

public static class InlineMarkdown
{
    public static InlineParseResult Parse(string input)
        => ParseCore(input);

    private static InlineParseResult ParseCore(string input)
    {
        input ??= string.Empty;

        if (input.Length == 0)
        {
            return new InlineParseResult(
                string.Empty,
                Array.Empty<InlineRun>(),
                new[] { 0 },
                new[] { 0 });
        }

        int srcN = input.Length;

        var runs = new List<InlineRun>();
        var sb = new StringBuilder();

        InlineStyle style = InlineStyle.None;

        // Mapping arrays
        var sourceToVisual = new int[srcN + 1];
        var visualToSource = new List<int>(srcN + 1) { 0 };

        int outPos = 0;
        int i = 0;

        sourceToVisual[0] = 0;

        void FlushRun()
        {
            if (sb.Length == 0) return;
            runs.Add(new InlineRun(sb.ToString(), style));
            sb.Clear();
        }

        while (i < srcN)
        {
            char ch = input[i];

            // Escape: \* \_ \~ \\ \`
            if (ch == '\\' && i + 1 < srcN && IsEscapable(input[i + 1]))
            {
                // '\' hidden
                sourceToVisual[i + 1] = outPos;

                // escaped char visible as literal
                sb.Append(input[i + 1]);
                outPos++;
                visualToSource.Add(i + 2);     // caret after consumed escaped char
                sourceToVisual[i + 2] = outPos;

                i += 2;
                continue;
            }

            // Inline code: `code` or ```code``` (1 or >=3, same delimiter to close)
            if (TryReadCodeSpan(input, i, out int codeMarkerLen, out int closeMarkerPos))
            {
                FlushRun();

                // opening marker hidden
                for (int k = 1; k <= codeMarkerLen && (i + k) <= srcN; k++)
                    sourceToVisual[i + k] = outPos;

                int innerStart = i + codeMarkerLen;
                int innerEnd = closeMarkerPos;

                if (innerEnd > innerStart)
                {
                    string codeText = input.Substring(innerStart, innerEnd - innerStart);
                    runs.Add(new InlineRun(codeText, style | InlineStyle.Code));

                    for (int s = innerStart; s < innerEnd; s++)
                    {
                        outPos++;
                        visualToSource.Add(s + 1);
                        sourceToVisual[s + 1] = outPos;
                    }
                }

                // closing marker hidden
                for (int k = 1; k <= codeMarkerLen && (closeMarkerPos + k) <= srcN; k++)
                    sourceToVisual[closeMarkerPos + k] = outPos;

                i = closeMarkerPos + codeMarkerLen;
                continue;
            }

            if (TryReadColorSpan(
                input,
                i,
                out int colorEndExclusive,
                out int colorContentStart,
                out int colorContentEnd,
                out Color colorValue,
                out bool isForegroundColor))
            {
                FlushRun();

                for (int s = i; s < colorContentStart; s++)
                    sourceToVisual[s + 1] = outPos;

                InlineParseResult nested = ParseCore(input[colorContentStart..colorContentEnd]);
                foreach (InlineRun nestedRun in nested.Runs)
                {
                    runs.Add(ApplyColorPresentation(
                        nestedRun,
                        style,
                        isForegroundColor ? colorValue : Color.Empty,
                        isForegroundColor ? Color.Empty : colorValue));
                }

                for (int s = 0; s <= (colorContentEnd - colorContentStart); s++)
                    sourceToVisual[colorContentStart + s] = outPos + nested.SourceToVisual[s];

                for (int v = 1; v < nested.VisualToSource.Length; v++)
                    visualToSource.Add(colorContentStart + nested.VisualToSource[v]);

                outPos += nested.Text.Length;

                for (int s = colorContentEnd; s < colorEndExclusive; s++)
                    sourceToVisual[s + 1] = outPos;

                i = colorEndExclusive;
                continue;
            }

            if (TryReadImageSpan(input, i, out int imageEndExclusive, out string altText, out string imageSource))
            {
                FlushRun();

                for (int s = i + 1; s < imageEndExclusive; s++)
                    sourceToVisual[s] = outPos;

                runs.Add(InlineRun.Image(altText, imageSource, style));
                outPos++;
                visualToSource.Add(imageEndExclusive);
                sourceToVisual[imageEndExclusive] = outPos;

                i = imageEndExclusive;
                continue;
            }

            if (TryReadFootnoteReference(
                input,
                i,
                out int footnoteLabelStart,
                out int footnoteLabelEnd,
                out int footnoteEndExclusive,
                out string footnoteLabel))
            {
                FlushRun();

                for (int s = i + 1; s <= footnoteLabelStart; s++)
                    sourceToVisual[s] = outPos;

                runs.Add(InlineRun.FootnoteReference(
                    footnoteLabel,
                    MarkdownFootnoteHelper.BuildDefinitionAnchor(footnoteLabel),
                    style));

                for (int s = footnoteLabelStart; s < footnoteLabelEnd; s++)
                {
                    outPos++;
                    visualToSource.Add(s + 1);
                    sourceToVisual[s + 1] = outPos;
                }

                for (int s = footnoteLabelEnd; s < footnoteEndExclusive; s++)
                    sourceToVisual[s + 1] = outPos;

                i = footnoteEndExclusive;
                continue;
            }

            if (TryReadLinkSpan(
                input,
                i,
                out int linkTextStart,
                out int linkTextEnd,
                out int linkEndExclusive,
                out string linkText,
                out string linkTarget))
            {
                FlushRun();

                sourceToVisual[linkTextStart] = outPos;
                runs.Add(InlineRun.Link(linkText, linkTarget, style));

                for (int s = linkTextStart; s < linkTextEnd; s++)
                {
                    outPos++;
                    visualToSource.Add(s + 1);
                    sourceToVisual[s + 1] = outPos;
                }

                for (int s = linkTextEnd; s < linkEndExclusive; s++)
                    sourceToVisual[s + 1] = outPos;

                i = linkEndExclusive;
                continue;
            }

            if (TryReadBareUrl(input, i, out int urlEndExclusive, out string bareUrl))
            {
                FlushRun();

                string urlText = input[i..urlEndExclusive];
                runs.Add(InlineRun.Link(urlText, bareUrl, style));

                for (int s = i; s < urlEndExclusive; s++)
                {
                    outPos++;
                    visualToSource.Add(s + 1);
                    sourceToVisual[s + 1] = outPos;
                }

                i = urlEndExclusive;
                continue;
            }

            // Marker handling (open/close)
            if (TryReadMarker(input, i, style, out int markerLen, out InlineStyle toggleStyle))
            {
                FlushRun();

                // hidden marker chars map to current visual pos
                for (int k = 1; k <= markerLen && (i + k) <= srcN; k++)
                    sourceToVisual[i + k] = outPos;

                style = Toggle(style, toggleStyle);
                i += markerLen;
                continue;
            }

            // normal visible char
            sb.Append(ch);
            outPos++;
            visualToSource.Add(i + 1);
            sourceToVisual[i + 1] = outPos;
            i++;
        }

        FlushRun();

        // Important: visual end must map to real source end
        if (visualToSource.Count == 0)
            visualToSource.Add(srcN);
        else
            visualToSource[^1] = srcN;

        sourceToVisual[srcN] = outPos;

        // Fill still-unset gaps in source->visual with monotonic carry
        // (keeps cursor navigation deterministic across hidden markers)
        int carry = 0;
        for (int s = 0; s <= srcN; s++)
        {
            if (sourceToVisual[s] < carry)
                sourceToVisual[s] = carry;

            carry = sourceToVisual[s];
        }

        // Build text from runs
        string text = string.Concat(runs.ConvertAll(r => r.Text));

        return new InlineParseResult(
            text,
            runs,
            visualToSource.ToArray(),
            sourceToVisual);
    }

    public static Font CreateStyledFont(Font baseFont, InlineStyle style)
    {
        FontStyle fs = baseFont.Style;

        if ((style & InlineStyle.Bold) != 0) fs |= FontStyle.Bold;
        if ((style & InlineStyle.Italic) != 0) fs |= FontStyle.Italic;
        if ((style & InlineStyle.Strike) != 0) fs |= FontStyle.Strikeout;

        // InlineStyle.Code steuert Rendering (Mono + Background), kein FontStyle-Flag
        return new Font(
            baseFont.FontFamily,
            baseFont.Size,
            fs,
            baseFont.Unit,
            baseFont.GdiCharSet,
            baseFont.GdiVerticalFont);
    }

    private static InlineStyle Toggle(InlineStyle current, InlineStyle toggle)
        => (current & toggle) == toggle
            ? current & ~toggle
            : current | toggle;

    private static bool IsEscapable(char c)
        => c is '*' or '_' or '~' or '\\' or '`' or '!' or '[' or ']' or '(' or ')';

    private static InlineRun ApplyColorPresentation(
        InlineRun run,
        InlineStyle inheritedStyle,
        Color foregroundColor,
        Color backgroundColor)
    {
        InlineStyle style = run.Style | inheritedStyle;
        Color mergedForeground = run.HasForegroundColor ? run.ForegroundColor : foregroundColor;
        Color mergedBackground = run.HasBackgroundColor ? run.BackgroundColor : backgroundColor;

        if (run.IsFootnoteReference)
            return InlineRun.FootnoteReference(run.Text, run.Href, style, mergedForeground, mergedBackground);

        if (run.IsLink)
            return InlineRun.Link(run.Text, run.Href, style, mergedForeground, mergedBackground);

        if (run.IsImage)
            return InlineRun.Image(run.AltText, run.Source, style, mergedForeground, mergedBackground);

        return new InlineRun(run.Text, style, mergedForeground, mergedBackground);
    }

    private static bool TryReadColorSpan(
        string s,
        int i,
        out int endExclusive,
        out int contentStart,
        out int contentEnd,
        out Color color,
        out bool isForegroundColor)
    {
        endExclusive = -1;
        contentStart = -1;
        contentEnd = -1;
        color = Color.Empty;
        isForegroundColor = false;

        if (!TryReadColorDirective(s, i, out int closeBracket, out color, out isForegroundColor))
            return false;

        if (closeBracket + 1 >= s.Length || s[closeBracket + 1] != '(')
            return false;

        contentStart = closeBracket + 2;
        int closeParen = FindImageSourceEnd(s, contentStart);
        if (closeParen < 0)
            return false;

        contentEnd = closeParen;
        endExclusive = closeParen + 1;
        return true;
    }

    private static bool TryReadColorDirective(
        string s,
        int i,
        out int closeBracket,
        out Color color,
        out bool isForegroundColor)
    {
        closeBracket = -1;
        color = Color.Empty;
        isForegroundColor = false;

        if (i < 0 || i + 6 >= s.Length)
            return false;

        if (s[i] != '!' || s[i + 1] != '[')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        if (!StartsWithReservedColorPrefix(s, i + 2, out isForegroundColor))
            return false;

        closeBracket = FindUnescapedChar(s, ']', i + 2);
        if (closeBracket < 0)
            return false;

        string token = s[(i + 2)..closeBracket].Trim();
        const int prefixLength = 3;
        if (token.Length <= prefixLength)
            return false;

        string rawColor = token[prefixLength..].Trim();
        if (!TryParseColor(rawColor, out color))
            return false;

        return true;
    }

    private static bool StartsWithReservedColorPrefix(string s, int start, out bool isForegroundColor)
    {
        isForegroundColor = false;

        if (start < 0 || start + 2 >= s.Length)
            return false;

        ReadOnlySpan<char> remaining = s.AsSpan(start);
        if (remaining.StartsWith("FG:".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            isForegroundColor = true;
            return true;
        }

        if (remaining.StartsWith("BG:".AsSpan(), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool TryParseColor(string value, out Color color)
    {
        color = Color.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            color = ColorTranslator.FromHtml(value.Trim());
            return !color.IsEmpty;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadImageSpan(
        string s,
        int i,
        out int endExclusive,
        out string altText,
        out string source)
    {
        endExclusive = -1;
        altText = string.Empty;
        source = string.Empty;

        if (i < 0 || i + 4 >= s.Length)
            return false;

        if (s[i] != '!' || s[i + 1] != '[')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        if (StartsWithReservedColorPrefix(s, i + 2, out _))
            return false;

        int altEnd = FindUnescapedChar(s, ']', i + 2);
        if (altEnd < 0 || altEnd + 1 >= s.Length || s[altEnd + 1] != '(')
            return false;

        int sourceStart = altEnd + 2;
        int sourceEnd = FindImageSourceEnd(s, sourceStart);
        if (sourceEnd < 0)
            return false;

        string rawSource = s[sourceStart..sourceEnd].Trim();
        if (rawSource.Length >= 2 && rawSource[0] == '<' && rawSource[^1] == '>')
            rawSource = rawSource[1..^1].Trim();

        if (string.IsNullOrEmpty(rawSource))
            return false;

        altText = s[(i + 2)..altEnd];
        source = rawSource;
        endExclusive = sourceEnd + 1;
        return true;
    }

    private static bool TryReadFootnoteReference(
        string s,
        int i,
        out int labelStart,
        out int labelEnd,
        out int endExclusive,
        out string label)
    {
        labelStart = -1;
        labelEnd = -1;
        endExclusive = -1;
        label = string.Empty;

        if (i < 0 || i + 4 > s.Length || s[i] != '[' || s[i + 1] != '^')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        int closeBracket = FindUnescapedChar(s, ']', i + 2);
        if (closeBracket < 0)
            return false;

        label = s[(i + 2)..closeBracket].Trim();
        if (string.IsNullOrEmpty(label))
            return false;

        labelStart = i + 2;
        labelEnd = closeBracket;
        endExclusive = closeBracket + 1;
        return true;
    }

    private static bool TryReadLinkSpan(
        string s,
        int i,
        out int textStart,
        out int textEnd,
        out int endExclusive,
        out string linkText,
        out string linkTarget)
    {
        textStart = -1;
        textEnd = -1;
        endExclusive = -1;
        linkText = string.Empty;
        linkTarget = string.Empty;

        if (i < 0 || i + 4 >= s.Length || s[i] != '[')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        int closeBracket = FindUnescapedChar(s, ']', i + 1);
        if (closeBracket < 0 || closeBracket + 1 >= s.Length || s[closeBracket + 1] != '(')
            return false;

        int targetStart = closeBracket + 2;
        int targetEnd = FindImageSourceEnd(s, targetStart);
        if (targetEnd < 0)
            return false;

        string rawTarget = s[targetStart..targetEnd].Trim();
        if (rawTarget.Length >= 2 && rawTarget[0] == '<' && rawTarget[^1] == '>')
            rawTarget = rawTarget[1..^1].Trim();

        linkText = s[(i + 1)..closeBracket];
        if (string.IsNullOrEmpty(linkText) || string.IsNullOrEmpty(rawTarget))
            return false;

        textStart = i + 1;
        textEnd = closeBracket;
        endExclusive = targetEnd + 1;
        linkTarget = rawTarget;
        return true;
    }

    private static bool TryReadBareUrl(string s, int i, out int endExclusive, out string href)
    {
        endExclusive = -1;
        href = string.Empty;

        if (!StartsWithHttpScheme(s, i) || !CanStartBareUrl(s, i))
            return false;

        int end = i;
        while (end < s.Length && !char.IsWhiteSpace(s[end]) && s[end] is not '<' and not '>')
            end++;

        end = TrimBareUrlEnd(s, i, end);
        if (end <= i)
            return false;

        href = s[i..end];
        endExclusive = end;
        return true;
    }

    private static bool TryReadCodeSpan(string s, int i, out int markerLen, out int closeMarkerPos)
    {
        markerLen = 0;
        closeMarkerPos = -1;

        if (i < 0 || i >= s.Length || s[i] != '`')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        int run = CountRun(s, i, '`');

        // Only 1 or >=3
        if (!(run == 1 || run >= 3))
            return false;

        int scanStart = i + run;

        for (int p = scanStart; p < s.Length; p++)
        {
            if (s[p] != '`' || IsEscapedAt(s, p))
                continue;

            int closeRun = CountRun(s, p, '`');
            if (closeRun == run)
            {
                markerLen = run;
                closeMarkerPos = p;
                return true;
            }

            p += Math.Max(0, closeRun - 1);
        }

        return false;
    }

    private static bool IsEscapedAt(string s, int index)
    {
        int backslashes = 0;
        for (int i = index - 1; i >= 0 && s[i] == '\\'; i--)
            backslashes++;

        return (backslashes & 1) == 1;
    }

    private static bool TryReadMarker(
        string s,
        int i,
        InlineStyle currentStyle,
        out int markerLen,
        out InlineStyle toggleStyle)
    {
        markerLen = 0;
        toggleStyle = InlineStyle.None;

        if (i < 0 || i >= s.Length) return false;

        char ch = s[i];
        if (ch is not ('*' or '_' or '~')) return false;
        if (IsEscapedAt(s, i)) return false;

        int run = CountRun(s, i, ch);
        int scanStart = i + run; // don't look inside same contiguous marker run

        if (ch is '*' or '_')
        {
            // Closing preference first
            if (run >= 3)
            {
                if (HasStyle(currentStyle, InlineStyle.Bold | InlineStyle.Italic))
                {
                    markerLen = 3;
                    toggleStyle = InlineStyle.Bold | InlineStyle.Italic;
                    return true;
                }

                if (HasStyle(currentStyle, InlineStyle.Bold))
                {
                    markerLen = 2;
                    toggleStyle = InlineStyle.Bold;
                    return true;
                }

                if (HasStyle(currentStyle, InlineStyle.Italic))
                {
                    markerLen = 1;
                    toggleStyle = InlineStyle.Italic;
                    return true;
                }

                if (HasMatchingRunAhead(s, scanStart, ch, 3))
                {
                    markerLen = 3;
                    toggleStyle = InlineStyle.Bold | InlineStyle.Italic;
                    return true;
                }

                return false;
            }

            if (run == 2)
            {
                if (HasStyle(currentStyle, InlineStyle.Bold))
                {
                    markerLen = 2;
                    toggleStyle = InlineStyle.Bold;
                    return true;
                }

                if (HasStyle(currentStyle, InlineStyle.Italic))
                {
                    markerLen = 1;
                    toggleStyle = InlineStyle.Italic;
                    return true;
                }

                if (HasMatchingRunAhead(s, scanStart, ch, 2))
                {
                    markerLen = 2;
                    toggleStyle = InlineStyle.Bold;
                    return true;
                }

                return false;
            }

            // run == 1
            if (HasStyle(currentStyle, InlineStyle.Italic))
            {
                markerLen = 1;
                toggleStyle = InlineStyle.Italic;
                return true;
            }

            if (HasMatchingRunAhead(s, scanStart, ch, 1))
            {
                markerLen = 1;
                toggleStyle = InlineStyle.Italic;
                return true;
            }

            return false;
        }

        // ch == '~'
        if (run >= 2)
        {
            if (HasStyle(currentStyle, InlineStyle.Strike))
            {
                markerLen = 2;
                toggleStyle = InlineStyle.Strike;
                return true;
            }

            if (HasMatchingRunAhead(s, scanStart, '~', 2))
            {
                markerLen = 2;
                toggleStyle = InlineStyle.Strike;
                return true;
            }

            // Optional single ~ close if already in strike mode
            if (HasStyle(currentStyle, InlineStyle.Strike))
            {
                markerLen = 1;
                toggleStyle = InlineStyle.Strike;
                return true;
            }

            return false;
        }

        // run == 1 (single ~ support)
        if (HasStyle(currentStyle, InlineStyle.Strike))
        {
            markerLen = 1;
            toggleStyle = InlineStyle.Strike;
            return true;
        }

        if (HasMatchingRunAhead(s, scanStart, '~', 1))
        {
            markerLen = 1;
            toggleStyle = InlineStyle.Strike;
            return true;
        }

        return false;
    }

    private static bool HasStyle(InlineStyle current, InlineStyle needed)
        => (current & needed) == needed;

    private static bool StartsWithHttpScheme(string text, int index)
    {
        ReadOnlySpan<char> remaining = text.AsSpan(index);
        return remaining.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               remaining.StartsWith("https://".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanStartBareUrl(string text, int index)
    {
        if (index <= 0)
            return true;

        char previous = text[index - 1];
        return char.IsWhiteSpace(previous) || previous is '(' or '[' or '<' or '"' or '\'' or ':';
    }

    private static int TrimBareUrlEnd(string text, int start, int end)
    {
        while (end > start)
        {
            char last = text[end - 1];
            if (last is '.' or ',' or ';' or ':' or '!' or '?')
            {
                end--;
                continue;
            }

            if (last == ')' && HasMoreClosingParens(text, start, end))
            {
                end--;
                continue;
            }

            break;
        }

        return end;
    }

    private static int FindUnescapedChar(string text, char ch, int startIndex)
    {
        for (int i = Math.Max(0, startIndex); i < text.Length; i++)
        {
            if (text[i] != ch)
                continue;

            if (!IsEscapedAt(text, i))
                return i;
        }

        return -1;
    }

    private static bool HasMoreClosingParens(string text, int start, int end)
    {
        int opens = 0;
        int closes = 0;

        for (int i = start; i < end; i++)
        {
            if (text[i] == '(')
                opens++;
            else if (text[i] == ')')
                closes++;
        }

        return closes > opens;
    }

    private static int FindImageSourceEnd(string text, int startIndex)
    {
        bool inAngle = false;
        int depth = 0;

        for (int i = Math.Max(0, startIndex); i < text.Length; i++)
        {
            char ch = text[i];

            if (ch == '\\' && i + 1 < text.Length)
            {
                i++;
                continue;
            }

            if (!inAngle && ch == '<' && depth == 0)
            {
                inAngle = true;
                continue;
            }

            if (inAngle)
            {
                if (ch == '>')
                    inAngle = false;

                continue;
            }

            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch != ')')
                continue;

            if (depth == 0)
                return i;

            depth--;
        }

        return -1;
    }

    private static int CountRun(string s, int start, char ch)
    {
        int p = start;
        while (p < s.Length && s[p] == ch) p++;
        return p - start;
    }

    private static bool HasMatchingRunAhead(string s, int start, char ch, int neededLen)
    {
        for (int p = start; p < s.Length; p++)
        {
            if (s[p] == '\\')
            {
                // skip escaped next char if present
                if (p + 1 < s.Length) p++;
                continue;
            }

            if (s[p] != ch) continue;

            int run = CountRun(s, p, ch);
            if (run >= neededLen) return true;

            p += Math.Max(0, run - 1);
        }

        return false;
    }
}
