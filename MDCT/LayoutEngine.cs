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

    // NEU: bereits inline-parste Runs (ohne Markdown-Marker)
    public required IReadOnlyList<InlineRun> InlineRuns { get; init; }
}

public readonly record struct TableHit(TableLayout Table, int Row, int Col, Rectangle CellBounds);

public sealed class TableLayout
{
    private readonly Rectangle[,] _cellRects;
    private readonly string[,] _cellTexts;

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
        string[,] cellTexts)
    {
        StartLine = startLine;
        EndLine = endLine;
        Rows = rows;
        Cols = cols;
        Bounds = bounds;
        Block = block;
        _cellRects = cellRects;
        _cellTexts = cellTexts;
    }

    public Rectangle GetCellRect(int row, int col) => _cellRects[row, col];
    public string GetCellText(int row, int col) => _cellTexts[row, col];

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

    /// <summary>
    /// forceRawTableStarts: table start-lines that should be rendered as raw source (not as grid).
    /// </summary>
    public void Rebuild(
        DocumentModel doc,
        Size viewport,
        Font baseFont,
        Font boldFont,
        Font monoFont,
        IReadOnlySet<int>? forceRawTableStarts = null)
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

        // Only non-RAW tables are rendered as grid.
        var tableStarts = new Dictionary<int, TableBlock>();
        foreach (var t in doc.Blocks.OfType<TableBlock>())
        {
            bool forceRaw = forceRawTableStarts?.Contains(t.StartLine) == true;
            if (forceRaw) continue;

            tableStarts[t.StartLine] = t;

            for (int i = t.StartLine; i <= t.EndLine; i++)
                _tableSourceLines.Add(i);
        }

        int y = 6;
        int maxWidth = Math.Max(1, viewport.Width);

        int lineIdx = 0;
        while (lineIdx < doc.LineCount)
        {
            // Render table as grid.
            if (tableStarts.TryGetValue(lineIdx, out var tb))
            {
                var tableLayout = BuildTableLayout(tb, y);
                _tables.Add(tableLayout);

                y += tableLayout.Bounds.Height + 6;
                maxWidth = Math.Max(maxWidth, tableLayout.Bounds.Right + 12);
                lineIdx = tb.EndLine + 1;
                continue;
            }

            // Skip source lines that belong to grid tables.
            if (_tableSourceLines.Contains(lineIdx))
            {
                lineIdx++;
                continue;
            }

            string source = doc.GetLine(lineIdx);
            var sem = semantics[lineIdx];

            // If parser says "Table" but this table is in raw-mode, draw as plain text.
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

            int left = sem.Kind switch
            {
                MarkdownBlockKind.Quote => 24,
                MarkdownBlockKind.List => 24,
                MarkdownBlockKind.CodeFence => 16,
                _ => 8
            };

            VisualProjection proj;
            IReadOnlyList<InlineRun> runs;

            if (isHorizontalRule)
            {
                // hide complete source
                proj = VisualProjection.HidePrefix(source, source.Length);
                runs = Array.Empty<InlineRun>();
            }
            else
            {
                // 1) block-prefix projection (quote/list/heading marker)
                var prefixProj = ProjectionFactory.Build(sem.Kind, source);

                // 2) inline projection (bold/italic/strike marker)
                if (SupportsInlineFormatting(sem.Kind))
                {
                    InlineParseResult inline = InlineMarkdown.Parse(prefixProj.DisplayText);
                    proj = ComposeProjection(prefixProj, inline);
                    runs = inline.Runs;
                }
                else
                {
                    proj = prefixProj;
                    runs = proj.DisplayText.Length == 0
                        ? Array.Empty<InlineRun>()
                        : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                }
            }

            string display = proj.DisplayText;

            int lineHeight = MeasureHeight(measureFont) + 4;
            if (sem.Kind == MarkdownBlockKind.Heading)
                lineHeight += (sem.HeadingLevel <= 2 ? 4 : 2);
            else if (isHorizontalRule)
                lineHeight = Math.Max(lineHeight, 14);

            int textWidth = isHorizontalRule
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

            if (disposeMeasureFont)
                measureFont.Dispose();

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

            // Inside table but on border/line -> choose nearest cell.
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

        for (int c = 0; c < cols; c++)
        {
            int w = 30;
            for (int r = 0; r < rows; r++)
            {
                string txt = model.Rows[r][c];
                w = Math.Max(w, MeasureWidth(txt, _baseFont) + cellPadX * 2);
            }
            colWidths[c] = w;
        }

        int baseHeight = MeasureHeight(_baseFont) + cellPadY * 2;
        for (int r = 0; r < rows; r++)
            rowHeights[r] = baseHeight;

        int totalW = colWidths.Sum();
        int totalH = rowHeights.Sum();

        var bounds = new Rectangle(left, topY, totalW, totalH);
        var rects = new Rectangle[rows, cols];
        var texts = new string[rows, cols];

        int y = topY;
        for (int r = 0; r < rows; r++)
        {
            int x = left;
            for (int c = 0; c < cols; c++)
            {
                rects[r, c] = new Rectangle(x, y, colWidths[c], rowHeights[r]);
                texts[r, c] = model.Rows[r][c] ?? string.Empty;
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
            texts);
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
        => kind.ToString().Equals("HorizontalRule", StringComparison.Ordinal);

    private static LineSemantic[] BuildSemantics(DocumentModel doc)
    {
        int n = doc.LineCount;
        var sem = new LineSemantic[n];

        // Default: blank or paragraph
        for (int i = 0; i < n; i++)
        {
            sem[i].Kind = string.IsNullOrWhiteSpace(doc.GetLine(i))
                ? MarkdownBlockKind.Blank
                : MarkdownBlockKind.Paragraph;
            sem[i].HeadingLevel = 0;
        }

        // Override by parsed blocks
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
                    break;

                case TableBlock t:
                    for (int i = Math.Max(0, t.StartLine); i <= Math.Min(n - 1, t.EndLine); i++)
                    {
                        sem[i].Kind = MarkdownBlockKind.Table;
                        sem[i].HeadingLevel = 0;
                    }
                    break;

                default:
                    // Catch-all for future block types (e.g. HorizontalRuleBlock)
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
    }
}
