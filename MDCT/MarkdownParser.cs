using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownGdi;

public static class MarkdownParser
{
    private static readonly Regex HeadingRegex = new(@"^\s*(#{1,6})\s+(.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex QuoteRegex = new(@"^\s*>\s?.*$", RegexOptions.Compiled);
    private static readonly Regex OrderedListRegex = new(@"^\s*\d+\.\s+.+$", RegexOptions.Compiled);
    private static readonly Regex UnorderedListRegex = new(@"^\s*[-+*]\s+.+$", RegexOptions.Compiled);
    private static readonly Regex FenceStartRegex = new(@"^\s*(```+|~~~+)\s*(.*)$", RegexOptions.Compiled);

    // Horizontal Rule: ---  ***  ___  (auch mit Spaces dazwischen)
    // Bis zu 3 führende Spaces wie bei Markdown üblich.
    private static readonly Regex HorizontalRuleRegex =
        new(@"^\s{0,3}(?:(?:-\s*){3,}|(?:\*\s*){3,}|(?:_\s*){3,})\s*$", RegexOptions.Compiled);

    // Gültige Delimiter-Zelle: ---  :---  ---:  :---: (mind. 3 Bindestriche)
    private static readonly Regex TableDelimiterCellRegex = new(@"^:?-{3,}:?$", RegexOptions.Compiled);

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
                blocks.Add(new QuoteBlock(start, i - 1));
                continue;
            }

            if (IsListLine(line))
            {
                int start = i;
                bool ordered = OrderedListRegex.IsMatch(lines[i]);

                while (i < lines.Count && IsListLine(lines[i]))
                    i++;

                blocks.Add(new ListBlock(start, i - 1, ordered));
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
        => OrderedListRegex.IsMatch(line) || UnorderedListRegex.IsMatch(line);

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
        // Unclosed fence => NICHT als CodeFenceBlock parsen,
        // damit nicht der Rest des Dokuments "im Codeblock hängt".
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

        // Danach muss direkt die Fence beginnen (kein Text davor)
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

        // Für Editor-UX: min. 2 Spalten (vermeidet frühe False-Positives)
        if (headerCells.Count < 2) return false;

        // Strikt: beide Zeilen müssen wie echte Tabellenzeilen aussehen
        // z.B. "| A | B |" und "| --- | --- |"
        if (!IsPipeBounded(headerLine) || !IsPipeBounded(delimiterLine))
            return false;

        var delimiterCells = ParseTableCells(delimiterLine);

        // Delimiter muss exakt die gleiche Spaltenanzahl wie Header haben
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
        return s.StartsWith("|") && s.EndsWith("|");
    }

    private static bool IsSpecialStart(IReadOnlyList<string> lines, int i)
    {
        string line = lines[i];

        if (string.IsNullOrWhiteSpace(line)) return true;
        if (HeadingRegex.IsMatch(line)) return true;
        if (IsHorizontalRule(line)) return true;
        if (QuoteRegex.IsMatch(line)) return true;
        if (IsListLine(line)) return true;
        if (IsPotentialTableStart(lines, i)) return true;

        // Fence nur dann als "special start", wenn später auch ein passendes Ende existiert.
        // Sonst bleibt es normaler Paragraph-Text (Editor-UX).
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


    // Splittet Zellen und unterstützt escaped pipes "\|"
    private static List<string> ParseTableCells(string line)
    {
        string s = line.Trim();

        if (s.StartsWith("|")) s = s[1..];
        if (s.EndsWith("|")) s = s[..^1];

        var cells = new List<string>();
        var sb = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            // escaped pipe: \|
            if (ch == '\\' && i + 1 < s.Length && s[i + 1] == '|')
            {
                sb.Append('|');
                i++; // nächstes Zeichen überspringen
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
}
