using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MarkdownGdi;

public enum LineFontRole
{
    Base,
    Bold,
    Mono
}

public sealed class LayoutLine
{
    public required int SourceLine { get; init; }
    public required string SourceText { get; init; }
    public required VisualProjection Projection { get; init; }
    public required Rectangle Bounds { get; init; }
    public required int TextX { get; init; }
    public required int TextWidth { get; init; }
    public required MarkdownBlockKind Kind { get; init; }
    public required int HeadingLevel { get; init; }
    public required LineFontRole FontRole { get; init; }

    // Bereits inline-geparste Runs (ohne Markdown-Marker)
    public required IReadOnlyList<InlineRun> InlineRuns { get; init; }
}

public readonly record struct TableHit(TableLayout Table, int Row, int Col, Rectangle CellBounds);

public sealed class TableLayout
{
    private readonly Rectangle[,] _cellRects;
    private readonly string[,] _cellTexts;
    private readonly IReadOnlyList<InlineRun>[,] _cellRuns;
    private readonly TableAlignment[] _colAlignments;

    public int StartLine { get; }
    public int EndLine { get; }
    public int Rows { get; }
    public int Cols { get; }
    public Rectangle Bounds { get; }
    public TableBlock Block { get; }

    public TableLayout(
        int startLine,
        int endLine,
        int rows,
        int cols,
        Rectangle bounds,
        TableBlock block,
        Rectangle[,] cellRects,
        string[,] cellTexts,
        IReadOnlyList<InlineRun>[,] cellRuns,
        TableAlignment[] colAlignments)
    {
        StartLine = startLine;
        EndLine = endLine;
        Rows = rows;
        Cols = cols;
        Bounds = bounds;
        Block = block;
        _cellRects = cellRects;
        _cellTexts = cellTexts;
        _cellRuns = cellRuns;
        _colAlignments = colAlignments;
    }

    public Rectangle GetCellRect(int row, int col) => _cellRects[row, col];
    public string GetCellText(int row, int col) => _cellTexts[row, col];
    public IReadOnlyList<InlineRun> GetCellRuns(int row, int col) => _cellRuns[row, col];

    public TableAlignment GetColumnAlignment(int col)
        => (col >= 0 && col < _colAlignments.Length) ? _colAlignments[col] : default;

    public IEnumerable<(int Row, int Col, Rectangle Rect, string Text)> EnumerateCells()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                yield return (r, c, _cellRects[r, c], _cellTexts[r, c]);
    }
}

public sealed class LayoutEngine
{
    private readonly List<LayoutLine> _lines = new();
    private readonly Dictionary<int, LayoutLine> _lineBySource = new();
    private readonly List<TableLayout> _tables = new();
    private readonly HashSet<int> _tableSourceLines = new();

    private Font _baseFont = SystemFonts.DefaultFont;
    private Font _boldFont = SystemFonts.DefaultFont;
    private Font _monoFont = SystemFonts.DefaultFont;

    private int _sourceLineCount;

    public Size ContentSize { get; private set; } = new(1, 1);

    private static readonly TextFormatFlags MeasureFlags =
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

    private readonly record struct CodeFenceSpan(
        int StartLine,
        int EndLine,
        int StartMarkerCol,
        int StartMarkerLen,
        int EndMarkerCol,
        int EndMarkerLen,
        char FenceChar);

    private readonly record struct FenceMarker(int Col, int Len, char Char);

    private const int TableCodeChipPadX = 4;

    private int MeasureInlineRunsWidthForTableLayout(IReadOnlyList<InlineRun> runs, Font baseFont)
    {
        if (runs.Count == 0) return 0;

        int width = 0;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                InlineStyle normalized = run.Style & ~InlineStyle.Code;

                int key = ((int)normalized & 0xFF)
                          | (isCode ? 0x100 : 0)
                          | (((int)baseFont.Style & 0xFF) << 9);

                if (!cache.TryGetValue(key, out var f))
                {
                    Font seed = isCode ? _monoFont : baseFont;
                    f = InlineMarkdown.CreateStyledFont(seed, normalized);
                    cache[key] = f;
                }

                int w = MeasureWidth(run.Text, f);
                if (isCode) w += TableCodeChipPadX * 2;

                width += w;
            }
        }
        finally
        {
            foreach (var f in cache.Values)
                f.Dispose();
        }

        return width;
    }

    // ------------------------------------------------------------
    // Fence-Erkennung:
    // - Backtick: 1 oder >=3
    // - Tilde: >=3
    // ------------------------------------------------------------

    private static bool IsValidFenceOpenerLen(char ch, int len)
    {
        if (ch == '`')
            return len == 1 || len >= 3;

        if (ch == '~')
            return len >= 3;

        return false;
    }

    private static bool IsValidFenceCloserLen(char ch, int openLen, int closeLen)
    {
        if (ch == '`')
        {
            if (!(closeLen == 1 || closeLen >= 3))
                return false;

            return closeLen >= openLen;
        }

        if (ch == '~')
        {
            if (closeLen < 3)
                return false;

            return closeLen >= openLen;
        }

        return false;
    }

    private static bool IsEscaped(string s, int index)
    {
        int backslashes = 0;
        for (int i = index - 1; i >= 0 && s[i] == '\\'; i--)
            backslashes++;

        return (backslashes & 1) == 1;
    }

    private static bool IsWhitespaceTail(string line, int startIndex)
    {
        for (int i = startIndex; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
                return false;
        }

        return true;
    }

    private static bool TryFindFenceOpener(string line, out FenceMarker marker)
    {
        marker = default;

        if (string.IsNullOrEmpty(line))
            return false;

        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        if (i >= line.Length) return false;

        char ch = line[i];
        if (ch != '`' && ch != '~') return false;
        if (IsEscaped(line, i)) return false;

        int j = i;
        while (j < line.Length && line[j] == ch) j++;

        int len = j - i;
        if (!IsValidFenceOpenerLen(ch, len))
            return false;

        marker = new FenceMarker(i, len, ch);
        return true;
    }

    private static bool TryFindFenceCloser(
        string line,
        char expectedChar,
        int openLen,
        int fromIndex,
        out FenceMarker close)
    {
        close = default;

        if (string.IsNullOrEmpty(line))
            return false;

        int from = Math.Clamp(fromIndex, 0, line.Length);

        for (int i = from; i < line.Length; i++)
        {
            if (line[i] != expectedChar || IsEscaped(line, i))
                continue;

            int j = i;
            while (j < line.Length && line[j] == expectedChar) j++;

            int len = j - i;
            if (!IsValidFenceCloserLen(expectedChar, openLen, len))
            {
                i = j - 1;
                continue;
            }

            if (IsWhitespaceTail(line, j))
            {
                close = new FenceMarker(i, len, expectedChar);
                return true;
            }

            i = j - 1;
        }

        return false;
    }

    private static List<CodeFenceSpan> BuildCodeFenceSpans(DocumentModel doc)
    {
        var spans = new List<CodeFenceSpan>();
        int n = doc.LineCount;
        int line = 0;

        while (line < n)
        {
            string s = doc.GetLine(line);

            if (!TryFindFenceOpener(s, out var open))
            {
                line++;
                continue;
            }

            int endLine = n - 1;
            int endCol = -1;
            int endLen = 0;
            bool closed = false;

            if (TryFindFenceCloser(s, open.Char, open.Len, open.Col + open.Len, out var sameClose))
            {
                endLine = line;
                endCol = sameClose.Col;
                endLen = sameClose.Len;
                closed = true;
            }
            else
            {
                for (int j = line + 1; j < n; j++)
                {
                    if (TryFindFenceCloser(doc.GetLine(j), open.Char, open.Len, 0, out var close))
                    {
                        endLine = j;
                        endCol = close.Col;
                        endLen = close.Len;
                        closed = true;
                        break;
                    }
                }
            }

            spans.Add(new CodeFenceSpan(
                StartLine: line,
                EndLine: endLine,
                StartMarkerCol: open.Col,
                StartMarkerLen: open.Len,
                EndMarkerCol: endCol,
                EndMarkerLen: endLen,
                FenceChar: open.Char));

            line = closed ? endLine + 1 : n;
        }

        return spans;
    }

    private static VisualProjection BuildProjectionHidingRanges(
        string source,
        IReadOnlyList<(int Start, int Length)> ranges)
    {
        source ??= string.Empty;
        int n = source.Length;

        if (n == 0 || ranges.Count == 0)
            return CreateIdentityProjection(source);

        var normalized = ranges
            .Where(r => r.Length > 0)
            .Select(r =>
            {
                int s = Math.Clamp(r.Start, 0, n);
                int e = Math.Clamp(r.Start + r.Length, 0, n);
                return (Start: s, End: e);
            })
            .Where(r => r.End > r.Start)
            .OrderBy(r => r.Start)
            .ToList();

        if (normalized.Count == 0)
            return CreateIdentityProjection(source);

        bool[] hide = new bool[n];
        foreach (var r in normalized)
            for (int k = r.Start; k < r.End; k++)
                hide[k] = true;

        var sb = new System.Text.StringBuilder(n);
        int[] s2v = new int[n + 1];
        var v2s = new List<int>(n + 1) { 0 };

        int v = 0;
        for (int s = 0; s < n; s++)
        {
            s2v[s] = v;

            if (hide[s])
                continue;

            sb.Append(source[s]);
            v++;
            v2s.Add(s + 1);
        }

        s2v[n] = v;

        if (v2s.Count == 0)
            v2s.Add(n);
        else
            v2s[v2s.Count - 1] = n;

        return VisualProjection.Create(sb.ToString(), v2s.ToArray(), s2v);
    }

    private static VisualProjection CreateIdentityProjection(string source)
    {
        source ??= string.Empty;
        int n = source.Length;

        int[] v2s = new int[n + 1];
        int[] s2v = new int[n + 1];

        for (int i = 0; i <= n; i++)
        {
            v2s[i] = i;
            s2v[i] = i;
        }

        return VisualProjection.Create(source, v2s, s2v);
    }

    /// <summary>
    /// forceRawTableStarts: table start-lines that should be rendered as raw source (not as grid).
    /// forceRawCodeFenceStarts: fence start-lines that should be rendered raw (show full source including markers).
    /// forceRawLines: source-lines that must be rendered raw (identity projection, no inline parsing, no marker-hiding).
    /// forceRawInlineLines: source-lines where ONLY inline markdown must stay visible as source
    ///                    (no InlineMarkdown.Parse, aber block-prefix Projektion darf bleiben).
    /// </summary>
    public void Rebuild(
        DocumentModel doc,
        Size viewport,
        Font baseFont,
        Font boldFont,
        Font monoFont,
        IReadOnlySet<int>? forceRawTableStarts = null,
        IReadOnlySet<int>? forceRawCodeFenceStarts = null,
        IReadOnlySet<int>? forceRawLines = null,
        IReadOnlySet<int>? forceRawInlineLines = null)
    {
        _sourceLineCount = doc.LineCount;

        _baseFont = baseFont;
        _boldFont = boldFont;
        _monoFont = monoFont;

        _lines.Clear();
        _lineBySource.Clear();
        _tables.Clear();
        _tableSourceLines.Clear();

        if (doc.LineCount == 0)
        {
            ContentSize = new Size(Math.Max(1, viewport.Width), Math.Max(1, viewport.Height));
            return;
        }

        var semantics = BuildSemantics(doc);

        // Mapping SourceLine -> ListItem (für ordered/unordered/nested Rendering)
        var listItemByLine = new Dictionary<int, ListItem>();
        foreach (var lb in doc.Blocks.OfType<ListBlock>())
        {
            foreach (var it in lb.Items)
                listItemByLine[it.SourceLine] = it;
        }

        var tableStarts = new Dictionary<int, TableBlock>();
        foreach (var t in doc.Blocks.OfType<TableBlock>())
        {
            bool forceRaw = forceRawTableStarts?.Contains(t.StartLine) == true;
            if (forceRaw) continue;

            tableStarts[t.StartLine] = t;

            for (int i = t.StartLine; i <= t.EndLine; i++)
                _tableSourceLines.Add(i);
        }

        var fenceSpans = BuildCodeFenceSpans(doc);
        int[] fenceSpanByLine = Enumerable.Repeat(-1, doc.LineCount).ToArray();

        // Parser-CodeFence neutralisieren (eigene Span-Logik übernimmt)
        for (int i = 0; i < doc.LineCount; i++)
        {
            if (semantics[i].Kind == MarkdownBlockKind.CodeFence)
            {
                semantics[i].Kind = string.IsNullOrWhiteSpace(doc.GetLine(i))
                    ? MarkdownBlockKind.Blank
                    : MarkdownBlockKind.Paragraph;
                semantics[i].HeadingLevel = 0;
                semantics[i].IsCodeFenceStart = false;
                semantics[i].IsCodeFenceEnd = false;
            }
        }

        // Eigene Fence-Spans anwenden
        for (int si = 0; si < fenceSpans.Count; si++)
        {
            var sp = fenceSpans[si];
            for (int i = sp.StartLine; i <= sp.EndLine && i < doc.LineCount; i++)
            {
                fenceSpanByLine[i] = si;
                semantics[i].Kind = MarkdownBlockKind.CodeFence;
                semantics[i].HeadingLevel = 0;
            }

            if (sp.StartLine >= 0 && sp.StartLine < doc.LineCount)
                semantics[sp.StartLine].IsCodeFenceStart = true;

            if (sp.EndLine >= 0 && sp.EndLine < doc.LineCount)
                semantics[sp.EndLine].IsCodeFenceEnd = true;
        }

        int y = 6;
        int maxWidth = Math.Max(1, viewport.Width);

        int lineIdx = 0;
        while (lineIdx < doc.LineCount)
        {
            // Table als Grid rendern
            if (tableStarts.TryGetValue(lineIdx, out var tb))
            {
                var tableLayout = BuildTableLayout(tb, y);
                _tables.Add(tableLayout);

                y += tableLayout.Bounds.Height + 6;
                maxWidth = Math.Max(maxWidth, tableLayout.Bounds.Right + 12);
                lineIdx = tb.EndLine + 1;
                continue;
            }

            // Source-Zeilen von Grid-Tables überspringen
            if (_tableSourceLines.Contains(lineIdx))
            {
                lineIdx++;
                continue;
            }

            string source = doc.GetLine(lineIdx);
            var sem = semantics[lineIdx];

            bool forceRawThisLine = forceRawLines?.Contains(lineIdx) == true;
            bool forceRawInlineThisLine = forceRawInlineLines?.Contains(lineIdx) == true;

            // Parser sagt Table, aber hier raw -> als normaler Text rendern
            if (sem.Kind == MarkdownBlockKind.Table)
            {
                sem.Kind = string.IsNullOrWhiteSpace(source)
                    ? MarkdownBlockKind.Blank
                    : MarkdownBlockKind.Paragraph;
                sem.HeadingLevel = 0;
            }

            bool isHorizontalRule = IsHorizontalRuleKind(sem.Kind);

            LineFontRole role = ResolveFontRole(sem.Kind);
            Font measureFont = ResolveLineMeasureFont(sem.Kind, sem.HeadingLevel, role, out bool disposeMeasureFont);

            try
            {
                int left = sem.Kind switch
                {
                    MarkdownBlockKind.Quote => 24,
                    MarkdownBlockKind.List => 24,
                    MarkdownBlockKind.CodeFence => 16,
                    _ => 8
                };

                VisualProjection proj;
                IReadOnlyList<InlineRun> runs;

                if (forceRawThisLine)
                {
                    // Einheitlich raw: komplette Source sichtbar + keine inline transformation
                    proj = CreateIdentityProjection(source);
                    runs = proj.DisplayText.Length == 0
                        ? Array.Empty<InlineRun>()
                        : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                }
                else if (isHorizontalRule)
                {
                    proj = VisualProjection.HidePrefix(source, source.Length);
                    runs = Array.Empty<InlineRun>();
                }
                else if (sem.Kind == MarkdownBlockKind.CodeFence)
                {
                    int spanIdx = fenceSpanByLine[lineIdx];
                    bool hasSpan = spanIdx >= 0;
                    bool rawFence = false;

                    if (hasSpan)
                    {
                        var span = fenceSpans[spanIdx];
                        rawFence = forceRawCodeFenceStarts?.Contains(span.StartLine) == true;
                    }

                    if (!hasSpan || rawFence)
                    {
                        // Raw-Modus: komplette Source inkl. Marker
                        proj = CreateIdentityProjection(source);
                    }
                    else
                    {
                        var span = fenceSpans[spanIdx];
                        var hideRanges = new List<(int Start, int Length)>(2);

                        // Nur Marker ausblenden, Inhalt sichtbar lassen
                        if (lineIdx == span.StartLine && span.StartMarkerCol >= 0 && span.StartMarkerLen > 0)
                            hideRanges.Add((span.StartMarkerCol, span.StartMarkerLen));

                        if (lineIdx == span.EndLine && span.EndMarkerCol >= 0 && span.EndMarkerLen > 0)
                            hideRanges.Add((span.EndMarkerCol, span.EndMarkerLen));

                        proj = hideRanges.Count == 0
                            ? CreateIdentityProjection(source)
                            : BuildProjectionHidingRanges(source, hideRanges);
                    }

                    runs = proj.DisplayText.Length == 0
                        ? Array.Empty<InlineRun>()
                        : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                }
                else if (sem.Kind == MarkdownBlockKind.List && listItemByLine.TryGetValue(lineIdx, out var li))
                {
                    if (forceRawInlineThisLine)
                    {
                        // bei inline-raw in Listen komplette Source zeigen
                        proj = CreateIdentityProjection(source);
                        runs = proj.DisplayText.Length == 0
                            ? Array.Empty<InlineRun>()
                            : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                    }
                    else
                    {
                        // Ordered/Unordered/Nested sichtbar + korrektes Mapping
                        proj = BuildListProjection(source, li);

                        InlineParseResult inline = InlineMarkdown.Parse(proj.DisplayText);
                        proj = ComposeProjection(proj, inline);
                        runs = inline.Runs;
                    }
                }
                else
                {
                    // Für Nicht-Listen bleibt ProjectionFactory zuständig
                    var prefixProj = ProjectionFactory.Build(sem.Kind, source);

                    if (SupportsInlineFormatting(sem.Kind))
                    {
                        if (forceRawInlineThisLine)
                        {
                            // Prefix-Projektion bleibt, aber inline marker NICHT entfernen
                            proj = prefixProj;
                            runs = proj.DisplayText.Length == 0
                                ? Array.Empty<InlineRun>()
                                : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                        }
                        else
                        {
                            InlineParseResult inline = InlineMarkdown.Parse(prefixProj.DisplayText);
                            proj = ComposeProjection(prefixProj, inline);
                            runs = inline.Runs;
                        }
                    }
                    else
                    {
                        proj = prefixProj;
                        runs = proj.DisplayText.Length == 0
                            ? Array.Empty<InlineRun>()
                            : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                    }
                }

                int lineHeight = MeasureHeight(measureFont) + 4;
                if (sem.Kind == MarkdownBlockKind.Heading)
                    lineHeight += (sem.HeadingLevel <= 2 ? 4 : 2);
                else if (isHorizontalRule && !forceRawThisLine)
                    lineHeight = Math.Max(lineHeight, 14);

                int textWidth = (isHorizontalRule && !forceRawThisLine)
                    ? Math.Max(1, viewport.Width - left - 12)
                    : Math.Max(1, MeasureInlineRunsWidth(runs, measureFont));

                var line = new LayoutLine
                {
                    SourceLine = lineIdx,
                    SourceText = source,
                    Projection = proj,
                    Bounds = new Rectangle(left, y, Math.Max(1, textWidth), lineHeight),
                    TextX = left,
                    TextWidth = textWidth,
                    Kind = sem.Kind,
                    HeadingLevel = sem.HeadingLevel,
                    FontRole = role,
                    InlineRuns = runs
                };

                _lines.Add(line);
                _lineBySource[lineIdx] = line;

                y += lineHeight;
                maxWidth = Math.Max(maxWidth, left + textWidth + 24);
            }
            finally
            {
                if (disposeMeasureFont)
                    measureFont.Dispose();
            }

            lineIdx++;
        }

        ContentSize = new Size(Math.Max(viewport.Width, maxWidth), Math.Max(viewport.Height, y + 6));
    }

    public IEnumerable<LayoutLine> GetVisibleLines(Rectangle viewport)
    {
        foreach (var line in _lines)
        {
            if (line.Bounds.Bottom >= viewport.Top && line.Bounds.Top <= viewport.Bottom)
                yield return line;
        }
    }

    public IEnumerable<TableLayout> GetVisibleTables(Rectangle viewport)
    {
        foreach (var t in _tables)
        {
            if (t.Bounds.Bottom >= viewport.Top && t.Bounds.Top <= viewport.Bottom)
                yield return t;
        }
    }

    public LayoutLine? GetLine(int sourceLine)
        => _lineBySource.TryGetValue(sourceLine, out var line) ? line : null;

    public bool IsTableSourceLine(int sourceLine) => _tableSourceLines.Contains(sourceLine);

    public int GetNearestTextLine(int sourceLine, bool preferForward)
    {
        if (_lineBySource.ContainsKey(sourceLine)) return sourceLine;
        if (_sourceLineCount <= 0) return -1;

        for (int d = 1; d <= _sourceLineCount; d++)
        {
            int f = sourceLine + d;
            int b = sourceLine - d;

            if (preferForward)
            {
                if (f < _sourceLineCount && _lineBySource.ContainsKey(f)) return f;
                if (b >= 0 && _lineBySource.ContainsKey(b)) return b;
            }
            else
            {
                if (b >= 0 && _lineBySource.ContainsKey(b)) return b;
                if (f < _sourceLineCount && _lineBySource.ContainsKey(f)) return f;
            }
        }

        return _lineBySource.Keys.OrderBy(x => x).FirstOrDefault(-1);
    }

    public bool TryGetTableByStartLine(int startLine, out TableLayout table)
    {
        for (int i = 0; i < _tables.Count; i++)
        {
            if (_tables[i].StartLine == startLine)
            {
                table = _tables[i];
                return true;
            }
        }

        table = null!;
        return false;
    }

    public bool TryHitTestTableCell(Point contentPoint, out TableHit hit)
    {
        foreach (var t in _tables)
        {
            if (!t.Bounds.Contains(contentPoint)) continue;

            for (int r = 0; r < t.Rows; r++)
            {
                for (int c = 0; c < t.Cols; c++)
                {
                    Rectangle rect = t.GetCellRect(r, c);
                    if (rect.Contains(contentPoint))
                    {
                        hit = new TableHit(t, r, c, rect);
                        return true;
                    }
                }
            }

            int rr = Math.Clamp((contentPoint.Y - t.Bounds.Y) / Math.Max(1, t.Bounds.Height / Math.Max(1, t.Rows)), 0, t.Rows - 1);
            int cc = Math.Clamp((contentPoint.X - t.Bounds.X) / Math.Max(1, t.Bounds.Width / Math.Max(1, t.Cols)), 0, t.Cols - 1);
            Rectangle fallback = t.GetCellRect(rr, cc);
            hit = new TableHit(t, rr, cc, fallback);
            return true;
        }

        hit = default;
        return false;
    }

    public MarkdownPosition HitTestText(Point contentPoint)
    {
        if (_lines.Count == 0)
            return new MarkdownPosition(0, 0);

        LayoutLine best = _lines[0];
        int bestDist = DistanceToY(best.Bounds, contentPoint.Y);

        for (int i = 1; i < _lines.Count; i++)
        {
            int d = DistanceToY(_lines[i].Bounds, contentPoint.Y);
            if (d < bestDist)
            {
                bestDist = d;
                best = _lines[i];
            }

            if (d == 0)
            {
                best = _lines[i];
                break;
            }
        }

        int localX = contentPoint.X - best.TextX;

        Font hitFont = ResolveLineMeasureFont(best.Kind, best.HeadingLevel, best.FontRole, out bool disposeHitFont);
        int visCol = ColumnFromX(best.Projection.DisplayText, best.InlineRuns, hitFont, localX);
        if (disposeHitFont) hitFont.Dispose();

        visCol = Math.Clamp(visCol, 0, best.Projection.VisualToSource.Length - 1);

        int srcCol = best.Projection.VisualToSource[visCol];
        srcCol = Math.Clamp(srcCol, 0, best.SourceText.Length);

        return new MarkdownPosition(best.SourceLine, srcCol);
    }

    private TableLayout BuildTableLayout(TableBlock block, int topY)
    {
        TableModel model = TableModel.FromBlock(block);
        model.Normalize();

        int rows = model.RowCount;
        int cols = model.ColumnCount;

        int cellPadX = 8;
        int cellPadY = 5;
        int left = 8;

        var colWidths = new int[cols];
        var rowHeights = new int[rows];

        var rects = new Rectangle[rows, cols];
        var texts = new string[rows, cols];
        var runs = new IReadOnlyList<InlineRun>[rows, cols];

        var alignments = new TableAlignment[cols];
        for (int c = 0; c < cols; c++)
        {
            alignments[c] = (block.Alignments is not null && c < block.Alignments.Count)
                ? block.Alignments[c]
                : default;
        }

        for (int c = 0; c < cols; c++)
        {
            int w = 30;

            for (int r = 0; r < rows; r++)
            {
                string raw = model.Rows[r][c] ?? string.Empty;
                InlineParseResult parsed = InlineMarkdown.Parse(raw);

                texts[r, c] = parsed.Text;
                runs[r, c] = parsed.Runs;

                Font measureFont = (r == 0) ? _boldFont : _baseFont;

                int contentW = parsed.Runs.Count > 0
                    ? MeasureInlineRunsWidthForTableLayout(parsed.Runs, measureFont)
                    : MeasureWidth(parsed.Text, measureFont);

                w = Math.Max(w, contentW + cellPadX * 2);
            }

            colWidths[c] = w;
        }

        int baseHeight = MeasureHeight(_baseFont) + cellPadY * 2;
        for (int r = 0; r < rows; r++)
            rowHeights[r] = baseHeight;

        int totalW = colWidths.Sum();
        int totalH = rowHeights.Sum();

        var bounds = new Rectangle(left, topY, totalW, totalH);

        int y = topY;
        for (int r = 0; r < rows; r++)
        {
            int x = left;
            for (int c = 0; c < cols; c++)
            {
                rects[r, c] = new Rectangle(x, y, colWidths[c], rowHeights[r]);
                x += colWidths[c];
            }
            y += rowHeights[r];
        }

        return new TableLayout(
            block.StartLine,
            block.EndLine,
            rows,
            cols,
            bounds,
            block,
            rects,
            texts,
            runs,
            alignments);
    }

    private static bool SupportsInlineFormatting(MarkdownBlockKind kind) => kind switch
    {
        MarkdownBlockKind.Paragraph => true,
        MarkdownBlockKind.Heading => true,
        MarkdownBlockKind.Quote => true,
        MarkdownBlockKind.List => true,
        _ => false
    };

    private static VisualProjection ComposeProjection(VisualProjection outer, InlineParseResult inner)
    {
        int srcN = outer.SourceToVisual.Length - 1;
        int visN = inner.Text.Length;

        int[] v2s = new int[visN + 1];
        for (int v = 0; v <= visN; v++)
        {
            int mid = inner.VisualToSource[v];
            mid = Math.Clamp(mid, 0, outer.VisualToSource.Length - 1);
            v2s[v] = outer.VisualToSource[mid];
        }

        int[] s2v = new int[srcN + 1];
        for (int s = 0; s <= srcN; s++)
        {
            int mid = outer.SourceToVisual[s];
            mid = Math.Clamp(mid, 0, inner.SourceToVisual.Length - 1);
            s2v[s] = inner.SourceToVisual[mid];
        }

        return VisualProjection.Create(inner.Text, v2s, s2v);
    }

    private static VisualProjection BuildListProjection(string source, ListItem item)
    {
        source ??= string.Empty;
        int n = source.Length;

        int contentStart = FindListContentStart(source);
        contentStart = Math.Clamp(contentStart, 0, n);

        string indent = new string(' ', Math.Max(0, item.Level) * 2);
        string marker = item.MarkerKind == ListMarkerKind.Ordered
            ? $"{(item.OrderedNumber ?? 1)}. "
            : "• ";

        string content = contentStart < n ? source[contentStart..] : string.Empty;
        string display = indent + marker + content;

        int prefixLen = indent.Length + marker.Length;

        int[] s2v = new int[n + 1];
        for (int s = 0; s <= n; s++)
        {
            s2v[s] = s < contentStart ? prefixLen : prefixLen + (s - contentStart);
        }

        int visLen = display.Length;
        int[] v2s = new int[visLen + 1];
        for (int v = 0; v <= visLen; v++)
        {
            if (v <= prefixLen)
                v2s[v] = contentStart;
            else
                v2s[v] = Math.Min(n, contentStart + (v - prefixLen));
        }

        return VisualProjection.Create(display, v2s, s2v);
    }

    private static int FindListContentStart(string line)
    {
        if (string.IsNullOrEmpty(line)) return 0;

        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;

        if (i >= line.Length) return i;

        if (line[i] is '-' or '+' or '*')
        {
            i++;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            return i;
        }

        int p = i;
        while (p < line.Length && char.IsDigit(line[p])) p++;

        if (p > i && p < line.Length && line[p] == '.')
        {
            p++;
            while (p < line.Length && char.IsWhiteSpace(line[p])) p++;
            return p;
        }

        return i;
    }

    private static int DistanceToY(Rectangle r, int y)
    {
        if (y < r.Top) return r.Top - y;
        if (y > r.Bottom) return y - r.Bottom;
        return 0;
    }

    private static int ColumnFromX(string displayText, IReadOnlyList<InlineRun> runs, Font baseFont, int localX)
    {
        if (displayText.Length == 0 || localX <= 0) return 0;
        if (runs.Count == 0) return ColumnFromXSimple(displayText, baseFont, localX);

        int x = 0;
        int col = 0;

        var cache = new Dictionary<InlineStyle, Font>();
        try
        {
            foreach (var run in runs)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;

                Font f = GetOrCreateRunFont(cache, baseFont, run.Style);

                for (int i = 0; i < run.Text.Length; i++)
                {
                    int cw = MeasureWidth(run.Text[i].ToString(), f);
                    int nextX = x + cw;

                    if (localX <= nextX)
                    {
                        int dl = localX - x;
                        int dr = nextX - localX;
                        return dl <= dr ? col : col + 1;
                    }

                    x = nextX;
                    col++;
                }
            }
        }
        finally
        {
            foreach (var kv in cache)
                kv.Value.Dispose();
        }

        return displayText.Length;
    }

    private static int ColumnFromXSimple(string displayText, Font font, int localX)
    {
        int prevWidth = 0;
        for (int col = 1; col <= displayText.Length; col++)
        {
            int w = MeasureWidth(displayText[..col], font);
            if (localX <= w)
            {
                int dl = localX - prevWidth;
                int dr = w - localX;
                return dl <= dr ? col - 1 : col;
            }
            prevWidth = w;
        }

        return displayText.Length;
    }

    private static int MeasureInlineRunsWidth(IReadOnlyList<InlineRun> runs, Font baseFont)
    {
        if (runs.Count == 0) return 0;

        int width = 0;
        var cache = new Dictionary<InlineStyle, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;
                Font f = GetOrCreateRunFont(cache, baseFont, run.Style);
                width += MeasureWidth(run.Text, f);
            }
        }
        finally
        {
            foreach (var kv in cache)
                kv.Value.Dispose();
        }

        return width;
    }

    private static Font GetOrCreateRunFont(Dictionary<InlineStyle, Font> cache, Font baseFont, InlineStyle style)
    {
        if (style == InlineStyle.None)
            return baseFont;

        if (cache.TryGetValue(style, out var f))
            return f;

        f = InlineMarkdown.CreateStyledFont(baseFont, style);
        cache[style] = f;
        return f;
    }

    private Font ResolveFont(LineFontRole role) => role switch
    {
        LineFontRole.Bold => _boldFont,
        LineFontRole.Mono => _monoFont,
        _ => _baseFont
    };

    private Font ResolveLineMeasureFont(MarkdownBlockKind kind, int headingLevel, LineFontRole role, out bool dispose)
    {
        if (kind == MarkdownBlockKind.Heading)
        {
            dispose = true;
            return MarkdownTypography.CreateHeadingFont(_baseFont, headingLevel);
        }

        dispose = false;
        return ResolveFont(role);
    }

    private static LineFontRole ResolveFontRole(MarkdownBlockKind kind) => kind switch
    {
        MarkdownBlockKind.Heading => LineFontRole.Bold,
        MarkdownBlockKind.CodeFence => LineFontRole.Mono,
        _ => LineFontRole.Base
    };

    private static int MeasureWidth(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), MeasureFlags).Width;
    }

    private static int MeasureHeight(Font font)
        => TextRenderer.MeasureText("Ag", font, new Size(int.MaxValue, int.MaxValue), MeasureFlags).Height;

    private static bool IsHorizontalRuleKind(MarkdownBlockKind kind)
        => kind == MarkdownBlockKind.HorizontalRule;

    private static LineSemantic[] BuildSemantics(DocumentModel doc)
    {
        int n = doc.LineCount;
        var sem = new LineSemantic[n];

        for (int i = 0; i < n; i++)
        {
            sem[i].Kind = string.IsNullOrWhiteSpace(doc.GetLine(i))
                ? MarkdownBlockKind.Blank
                : MarkdownBlockKind.Paragraph;
            sem[i].HeadingLevel = 0;
            sem[i].IsCodeFenceStart = false;
            sem[i].IsCodeFenceEnd = false;
        }

        foreach (var b in doc.Blocks)
        {
            switch (b)
            {
                case BlankBlock bb:
                    if (bb.StartLine >= 0 && bb.StartLine < n)
                    {
                        sem[bb.StartLine].Kind = MarkdownBlockKind.Blank;
                        sem[bb.StartLine].HeadingLevel = 0;
                    }
                    break;

                case HeadingBlock h:
                    if (h.StartLine >= 0 && h.StartLine < n)
                    {
                        sem[h.StartLine].Kind = MarkdownBlockKind.Heading;
                        sem[h.StartLine].HeadingLevel = h.Level;
                    }
                    break;

                case QuoteBlock q:
                    for (int i = Math.Max(0, q.StartLine); i <= Math.Min(n - 1, q.EndLine); i++)
                    {
                        sem[i].Kind = MarkdownBlockKind.Quote;
                        sem[i].HeadingLevel = 0;
                    }
                    break;

                case ListBlock l:
                    for (int i = Math.Max(0, l.StartLine); i <= Math.Min(n - 1, l.EndLine); i++)
                    {
                        sem[i].Kind = MarkdownBlockKind.List;
                        sem[i].HeadingLevel = 0;
                    }
                    break;

                case CodeFenceBlock c:
                    for (int i = Math.Max(0, c.StartLine); i <= Math.Min(n - 1, c.EndLine); i++)
                    {
                        sem[i].Kind = MarkdownBlockKind.CodeFence;
                        sem[i].HeadingLevel = 0;
                    }

                    if (c.StartLine >= 0 && c.StartLine < n)
                        sem[c.StartLine].IsCodeFenceStart = true;

                    if (c.EndLine >= 0 && c.EndLine < n)
                        sem[c.EndLine].IsCodeFenceEnd = true;
                    break;

                case TableBlock t:
                    for (int i = Math.Max(0, t.StartLine); i <= Math.Min(n - 1, t.EndLine); i++)
                    {
                        sem[i].Kind = MarkdownBlockKind.Table;
                        sem[i].HeadingLevel = 0;
                    }
                    break;

                default:
                    for (int i = Math.Max(0, b.StartLine); i <= Math.Min(n - 1, b.EndLine); i++)
                    {
                        sem[i].Kind = b.Kind;
                        if (b.Kind != MarkdownBlockKind.Heading)
                            sem[i].HeadingLevel = 0;
                    }
                    break;
            }
        }

        return sem;
    }

    private struct LineSemantic
    {
        public MarkdownBlockKind Kind;
        public int HeadingLevel;
        public bool IsCodeFenceStart;
        public bool IsCodeFenceEnd;
    }
}
