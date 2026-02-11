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

public readonly record struct InlineRun(string Text, InlineStyle Style);

public sealed record InlineParseResult(
    string Text,
    IReadOnlyList<InlineRun> Runs,
    int[] VisualToSource, // output vis-col -> input src-col
    int[] SourceToVisual  // input src-col -> output vis-col
);

public static class InlineMarkdown
{
    public static InlineParseResult Parse(string input)
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

        var runs = new List<InlineRun>();
        var sb = new StringBuilder();

        InlineStyle style = InlineStyle.None;

        int srcN = input.Length;
        var sourceToVisual = new int[srcN + 1];

        // visualToSource[v] = source index at visual caret position v
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
                visualToSource.Add(i + 2);
                sourceToVisual[i + 2] = outPos;

                i += 2;
                continue;
            }

            // Inline code: `code` oder ```code``` (1 oder >=3, gleicher Delimiter zum Schließen)
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
        visualToSource[^1] = srcN;
        sourceToVisual[srcN] = outPos;

        string text = string.Concat(runs.ConvertAll(r => r.Text));
        return new InlineParseResult(text, runs, visualToSource.ToArray(), sourceToVisual);
    }

    public static Font CreateStyledFont(Font baseFont, InlineStyle style)
    {
        FontStyle fs = baseFont.Style;

        if ((style & InlineStyle.Bold) != 0) fs |= FontStyle.Bold;
        if ((style & InlineStyle.Italic) != 0) fs |= FontStyle.Italic;
        if ((style & InlineStyle.Strike) != 0) fs |= FontStyle.Strikeout;

        // InlineStyle.Code steuert Rendering (Mono + Hintergrund), kein FontStyle-Flag
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
        => c is '*' or '_' or '~' or '\\' or '`';

    private static bool TryReadCodeSpan(string s, int i, out int markerLen, out int closeMarkerPos)
    {
        markerLen = 0;
        closeMarkerPos = -1;

        if (i < 0 || i >= s.Length || s[i] != '`')
            return false;

        if (IsEscapedAt(s, i))
            return false;

        int run = CountRun(s, i, '`');

        // Nur 1 oder >=3 zulassen
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

        char ch = s[i];
        if (ch is not ('*' or '_' or '~')) return false;

        int run = CountRun(s, i, ch);

        // Do not look inside the same contiguous marker run.
        int scanStart = i + run;

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
                p++; // skip escaped char
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
