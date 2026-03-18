using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownGdi;

public static class MarkdownParser
{
    private static readonly Regex HeadingRegex = new(@"^\s*(#{1,6})\s+(.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex QuoteRegex = new(@"^\s*>\s?.*$", RegexOptions.Compiled);
    private static readonly Regex QuoteContentRegex = new(@"^\s*>\s?(.*)$", RegexOptions.Compiled);
    private static readonly Regex FenceStartRegex = new(@"^\s*(```+|~~~+)\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex FootnoteDefinitionRegex = new(
        @"^\s{0,3}\[\^(?<label>[^\]]+)\]:(?<ws>[ \t]*)(?<text>.*)$",
        RegexOptions.Compiled);

    // [!NOTE], [!TIP], [!IMPORTANT], [!WARNING], [!CAUTION]
    private static readonly Regex AdmonitionMarkerRegex =
        new(@"^\[\!(NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Horizontal Rule: ---  ***  ___  (auch mit Spaces dazwischen)
    // Bis zu 3 führende Spaces wie bei Markdown üblich.
    private static readonly Regex HorizontalRuleRegex =
        new(@"^\s{0,3}(?:(?:-\s*){3,}|(?:\*\s*){3,}|(?:_\s*){3,})\s*$", RegexOptions.Compiled);

    // Gültige Delimiter-Zelle: ---  :---  ---:  :---: (mind. 3 Bindestriche)
    private static readonly Regex TableDelimiterCellRegex = new(@"^:?-{3,}:?$", RegexOptions.Compiled);

    // List line:
    // indent + (unordered marker OR ordered number.) + at least one whitespace + text (can be empty/space)
    // Beispiele:
    // "- a", "  * b", "    + c", "1. x", "  42. y"
    private static readonly Regex ListLineRegex = new(
        @"^(?<indent>[ \t]*)(?:(?<ul>[-+*])|(?<num>\d+)\.)(?<ws>[ \t]+)(?<text>.*)$",
        RegexOptions.Compiled);

    // Task marker direkt am Anfang des Item-Texts:
    // [ ] , [x], [X]
    // Erlaubt:
    //  - exakt Marker am Zeilenende
    //  - Marker + mindestens ein Whitespace + Resttext
    // Nicht erlaubt:
    //  - "[x]Text" (ohne Whitespace)
    private static readonly Regex TaskPrefixRegex = new(
        @"^\[(?<state>[ xX])\](?:(?<ws>[ \t]+)(?<rest>.*)|(?<rest>))$",
        RegexOptions.Compiled);

    public static IReadOnlyList<MarkdownBlock> Parse(IReadOnlyList<string> lines)
    {
        var blocks = new List<MarkdownBlock>();
        int i = 0;

        while (i < lines.Count)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                blocks.Add(new BlankBlock(i));
                i++;
                continue;
            }

            if (TryParseFencedCode(lines, ref i, out var code))
            {
                blocks.Add(code);
                continue;
            }

            if (TryParseTable(lines, ref i, out var table))
            {
                blocks.Add(table);
                continue;
            }

            if (TryParseStandaloneImage(lines[i], out string altText, out string source))
            {
                blocks.Add(new ImageBlock(i, altText, source));
                i++;
                continue;
            }

            if (TryParseFootnoteDefinition(lines, ref i, out var footnote))
            {
                blocks.Add(footnote);
                continue;
            }

            var hm = HeadingRegex.Match(line);
            if (hm.Success)
            {
                int level = hm.Groups[1].Value.Length;
                string text = hm.Groups[2].Value;
                blocks.Add(new HeadingBlock(i, level, text));
                i++;
                continue;
            }

            // Horizontal Rule (---, ***, ___)
            if (IsHorizontalRule(line))
            {
                string marker = ExtractHrMarker(line);
                blocks.Add(new HorizontalRuleBlock(i, marker));
                i++;
                continue;
            }

            if (QuoteRegex.IsMatch(line))
            {
                int start = i;
                while (i < lines.Count && QuoteRegex.IsMatch(lines[i])) i++;
                int end = i - 1;

                var (admonition, markerLine, markerText) = DetectQuoteAdmonition(lines, start, end);
                blocks.Add(new QuoteBlock(
                    StartLine: start,
                    EndLine: end,
                    Admonition: admonition,
                    AdmonitionMarkerLine: markerLine,
                    AdmonitionMarkerText: markerText));

                continue;
            }

            if (IsListLine(line))
            {
                int start = i;
                var listLines = new List<(int SourceLine, string Text)>();

                while (i < lines.Count && IsListLine(lines[i]))
                {
                    listLines.Add((i, lines[i]));
                    i++;
                }

                var items = ParseListItems(listLines);
                bool isOrderedTopLevel = AreAllTopLevelItemsOrdered(items);

                blocks.Add(new ListBlock(start, i - 1, items, isOrderedTopLevel));
                continue;
            }

            int pStart = i;
            while (i < lines.Count &&
                   !string.IsNullOrWhiteSpace(lines[i]) &&
                   !IsSpecialStart(lines, i))
            {
                i++;
            }

            if (i == pStart) i++;
            blocks.Add(new ParagraphBlock(pStart, i - 1));
        }

        return blocks;
    }

    private static bool IsListLine(string line)
        => ListLineRegex.IsMatch(line);

    private static List<ListItem> ParseListItems(IReadOnlyList<(int SourceLine, string Text)> listLines)
    {
        var result = new List<ListItem>(listLines.Count);

        // Stack enthält Indent-Werte, die die aktuelle Hierarchie repräsentieren.
        // Beispiel Indents: 0,2,4 => Levels 0,1,2
        var indentStack = new List<int>();

        foreach (var (sourceLine, raw) in listLines)
        {
            if (!TryParseListLine(raw, out var parsed))
                continue; // defensive; sollte wegen Vorfilter nicht passieren

            int indent = parsed.Indent;

            // Solange wir nicht tiefer sind, auf passende Parent-Ebene zurück.
            while (indentStack.Count > 0 && indent <= indentStack[^1])
                indentStack.RemoveAt(indentStack.Count - 1);

            int level = indentStack.Count;
            indentStack.Add(indent);

            result.Add(new ListItem(
                SourceLine: sourceLine,
                Indent: indent,
                Level: level,
                MarkerKind: parsed.MarkerKind,
                UnorderedMarker: parsed.UnorderedMarker,
                OrderedNumber: parsed.OrderedNumber,
                Text: parsed.Text,
                IsTask: parsed.IsTask,
                IsChecked: parsed.IsChecked,
                TaskMarkerStartColumn: parsed.TaskMarkerStartColumn,
                TaskMarkerLength: parsed.TaskMarkerLength,
                ContentStartColumn: parsed.ContentStartColumn));
        }

        return result;
    }

    private static bool AreAllTopLevelItemsOrdered(IReadOnlyList<ListItem> items)
    {
        bool hasTop = false;
        foreach (var it in items)
        {
            if (it.Level != 0) continue;
            hasTop = true;
            if (it.MarkerKind != ListMarkerKind.Ordered)
                return false;
        }

        return hasTop;
    }

    private readonly record struct ParsedListLine(
        int Indent,
        ListMarkerKind MarkerKind,
        char? UnorderedMarker,
        int? OrderedNumber,
        string Text,
        bool IsTask,
        bool IsChecked,
        int TaskMarkerStartColumn,
        int TaskMarkerLength,
        int ContentStartColumn);

    private readonly record struct ParsedTaskPrefix(
        bool IsChecked,
        string RestText,
        int MarkerLength,
        int WsAfterLength);

    private static bool TryParseTaskPrefix(string text, out ParsedTaskPrefix parsed)
    {
        parsed = default;

        if (text is null)
            return false;

        var m = TaskPrefixRegex.Match(text);
        if (!m.Success)
            return false;

        char state = m.Groups["state"].Value[0];
        bool isChecked = state == 'x' || state == 'X';

        int wsLen = m.Groups["ws"].Success ? m.Groups["ws"].Length : 0;
        string rest = m.Groups["rest"].Success ? m.Groups["rest"].Value : string.Empty;

        parsed = new ParsedTaskPrefix(
            IsChecked: isChecked,
            RestText: rest,
            MarkerLength: 3, // [ ] / [x] / [X]
            WsAfterLength: wsLen);

        return true;
    }

    private static bool TryParseListLine(string line, out ParsedListLine parsed)
    {
        parsed = default;

        var m = ListLineRegex.Match(line);
        if (!m.Success) return false;

        string indentRaw = m.Groups["indent"].Value;
        int indent = NormalizeIndent(indentRaw);

        Group ul = m.Groups["ul"];
        Group num = m.Groups["num"];

        Group textGroup = m.Groups["text"];
        string textRaw = textGroup.Value;

        int textStartColumn = textGroup.Index;
        int contentStartColumn = textStartColumn;

        bool isTask = false;
        bool isChecked = false;
        int taskMarkerStartColumn = -1;
        int taskMarkerLength = 0;

        string finalText = textRaw;

        if (TryParseTaskPrefix(textRaw, out var task))
        {
            isTask = true;
            isChecked = task.IsChecked;
            taskMarkerStartColumn = textStartColumn;
            taskMarkerLength = task.MarkerLength;
            contentStartColumn = textStartColumn + task.MarkerLength + task.WsAfterLength;
            finalText = task.RestText;
        }

        if (ul.Success)
        {
            char marker = ul.Value[0];
            parsed = new ParsedListLine(
                Indent: indent,
                MarkerKind: ListMarkerKind.Unordered,
                UnorderedMarker: marker,
                OrderedNumber: null,
                Text: finalText,
                IsTask: isTask,
                IsChecked: isChecked,
                TaskMarkerStartColumn: taskMarkerStartColumn,
                TaskMarkerLength: taskMarkerLength,
                ContentStartColumn: contentStartColumn);
            return true;
        }

        if (num.Success && int.TryParse(num.Value, out int n))
        {
            parsed = new ParsedListLine(
                Indent: indent,
                MarkerKind: ListMarkerKind.Ordered,
                UnorderedMarker: null,
                OrderedNumber: n,
                Text: finalText,
                IsTask: isTask,
                IsChecked: isChecked,
                TaskMarkerStartColumn: taskMarkerStartColumn,
                TaskMarkerLength: taskMarkerLength,
                ContentStartColumn: contentStartColumn);
            return true;
        }

        return false;
    }

    // Tabs werden für Einrückung auf 4 Spaces normalisiert.
    private static int NormalizeIndent(string indent)
    {
        int count = 0;
        foreach (char ch in indent)
        {
            if (ch == '\t') count += 4;
            else if (ch == ' ') count++;
        }
        return count;
    }

    private static bool IsHorizontalRule(string line)
        => HorizontalRuleRegex.IsMatch(line);

    private static string ExtractHrMarker(string line)
    {
        foreach (char ch in line)
        {
            if (ch is '-' or '*' or '_')
                return ch.ToString();
        }

        // Fallback (sollte bei gültiger HR nicht auftreten)
        return "-";
    }

    private static bool HasFenceCloseAhead(IReadOnlyList<string> lines, int startIndex, char fenceChar, int minFenceLen)
    {
        for (int j = startIndex; j < lines.Count; j++)
        {
            if (IsFenceClose(lines[j], fenceChar, minFenceLen))
                return true;
        }

        return false;
    }

    private static bool TryParseFencedCode(IReadOnlyList<string> lines, ref int i, out CodeFenceBlock block)
    {
        block = null!;

        if (i < 0 || i >= lines.Count) return false;

        var m = FenceStartRegex.Match(lines[i]);
        if (!m.Success) return false;

        string fenceToken = m.Groups[1].Value;   // ``` oder ~~~ (auch länger)
        string language = m.Groups[2].Value.Trim();

        char fenceChar = fenceToken[0];
        int minFenceLen = fenceToken.Length;

        int start = i;
        int close = -1;

        for (int j = i + 1; j < lines.Count; j++)
        {
            if (IsFenceClose(lines[j], fenceChar, minFenceLen))
            {
                close = j;
                break;
            }
        }

        // Editor-friendly:
        // Unclosed fence => NICHT als CodeFenceBlock parsen.
        if (close < 0)
            return false;

        block = new CodeFenceBlock(start, close, fenceToken, language);
        i = close + 1;
        return true;
    }

    private static bool IsFenceClose(string line, char fenceChar, int minFenceLen)
    {
        int i = 0;

        // Markdown: bis zu 3 führende Spaces erlauben
        int leadingSpaces = 0;
        while (i < line.Length && line[i] == ' ' && leadingSpaces < 3)
        {
            i++;
            leadingSpaces++;
        }

        // Danach muss direkt die Fence beginnen
        int count = 0;
        while (i < line.Length && line[i] == fenceChar)
        {
            count++;
            i++;
        }

        if (count < minFenceLen) return false;

        // Danach nur noch Whitespace
        while (i < line.Length)
        {
            if (!char.IsWhiteSpace(line[i])) return false;
            i++;
        }

        return true;
    }

    // ---------- TABLES (strict) ----------

    private static bool IsPotentialTableStart(IReadOnlyList<string> lines, int i)
    {
        if (i + 1 >= lines.Count) return false;

        return TryParseTableHeaderAndDelimiter(
            lines[i],
            lines[i + 1],
            out _,
            out _);
    }

    private static bool TryParseTable(IReadOnlyList<string> lines, ref int i, out TableBlock block)
    {
        block = null!;

        if (i + 1 >= lines.Count) return false;

        string headerLine = lines[i];
        string delimiterLine = lines[i + 1];

        if (!TryParseTableHeaderAndDelimiter(headerLine, delimiterLine, out var headerCells, out var alignments))
            return false;

        int expectedCols = headerCells.Count;

        var rows = new List<TableRow>
        {
            new(i, headerCells)
        };

        int j = i + 2;
        while (j < lines.Count && IsLikelyTableBodyRow(lines[j], expectedCols))
        {
            rows.Add(new TableRow(j, ParseTableCells(lines[j])));
            j++;
        }

        // enthält mindestens Header + Delimiter
        int end = Math.Max(i + 1, j - 1);

        block = new TableBlock(i, end, rows, alignments);
        i = j;
        return true;
    }

    private static bool TryParseTableHeaderAndDelimiter(
        string headerLine,
        string delimiterLine,
        out List<string> headerCells,
        out List<TableAlignment> alignments)
    {
        headerCells = ParseTableCells(headerLine);
        alignments = new List<TableAlignment>();

        // Für Editor-UX: min. 2 Spalten
        if (headerCells.Count < 2) return false;

        // Strikt: beide Zeilen müssen echt table-like sein
        if (!IsPipeBounded(headerLine) || !IsPipeBounded(delimiterLine))
            return false;

        var delimiterCells = ParseTableCells(delimiterLine);

        if (delimiterCells.Count != headerCells.Count)
            return false;

        foreach (string raw in delimiterCells)
        {
            string cell = raw.Trim();

            if (!TableDelimiterCellRegex.IsMatch(cell))
                return false;

            bool left = cell.StartsWith(":");
            bool right = cell.EndsWith(":");

            if (left && right) alignments.Add(TableAlignment.Center);
            else if (left) alignments.Add(TableAlignment.Left);
            else if (right) alignments.Add(TableAlignment.Right);
            else alignments.Add(TableAlignment.None);
        }

        return true;
    }

    private static bool IsLikelyTableBodyRow(string line, int expectedCols)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        if (!IsPipeBounded(line)) return false;

        var cells = ParseTableCells(line);
        return cells.Count == expectedCols;
    }

    private static bool IsPipeBounded(string line)
    {
        string s = line.Trim();
        return s.StartsWith("|", StringComparison.Ordinal) && s.EndsWith("|", StringComparison.Ordinal);
    }

    private static bool TryParseStandaloneImage(string line, out string altText, out string source)
    {
        altText = string.Empty;
        source = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        string s = line.Trim();
        if (!s.StartsWith("![", StringComparison.Ordinal) || !s.EndsWith(")", StringComparison.Ordinal))
            return false;

        if (StartsWithReservedColorPrefix(s, 2))
            return false;

        int altEnd = FindUnescapedChar(s, ']', 2);
        if (altEnd < 0 || altEnd + 1 >= s.Length || s[altEnd + 1] != '(')
            return false;

        string rawSource = s[(altEnd + 2)..^1].Trim();
        if (string.IsNullOrEmpty(rawSource))
            return false;

        if (rawSource.Length >= 2 && rawSource[0] == '<' && rawSource[^1] == '>')
            rawSource = rawSource[1..^1].Trim();

        if (string.IsNullOrEmpty(rawSource))
            return false;

        altText = s[2..altEnd];
        source = rawSource;
        return true;
    }

    private static bool StartsWithReservedColorPrefix(string text, int start)
    {
        if (start < 0 || start + 2 >= text.Length)
            return false;

        ReadOnlySpan<char> remaining = text.AsSpan(start);
        return remaining.StartsWith("FG:".AsSpan(), StringComparison.OrdinalIgnoreCase)
            || remaining.StartsWith("BG:".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static int FindUnescapedChar(string text, char ch, int startIndex)
    {
        for (int i = Math.Max(0, startIndex); i < text.Length; i++)
        {
            if (text[i] != ch)
                continue;

            int backslashes = 0;
            for (int j = i - 1; j >= 0 && text[j] == '\\'; j--)
                backslashes++;

            if ((backslashes & 1) == 0)
                return i;
        }

        return -1;
    }

    private static bool IsSpecialStart(IReadOnlyList<string> lines, int i)
    {
        string line = lines[i];

        if (string.IsNullOrWhiteSpace(line)) return true;
        if (FootnoteDefinitionRegex.IsMatch(line)) return true;
        if (HeadingRegex.IsMatch(line)) return true;
        if (TryParseStandaloneImage(line, out _, out _)) return true;
        if (IsHorizontalRule(line)) return true;
        if (QuoteRegex.IsMatch(line)) return true;
        if (IsListLine(line)) return true;
        if (IsPotentialTableStart(lines, i)) return true;

        // Fence nur dann special, wenn später ein Ende existiert.
        var m = FenceStartRegex.Match(line);
        if (m.Success)
        {
            string token = m.Groups[1].Value;
            char fenceChar = token[0];
            int minFenceLen = token.Length;

            if (HasFenceCloseAhead(lines, i + 1, fenceChar, minFenceLen))
                return true;
        }

        return false;
    }

    private static bool TryParseFootnoteDefinition(IReadOnlyList<string> lines, ref int i, out FootnoteDefinitionBlock block)
    {
        block = null!;

        if (i < 0 || i >= lines.Count)
            return false;

        Match firstMatch = FootnoteDefinitionRegex.Match(lines[i]);
        if (!firstMatch.Success)
            return false;

        string label = firstMatch.Groups["label"].Value.Trim();
        string normalizedLabel = MarkdownFootnoteHelper.NormalizeFootnoteLabel(label);
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(normalizedLabel))
            return false;

        int startLine = i;
        var definitionLines = new List<FootnoteDefinitionLine>
        {
            new(
                SourceLine: i,
                MarkerStartColumn: Math.Max(0, firstMatch.Groups["label"].Index - 2),
                CaretColumn: Math.Max(0, firstMatch.Groups["label"].Index - 1),
                ContentStartColumn: Math.Clamp(firstMatch.Groups["text"].Index, 0, lines[i].Length),
                IsFirstLine: true)
        };

        i++;

        while (i < lines.Count)
        {
            if (TryGetFootnoteContinuationContentStart(lines[i], out int contentStart))
            {
                definitionLines.Add(new FootnoteDefinitionLine(
                    SourceLine: i,
                    MarkerStartColumn: 0,
                    CaretColumn: -1,
                    ContentStartColumn: contentStart,
                    IsFirstLine: false));
                i++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lines[i]))
                break;

            int scan = i;
            while (scan < lines.Count && string.IsNullOrWhiteSpace(lines[scan]))
                scan++;

            if (scan >= lines.Count || !TryGetFootnoteContinuationContentStart(lines[scan], out _))
                break;

            while (i < scan)
            {
                definitionLines.Add(new FootnoteDefinitionLine(
                    SourceLine: i,
                    MarkerStartColumn: 0,
                    CaretColumn: -1,
                    ContentStartColumn: lines[i].Length,
                    IsFirstLine: false));
                i++;
            }
        }

        int endLine = definitionLines[^1].SourceLine;
        block = new FootnoteDefinitionBlock(startLine, endLine, label, normalizedLabel, definitionLines);
        return true;
    }

    private static bool TryGetFootnoteContinuationContentStart(string line, out int contentStart)
    {
        contentStart = 0;

        if (string.IsNullOrEmpty(line))
        {
            contentStart = 0;
            return false;
        }

        if (line[0] == '\t')
        {
            contentStart = 1;
            while (contentStart < line.Length && char.IsWhiteSpace(line[contentStart]))
                contentStart++;

            return true;
        }

        int spaces = 0;
        while (spaces < line.Length && line[spaces] == ' ')
            spaces++;

        if (spaces < 4)
            return false;

        contentStart = spaces;
        while (contentStart < line.Length && line[contentStart] == ' ')
            contentStart++;

        return true;
    }

    // Splittet Zellen und unterstützt escaped pipes "\|"
    private static List<string> ParseTableCells(string line)
    {
        string s = line.Trim();

        if (s.StartsWith("|", StringComparison.Ordinal)) s = s[1..];
        if (s.EndsWith("|", StringComparison.Ordinal)) s = s[..^1];

        var cells = new List<string>();
        var sb = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            // escaped pipe: \|
            if (ch == '\\' && i + 1 < s.Length && s[i + 1] == '|')
            {
                sb.Append('|');
                i++;
                continue;
            }

            if (ch == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        cells.Add(sb.ToString().Trim());
        return cells;
    }

    // ---------- QUOTE ADMONITIONS ----------

    private static (AdmonitionKind Kind, int MarkerLine, string MarkerText) DetectQuoteAdmonition(
        IReadOnlyList<string> lines,
        int start,
        int end)
    {
        for (int lineIndex = start; lineIndex <= end; lineIndex++)
        {
            if (!TryGetQuoteContent(lines[lineIndex], out string content))
                continue;

            if (string.IsNullOrWhiteSpace(content))
                continue; // leere Quote-Zeilen überspringen

            string trimmed = content.Trim();

            if (TryParseAdmonitionMarker(trimmed, out var kind))
                return (kind, lineIndex, trimmed);

            // Erste inhaltliche Zeile ist kein Marker => kein Admonition-Block
            break;
        }

        return (AdmonitionKind.None, -1, string.Empty);
    }

    private static bool TryGetQuoteContent(string line, out string content)
    {
        content = string.Empty;
        var m = QuoteContentRegex.Match(line);
        if (!m.Success) return false;

        content = m.Groups[1].Value;
        return true;
    }

    private static bool TryParseAdmonitionMarker(string s, out AdmonitionKind kind)
    {
        kind = AdmonitionKind.None;

        var m = AdmonitionMarkerRegex.Match(s);
        if (!m.Success) return false;

        string token = m.Groups[1].Value.ToUpperInvariant();
        kind = token switch
        {
            "NOTE" => AdmonitionKind.Note,
            "TIP" => AdmonitionKind.Tip,
            "IMPORTANT" => AdmonitionKind.Important,
            "WARNING" => AdmonitionKind.Warning,
            "CAUTION" => AdmonitionKind.Caution,
            _ => AdmonitionKind.None
        };

        return kind != AdmonitionKind.None;
    }
}
