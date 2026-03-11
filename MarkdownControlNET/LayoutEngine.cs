using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
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
    public required int SourceLine { get; set; }
    public required string SourceText { get; set; }
    public required VisualProjection Projection { get; set; }
    public required float[] VisualOffsets { get; set; }
    public required Rectangle Bounds { get; set; }
    public required int TextX { get; init; }
    public required int TextWidth { get; set; }
    public required MarkdownBlockKind Kind { get; init; }
    public required int HeadingLevel { get; init; }
    public required LineFontRole FontRole { get; init; }

    // Already inline-parsed runs (without markdown markers)
    public required IReadOnlyList<InlineRun> InlineRuns { get; set; }
    public bool IsRealized { get; set; }

    public bool IsImagePreview { get; init; }
    public string ImageAltText { get; init; } = string.Empty;
    public string ImageSource { get; init; } = string.Empty;

    // Task-list metadata
    public bool IsTaskListItem { get; init; }
    public bool IsTaskChecked { get; init; }
    public int TaskMarkerSourceStart { get; init; } = -1; // '[' index in source
    public int TaskMarkerSourceLength { get; init; }      // usually 3 => [ ] / [x]
    public int ListContentSourceStart { get; init; } = -1; // source column where visible content starts

    // Quote/Admonition metadata
    public AdmonitionKind QuoteAdmonition { get; init; } = AdmonitionKind.None;
    public bool IsAdmonitionMarkerLine { get; init; }
    public int QuoteStartLine { get; init; } = -1;
    public int QuoteEndLine { get; init; } = -1;

    public bool IsAdmonition => QuoteAdmonition != AdmonitionKind.None;
    public bool IsQuoteStart => Kind == MarkdownBlockKind.Quote && QuoteStartLine >= 0 && SourceLine == QuoteStartLine;
    public bool IsQuoteEnd => Kind == MarkdownBlockKind.Quote && QuoteEndLine >= 0 && SourceLine == QuoteEndLine;

    public string AdmonitionTitle => QuoteAdmonition switch
    {
        AdmonitionKind.Note => "Note",
        AdmonitionKind.Tip => "Tip",
        AdmonitionKind.Important => "Important",
        AdmonitionKind.Warning => "Warning",
        AdmonitionKind.Caution => "Caution",
        _ => string.Empty
    };
}

public readonly record struct TableHit(TableLayout Table, int Row, int Col, Rectangle CellBounds);

public sealed class TableLayout
{
    private readonly Rectangle[,] _cellRects;
    private readonly string[,] _cellTexts;
    private readonly IReadOnlyList<InlineRun>[,] _cellRuns;
    private readonly TableAlignment[] _colAlignments;
    private Rectangle _bounds;

    public int StartLine { get; }
    public int EndLine { get; }
    public int Rows { get; }
    public int Cols { get; }
    public Rectangle Bounds => _bounds;
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
        _bounds = bounds;
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

    public void ShiftY(int deltaY)
    {
        if (deltaY == 0)
            return;

        _bounds = new Rectangle(_bounds.X, _bounds.Y + deltaY, _bounds.Width, _bounds.Height);

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                Rectangle rect = _cellRects[r, c];
                _cellRects[r, c] = new Rectangle(rect.X, rect.Y + deltaY, rect.Width, rect.Height);
            }
        }
    }
}

public sealed class LayoutEngine
{
    private readonly List<LayoutLine> _lines = new();
    private readonly Dictionary<int, LayoutLine> _lineBySource = new();
    private readonly Dictionary<int, int> _lineIndexBySource = new();
    private readonly List<TableLayout> _tables = new();
    private readonly Dictionary<int, TableLayout> _tableByStartLine = new();
    private readonly HashSet<int> _tableSourceLines = new();
    private readonly Dictionary<string, InlineParseResult> _inlineParseCache = new(StringComparer.Ordinal);
    private readonly Dictionary<int, ListItem> _listItemByLine = new();
    private readonly Dictionary<int, ImageBlock> _imageByLine = new();
    private readonly Dictionary<int, FootnoteDefinitionLine> _footnoteLineBySource = new();
    private readonly Dictionary<int, FootnoteDefinitionBlock> _footnoteBlockBySourceLine = new();
    private LineSemantic[] _semantics = Array.Empty<LineSemantic>();
    private List<CodeFenceSpan> _fenceSpans = new();
    private int[] _fenceSpanByLine = Array.Empty<int>();
    private IReadOnlySet<int> _forceRawCodeFenceStarts = new HashSet<int>();
    private IReadOnlySet<int> _forceRawLines = new HashSet<int>();
    private IReadOnlySet<int> _forceRawInlineLines = new HashSet<int>();

    private int[] _lineTops = Array.Empty<int>();
    private int[] _tableTops = Array.Empty<int>();

    private Font _baseFont = SystemFonts.DefaultFont;
    private Font _boldFont = SystemFonts.DefaultFont;
    private Font _monoFont = SystemFonts.DefaultFont;
    private Func<string, Size?>? _imageSizeProvider;
    private MarkdownFootnoteIndex _footnoteIndex = MarkdownFootnoteIndex.Empty;
    private IReadOnlyDictionary<string, int> _footnoteDisplayNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private float _measurementDpiX = 96f;
    private float _measurementDpiY = 96f;
    private IntPtr _controlHandle;

    private int _sourceLineCount;
    private int _maxContentWidth;
    private Size _lastViewport = new(1, 1);
    private DocumentModel? _currentDoc;

    public Size ContentSize { get; private set; } = new(1, 1);

    private static readonly StringFormat MeasureStringFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces,
        Trimming = StringTrimming.None,
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Near
    };

    /// <summary>
    /// Creates a screen-compatible Graphics for text measurement.
    /// Using a screen DC (instead of a memory-bitmap DC) ensures that
    /// MeasureCharacterRanges produces offsets consistent with DrawString
    /// on the paint Graphics — especially with ClearType grid-fitting.
    /// </summary>
    private Graphics CreateMeasurementGraphics()
    {
        // FromHwnd(IntPtr.Zero) = screen DC; FromHwnd(handle) = window DC.
        // Both are display-compatible and match the paint Graphics.
        var g = Graphics.FromHwnd(_controlHandle);
        g.PageUnit = GraphicsUnit.Pixel;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        return g;
    }

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
    private const int InlineCodeChipPadX = 4;
    private const float FootnoteFontScale = 0.82f;
    private const int ImagePreviewMaxWidth = 720;
    private const int ImagePreviewMaxHeight = 420;
    private const int ImagePreviewPlaceholderWidth = 320;
    private const int ImagePreviewPlaceholderHeight = 180;
    private const int ImagePreviewPaddingY = 8;
    private const TextFormatFlags PlainTextMeasureFlags =
        TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern bool GetTextExtentExPointW(
        IntPtr hdc,
        string lpString,
        int cchString,
        int nMaxExtent,
        out int lpnFit,
        [Out] int[] alpDx,
        out NativeSize lpSize);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int cx;
        public int cy;
    }

    private int MeasureInlineRunsWidthForTableLayout(IReadOnlyList<InlineRun> runs, Font baseFont, Graphics graphics)
    {
        if (runs.Count == 0) return 0;

        int width = 0;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (run.IsImage)
                {
                    width += InlineImageMetrics.CalculateSize(run.Source, _imageSizeProvider).Width;
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                bool isFootnoteReference = run.IsFootnoteReference;
                InlineStyle normalized = run.Style & ~InlineStyle.Code;

                int key = ((int)normalized & 0xFF)
                          | (isCode ? 0x100 : 0)
                          | (isFootnoteReference ? 0x200 : 0)
                          | (((int)baseFont.Style & 0xFF) << 9);

                if (!cache.TryGetValue(key, out var f))
                {
                    Font seed = isCode ? _monoFont : baseFont;
                    f = CreateRunDisplayFont(seed, normalized, isFootnoteReference);
                    cache[key] = f;
                }

                int w = MeasureWidth(graphics, run.Text, f);
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
    // Fence detection:
    // - Backtick: >=3
    // - Tilde: >=3
    // ------------------------------------------------------------

    private static bool IsValidFenceOpenerLen(char ch, int len)
    {
        if (ch == '`')
            return len >= 3;

        if (ch == '~')
            return len >= 3;

        return false;
    }

    private static bool IsValidFenceCloserLen(char ch, int openLen, int closeLen)
    {
        if (ch == '`')
        {
            if (closeLen < 3)
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
    ///                    (no InlineMarkdown.Parse, but block-prefix projection can stay).
    /// </summary>
    public void Rebuild(
        DocumentModel doc,
        Size viewport,
        Font baseFont,
        Font boldFont,
        Font monoFont,
        float dpiX,
        float dpiY,
        int preferredSourceLine = 0,
        Func<string, Size?>? imageSizeProvider = null,
        IReadOnlySet<int>? forceRawTableStarts = null,
        IReadOnlySet<int>? forceRawCodeFenceStarts = null,
        IReadOnlySet<int>? forceRawLines = null,
        IReadOnlySet<int>? forceRawInlineLines = null,
        IntPtr controlHandle = default)
    {
        _sourceLineCount = doc.LineCount;
        _currentDoc = doc;
        _lastViewport = viewport;

        _baseFont = baseFont;
        _boldFont = boldFont;
        _monoFont = monoFont;
        _imageSizeProvider = imageSizeProvider;
        _measurementDpiX = Math.Max(1f, dpiX);
        _measurementDpiY = Math.Max(1f, dpiY);
        _controlHandle = controlHandle;
        _footnoteIndex = MarkdownFootnoteHelper.BuildIndex(doc.Lines);
        _footnoteDisplayNumbers = BuildFootnoteDisplayNumbers(doc, _footnoteIndex);

        _lines.Clear();
        _lineBySource.Clear();
        _lineIndexBySource.Clear();
        _tables.Clear();
        _tableByStartLine.Clear();
        _tableSourceLines.Clear();
        _listItemByLine.Clear();
        _imageByLine.Clear();
        _footnoteLineBySource.Clear();
        _footnoteBlockBySourceLine.Clear();
        _lineTops = Array.Empty<int>();
        _tableTops = Array.Empty<int>();
        _semantics = Array.Empty<LineSemantic>();
        _fenceSpans.Clear();
        _fenceSpanByLine = Array.Empty<int>();
        _forceRawCodeFenceStarts = forceRawCodeFenceStarts ?? new HashSet<int>();
        _forceRawLines = forceRawLines ?? new HashSet<int>();
        _forceRawInlineLines = forceRawInlineLines ?? new HashSet<int>();
        _maxContentWidth = Math.Max(1, viewport.Width);

        if (doc.LineCount == 0)
        {
            ContentSize = new Size(Math.Max(1, viewport.Width), Math.Max(1, viewport.Height));
            return;
        }

        using var measurementGraphics = CreateMeasurementGraphics();

        // Pre-compute average character width for fast off-screen width estimation.
        float avgCharWidth = MeasureAverageCharWidth(measurementGraphics, baseFont);

        var semantics = BuildSemantics(doc);
        _semantics = semantics;

        // Mapping SourceLine -> ListItem
        foreach (var lb in doc.Blocks.OfType<ListBlock>())
        {
            foreach (var it in lb.Items)
                _listItemByLine[it.SourceLine] = it;
        }

        foreach (ImageBlock image in doc.Blocks.OfType<ImageBlock>())
            _imageByLine[image.StartLine] = image;

        foreach (FootnoteDefinitionBlock footnoteBlock in doc.Blocks.OfType<FootnoteDefinitionBlock>())
        {
            foreach (FootnoteDefinitionLine footnoteLine in footnoteBlock.Lines)
            {
                _footnoteLineBySource[footnoteLine.SourceLine] = footnoteLine;
                _footnoteBlockBySourceLine[footnoteLine.SourceLine] = footnoteBlock;
            }
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

        _fenceSpans = BuildCodeFenceSpans(doc);
        int[] fenceSpanByLine = new int[doc.LineCount];
        Array.Fill(fenceSpanByLine, -1);
        _fenceSpanByLine = fenceSpanByLine;

        // Neutralize parser code fence (custom span logic is authoritative)
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

        // Apply custom fence spans
        for (int si = 0; si < _fenceSpans.Count; si++)
        {
            var sp = _fenceSpans[si];
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
            // Render table as grid
            if (tableStarts.TryGetValue(lineIdx, out var tb))
            {
                var tableLayout = BuildTableLayout(tb, y, measurementGraphics);
                _tables.Add(tableLayout);
                _tableByStartLine[tableLayout.StartLine] = tableLayout;

                y += tableLayout.Bounds.Height + 6;
                maxWidth = Math.Max(maxWidth, tableLayout.Bounds.Right + 12);
                lineIdx = tb.EndLine + 1;
                continue;
            }

            // Skip source lines covered by grid tables
            if (_tableSourceLines.Contains(lineIdx))
            {
                lineIdx++;
                continue;
            }

            string source = doc.GetLine(lineIdx);
            var sem = semantics[lineIdx];

            bool forceRawThisLine = _forceRawLines.Contains(lineIdx);
            bool forceRawInlineThisLine = _forceRawInlineLines.Contains(lineIdx);

            // Parser says Table, but raw mode is active -> render as normal text
            if (sem.Kind == MarkdownBlockKind.Table)
            {
                sem.Kind = string.IsNullOrWhiteSpace(source)
                    ? MarkdownBlockKind.Blank
                    : MarkdownBlockKind.Paragraph;
                sem.HeadingLevel = 0;
            }

            bool isHorizontalRule = IsHorizontalRuleKind(sem.Kind);
            bool isAdmonitionMarkerLine =
                sem.Kind == MarkdownBlockKind.Quote &&
                sem.QuoteAdmonition != AdmonitionKind.None &&
                sem.IsAdmonitionMarkerLine;

                // Task metadata defaults for this line
                bool isTaskListItem = false;
                bool isTaskChecked = false;
                int taskMarkerSourceStart = -1;
                int taskMarkerSourceLength = 0;
                int listContentSourceStart = -1;
                bool isImagePreview = false;
                string imageAltText = string.Empty;
                string imageSource = string.Empty;

            LineFontRole role = ResolveFontRole(sem.Kind);
            if (isAdmonitionMarkerLine && !forceRawThisLine)
                role = LineFontRole.Bold;

            Font measureFont = ResolveLineMeasureFont(sem.Kind, sem.HeadingLevel, role, out bool disposeMeasureFont);

            try
            {
                bool eagerLineRealization = ShouldRealizeLineEagerly(lineIdx, y, viewport, preferredSourceLine);
                int left = sem.Kind switch
                {
                    MarkdownBlockKind.Quote => 24,
                    MarkdownBlockKind.List => 24,
                    MarkdownBlockKind.FootnoteDefinition => 24,
                    MarkdownBlockKind.CodeFence => 16,
                    _ => 8
                };

                VisualProjection proj;
                IReadOnlyList<InlineRun> runs;
                bool lineRealized = true;

                if (forceRawThisLine)
                {
                    // Uniform raw: full source + no inline transform
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
                    int spanIdx = _fenceSpanByLine[lineIdx];
                    bool hasSpan = spanIdx >= 0;
                    bool rawFence = false;

                    if (hasSpan)
                    {
                        var span = _fenceSpans[spanIdx];
                        rawFence = _forceRawCodeFenceStarts.Contains(span.StartLine);
                    }

                    if (!hasSpan || rawFence)
                    {
                        // Raw mode: full source including markers
                        proj = CreateIdentityProjection(source);
                    }
                    else
                    {
                        var span = _fenceSpans[spanIdx];
                        var hideRanges = new List<(int Start, int Length)>(2);

                        // Hide only markers, keep content visible
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
                else if (isAdmonitionMarkerLine)
                {
                    // Marker line (e.g. > [!NOTE]) is hidden in rich mode.
                    // Editor can render a visual header instead.
                    proj = VisualProjection.HidePrefix(source, source.Length);
                    runs = Array.Empty<InlineRun>();
                }
                else if (sem.Kind == MarkdownBlockKind.Image && _imageByLine.TryGetValue(lineIdx, out var image))
                {
                    imageAltText = image.AltText;
                    imageSource = image.Source;

                    if (forceRawInlineThisLine)
                    {
                        proj = CreateIdentityProjection(source);
                        runs = proj.DisplayText.Length == 0
                            ? Array.Empty<InlineRun>()
                            : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                    }
                    else
                    {
                        proj = VisualProjection.HidePrefix(source, source.Length);
                        runs = Array.Empty<InlineRun>();
                        isImagePreview = true;
                    }
                }
                else if (sem.Kind == MarkdownBlockKind.List && _listItemByLine.TryGetValue(lineIdx, out var li))
                {
                    isTaskListItem = li.IsTask;
                    isTaskChecked = li.IsChecked;
                    taskMarkerSourceStart = li.TaskMarkerStartColumn;
                    taskMarkerSourceLength = li.TaskMarkerLength;
                    listContentSourceStart = li.ContentStartColumn;

                    if (forceRawInlineThisLine)
                    {
                        // In inline-raw mode for lists show full source
                        proj = CreateIdentityProjection(source);
                        runs = proj.DisplayText.Length == 0
                            ? Array.Empty<InlineRun>()
                            : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                    }
                    else
                    {
                        // Ordered/unordered/nested/task visual + stable source mapping
                        proj = BuildListProjection(source, li);
                        if (MightNeedInlineParsing(proj.DisplayText))
                        {
                            if (eagerLineRealization)
                            {
                                InlineParseResult inline = ApplyFootnotePresentation(ParseInlineCached(proj.DisplayText));
                                proj = ComposeProjection(proj, inline);
                                runs = inline.Runs;
                            }
                            else
                            {
                                runs = CreatePlainRuns(proj.DisplayText);
                                lineRealized = false;
                            }
                        }
                        else
                        {
                            runs = CreatePlainRuns(proj.DisplayText);
                        }
                    }
                }
                else if (sem.Kind == MarkdownBlockKind.FootnoteDefinition &&
                         _footnoteLineBySource.TryGetValue(lineIdx, out var footnoteLine) &&
                         _footnoteBlockBySourceLine.TryGetValue(lineIdx, out var footnoteBlock))
                {
                    if (forceRawThisLine)
                    {
                        proj = CreateIdentityProjection(source);
                        runs = proj.DisplayText.Length == 0
                            ? Array.Empty<InlineRun>()
                            : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                    }
                    else
                    {
                        var hideRanges = new List<(int Start, int Length)>(1);
                        if (footnoteLine.ContentStartColumn > 0)
                            hideRanges.Add((0, footnoteLine.ContentStartColumn));

                        proj = hideRanges.Count == 0
                            ? CreateIdentityProjection(source)
                            : BuildProjectionHidingRanges(source, hideRanges);

                        if (eagerLineRealization)
                        {
                            InlineParseResult inline = ApplyFootnoteDefinitionPresentation(
                                ParseInlineCached(proj.DisplayText),
                                footnoteBlock,
                                isFirstLine: footnoteLine.IsFirstLine,
                                isLastLine: lineIdx == footnoteBlock.EndLine);
                            proj = ComposeProjection(proj, inline);
                            runs = inline.Runs;
                        }
                        else
                        {
                            runs = CreatePlainRuns(proj.DisplayText);
                            lineRealized = false;
                        }
                    }
                }
                else
                {
                    // Non-list lines use ProjectionFactory
                    var prefixProj = ProjectionFactory.Build(sem.Kind, source);

                    if (SupportsInlineFormatting(sem.Kind))
                    {
                        if (forceRawInlineThisLine)
                        {
                            // Keep block prefix projection, keep inline markers visible
                            proj = prefixProj;
                            runs = proj.DisplayText.Length == 0
                                ? Array.Empty<InlineRun>()
                                : new[] { new InlineRun(proj.DisplayText, InlineStyle.None) };
                        }
                        else
                        {
                            if (MightNeedInlineParsing(prefixProj.DisplayText))
                            {
                                if (eagerLineRealization)
                                {
                                    InlineParseResult inline = ApplyFootnotePresentation(ParseInlineCached(prefixProj.DisplayText));
                                    proj = ComposeProjection(prefixProj, inline);
                                    runs = inline.Runs;
                                }
                                else
                                {
                                    proj = prefixProj;
                                    runs = CreatePlainRuns(proj.DisplayText);
                                    lineRealized = false;
                                }
                            }
                            else
                            {
                                proj = prefixProj;
                                runs = CreatePlainRuns(proj.DisplayText);
                            }
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

                int lineHeight;
                int textWidth;
                bool eagerVisualOffsets = ShouldMeasureVisualOffsetsEagerly(lineIdx, y, viewport, preferredSourceLine);
                float[] visualOffsets = eagerVisualOffsets
                    ? BuildVisualOffsets(proj.DisplayText, runs, measureFont, measurementGraphics, preferExactPlainTextMeasurement: sem.Kind == MarkdownBlockKind.Heading)
                    : Array.Empty<float>();

                if (isImagePreview)
                {
                    Size imageSize = CalculateImagePreviewSize(imageSource, viewport, left, imageSizeProvider);
                    textWidth = Math.Max(1, imageSize.Width);
                    lineHeight = Math.Max(1, imageSize.Height + (ImagePreviewPaddingY * 2));
                }
                else
                {
                    lineHeight = MeasureInlineRunsContentHeight(runs, measureFont, measurementGraphics) + 4;
                    if (!lineRealized && proj.DisplayText.Contains("![", StringComparison.Ordinal))
                        lineHeight = Math.Max(lineHeight, InlineImageMetrics.MaxHeight + 4);

                    if (sem.Kind == MarkdownBlockKind.Heading)
                        lineHeight += 2;
                    else if (isHorizontalRule && !forceRawThisLine)
                        lineHeight = Math.Max(lineHeight, 14);

                    if (isHorizontalRule && !forceRawThisLine)
                    {
                        textWidth = Math.Max(1, viewport.Width - left - 12);
                    }
                    else if (eagerVisualOffsets)
                    {
                        textWidth = Math.Max(1, (int)Math.Ceiling(GetVisualWidth(visualOffsets)));
                    }
                    else if (eagerLineRealization)
                    {
                        textWidth = Math.Max(1, (int)Math.Ceiling(
                            MeasureRenderedLineWidth(proj.DisplayText, runs, measureFont, measurementGraphics)));
                    }
                    else
                    {
                        // Fast estimate for off-screen lines; corrected on realization.
                        textWidth = Math.Max(1, EstimateTextWidth(proj.DisplayText, avgCharWidth));
                    }
                }

                var line = new LayoutLine
                {
                    SourceLine = lineIdx,
                    SourceText = source,
                    Projection = proj,
                    VisualOffsets = visualOffsets,
                    Bounds = new Rectangle(left, y, Math.Max(1, textWidth), lineHeight),
                    TextX = left,
                    TextWidth = textWidth,
                    Kind = sem.Kind,
                    HeadingLevel = sem.HeadingLevel,
                    FontRole = role,
                    InlineRuns = runs,
                    IsRealized = lineRealized,
                    IsImagePreview = isImagePreview,
                    ImageAltText = imageAltText,
                    ImageSource = imageSource,

                    IsTaskListItem = isTaskListItem,
                    IsTaskChecked = isTaskChecked,
                    TaskMarkerSourceStart = taskMarkerSourceStart,
                    TaskMarkerSourceLength = taskMarkerSourceLength,
                    ListContentSourceStart = listContentSourceStart,

                    QuoteAdmonition = sem.QuoteAdmonition,
                    IsAdmonitionMarkerLine = sem.IsAdmonitionMarkerLine,
                    QuoteStartLine = sem.QuoteStartLine,
                    QuoteEndLine = sem.QuoteEndLine
                };

                _lines.Add(line);
                _lineBySource[lineIdx] = line;
                _lineIndexBySource[lineIdx] = _lines.Count - 1;

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

        _lineTops = _lines.Count == 0 ? Array.Empty<int>() : _lines.Select(line => line.Bounds.Top).ToArray();
        _tableTops = _tables.Count == 0 ? Array.Empty<int>() : _tables.Select(table => table.Bounds.Top).ToArray();
        ContentSize = new Size(Math.Max(viewport.Width, maxWidth), Math.Max(viewport.Height, y + 6));
    }

    public IEnumerable<LayoutLine> GetVisibleLines(Rectangle viewport)
    {
        if (_lines.Count == 0)
            yield break;

        int start = FindFirstVisibleIndex(_lineTops, viewport.Top);
        for (int i = start; i < _lines.Count; i++)
        {
            var line = _lines[i];
            if (line.Bounds.Top > viewport.Bottom)
                yield break;

            if (line.Bounds.Bottom >= viewport.Top && line.Bounds.Top <= viewport.Bottom)
            {
                EnsureLineRealized(line);
                yield return line;
            }
        }
    }

    public IEnumerable<TableLayout> GetVisibleTables(Rectangle viewport)
    {
        if (_tables.Count == 0)
            yield break;

        int start = FindFirstVisibleIndex(_tableTops, viewport.Top);
        for (int i = start; i < _tables.Count; i++)
        {
            var t = _tables[i];
            if (t.Bounds.Top > viewport.Bottom)
                yield break;

            if (t.Bounds.Bottom >= viewport.Top && t.Bounds.Top <= viewport.Bottom)
                yield return t;
        }
    }

    public LayoutLine? GetLine(int sourceLine)
        => _lineBySource.TryGetValue(sourceLine, out var line) ? line : null;

    public LayoutLine? GetPreparedLine(int sourceLine)
    {
        if (!_lineBySource.TryGetValue(sourceLine, out LayoutLine? line))
            return null;

        EnsureLineRealized(line);
        return line;
    }

    public bool TryFastUpdateSimpleTextLine(int sourceLine, string sourceText)
    {
        if (!_lineBySource.TryGetValue(sourceLine, out LayoutLine? line))
            return false;

        if (line.IsImagePreview || IsHorizontalRuleKind(line.Kind))
            return false;

        if (line.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank))
            return false;

        using var measurementGraphics = CreateMeasurementGraphics();
        Font measureFont = ResolveLineMeasureFont(line.Kind, line.HeadingLevel, line.FontRole, out bool disposeMeasureFont);
        try
        {
            VisualProjection projection = CreateIdentityProjection(sourceText);
            IReadOnlyList<InlineRun> runs = CreatePlainRuns(projection.DisplayText);
            float[] visualOffsets = BuildVisualOffsets(projection.DisplayText, runs, measureFont, measurementGraphics, preferExactPlainTextMeasurement: false);
            int lineHeight = MeasureInlineRunsContentHeight(runs, measureFont, measurementGraphics) + 4;
            int textWidth = Math.Max(1, (int)Math.Ceiling(GetVisualWidth(visualOffsets)));

            line.SourceText = sourceText;
            ApplyRealizedLine(line, projection, runs, visualOffsets, textWidth, lineHeight);
            line.IsRealized = true;
            return true;
        }
        finally
        {
            if (disposeMeasureFont)
                measureFont.Dispose();
        }
    }

    public bool TryFastSplitSimpleTextLine(int sourceLine, string firstText, string secondText)
    {
        if (!CanFastMutateSimplePlainTextDocument() ||
            !_lineIndexBySource.TryGetValue(sourceLine, out int lineIndex) ||
            lineIndex < 0 ||
            lineIndex >= _lines.Count)
        {
            return false;
        }

        LayoutLine oldLine = _lines[lineIndex];
        if (oldLine.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank))
            return false;

        using var measurementGraphics = CreateMeasurementGraphics();

        LayoutLine firstLine = CreateRealizedSimpleTextLine(sourceLine, firstText, oldLine.Bounds.Top, measurementGraphics);
        LayoutLine secondLine = CreateRealizedSimpleTextLine(sourceLine + 1, secondText, firstLine.Bounds.Bottom, measurementGraphics);

        int deltaHeight = firstLine.Bounds.Height + secondLine.Bounds.Height - oldLine.Bounds.Height;

        _lines[lineIndex] = firstLine;
        _lines.Insert(lineIndex + 1, secondLine);

        for (int i = lineIndex + 2; i < _lines.Count; i++)
        {
            LayoutLine line = _lines[i];
            line.SourceLine++;

            if (deltaHeight != 0)
                line.Bounds = new Rectangle(line.Bounds.X, line.Bounds.Y + deltaHeight, line.Bounds.Width, line.Bounds.Height);
        }

        _sourceLineCount = _lines.Count;
        RebuildSimplePlainTextSemantics();
        RebuildLineIndexesAndMetrics();
        return true;
    }

    public bool TryFastMergeSimpleTextLines(int firstSourceLine, string mergedText)
    {
        if (!CanFastMutateSimplePlainTextDocument() ||
            !_lineIndexBySource.TryGetValue(firstSourceLine, out int firstIndex) ||
            firstIndex < 0 ||
            firstIndex >= _lines.Count - 1)
        {
            return false;
        }

        LayoutLine firstLine = _lines[firstIndex];
        LayoutLine secondLine = _lines[firstIndex + 1];
        if (firstLine.SourceLine != firstSourceLine || secondLine.SourceLine != firstSourceLine + 1)
            return false;

        if (firstLine.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank) ||
            secondLine.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank))
        {
            return false;
        }

        using var measurementGraphics = CreateMeasurementGraphics();
        LayoutLine mergedLine = CreateRealizedSimpleTextLine(firstSourceLine, mergedText, firstLine.Bounds.Top, measurementGraphics);

        int deltaHeight = mergedLine.Bounds.Height - (firstLine.Bounds.Height + secondLine.Bounds.Height);

        _lines[firstIndex] = mergedLine;
        _lines.RemoveAt(firstIndex + 1);

        for (int i = firstIndex + 1; i < _lines.Count; i++)
        {
            LayoutLine line = _lines[i];
            line.SourceLine--;

            if (deltaHeight != 0)
                line.Bounds = new Rectangle(line.Bounds.X, line.Bounds.Y + deltaHeight, line.Bounds.Width, line.Bounds.Height);
        }

        _sourceLineCount = _lines.Count;
        RebuildSimplePlainTextSemantics();
        RebuildLineIndexesAndMetrics();
        return true;
    }

    public float[] GetVisualOffsets(LayoutLine line) => EnsureVisualOffsets(line);

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
        return _tableByStartLine.TryGetValue(startLine, out table!);
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

        int pivot = FindNearestLineIndex(contentPoint.Y);
        LayoutLine best = _lines[pivot];
        int bestDist = DistanceToY(best.Bounds, contentPoint.Y);

        for (int i = Math.Max(0, pivot - 1); i <= Math.Min(_lines.Count - 1, pivot + 1); i++)
        {
            int d = DistanceToY(_lines[i].Bounds, contentPoint.Y);
            if (d < bestDist)
            {
                bestDist = d;
                best = _lines[i];
            }
        }

        float[] visualOffsets = EnsureVisualOffsets(best);
        float localX = contentPoint.X - best.TextX;
        int visCol = ColumnFromX(visualOffsets, localX);

        visCol = Math.Clamp(visCol, 0, best.Projection.VisualToSource.Length - 1);

        int srcCol = best.Projection.VisualToSource[visCol];
        srcCol = Math.Clamp(srcCol, 0, best.SourceText.Length);

        return new MarkdownPosition(best.SourceLine, srcCol);
    }

    private TableLayout BuildTableLayout(TableBlock block, int topY, Graphics graphics)
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
        int baseHeight = MeasureHeight(graphics, _baseFont) + cellPadY * 2;

        var alignments = new TableAlignment[cols];
        for (int c = 0; c < cols; c++)
        {
            alignments[c] = (block.Alignments is not null && c < block.Alignments.Count)
                ? block.Alignments[c]
                : default;
        }

        for (int r = 0; r < rows; r++)
            rowHeights[r] = baseHeight;

        for (int c = 0; c < cols; c++)
        {
            int w = 30;

            for (int r = 0; r < rows; r++)
            {
                string raw = model.Rows[r][c] ?? string.Empty;
                IReadOnlyList<InlineRun> parsedRuns;
                string parsedText;

                if (MightNeedInlineParsing(raw))
                {
                    InlineParseResult parsed = ApplyFootnotePresentation(ParseInlineCached(raw));
                    parsedText = parsed.Text;
                    parsedRuns = parsed.Runs;
                }
                else
                {
                    parsedText = raw;
                    parsedRuns = CreatePlainRuns(raw);
                }

                texts[r, c] = parsedText;
                runs[r, c] = parsedRuns;

                Font measureFont = (r == 0) ? _boldFont : _baseFont;

                int contentW = parsedRuns.Count > 0
                    ? MeasureInlineRunsWidthForTableLayout(parsedRuns, measureFont, graphics)
                    : MeasureWidth(graphics, parsedText, measureFont);
                int contentH = MeasureInlineRunsContentHeight(parsedRuns, measureFont, graphics);

                w = Math.Max(w, contentW + cellPadX * 2);
                rowHeights[r] = Math.Max(rowHeights[r], contentH + cellPadY * 2);
            }

            colWidths[c] = w;
        }

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

    private InlineParseResult ParseInlineCached(string text)
    {
        text ??= string.Empty;

        if (_inlineParseCache.TryGetValue(text, out var parsed))
            return parsed;

        if (_inlineParseCache.Count >= 2048)
            _inlineParseCache.Clear();

        parsed = InlineMarkdown.Parse(text);
        _inlineParseCache[text] = parsed;
        return parsed;
    }

    private static IReadOnlyDictionary<string, int> BuildFootnoteDisplayNumbers(DocumentModel doc, MarkdownFootnoteIndex index)
    {
        var numbers = new Dictionary<string, int>(index.NumbersByLabel, StringComparer.OrdinalIgnoreCase);
        int next = numbers.Count == 0 ? 1 : numbers.Values.Max() + 1;

        foreach (FootnoteDefinitionBlock block in doc.Blocks.OfType<FootnoteDefinitionBlock>())
        {
            if (numbers.ContainsKey(block.NormalizedLabel))
                continue;

            numbers[block.NormalizedLabel] = next++;
        }

        return numbers;
    }

    private InlineParseResult ApplyFootnotePresentation(InlineParseResult parsed)
        => PresentInlineWithFootnotes(parsed, prefixText: string.Empty, suffixRuns: null);

    private InlineParseResult ApplyFootnoteDefinitionPresentation(
        InlineParseResult parsed,
        FootnoteDefinitionBlock block,
        bool isFirstLine,
        bool isLastLine)
    {
        string prefixText = string.Empty;
        if (isFirstLine && _footnoteDisplayNumbers.TryGetValue(block.NormalizedLabel, out int number))
            prefixText = $"{number}. ";

        List<InlineRun>? suffixRuns = null;
        IReadOnlyList<MarkdownPosition> referencePositions = _footnoteIndex.GetReferencePositions(block.NormalizedLabel);
        if (isLastLine && referencePositions.Count > 0)
        {
            suffixRuns = [new InlineRun(" ", InlineStyle.None)];

            for (int i = 0; i < referencePositions.Count; i++)
            {
                if (i > 0)
                    suffixRuns.Add(new InlineRun(" ", InlineStyle.None));

                string label = i == 0 ? "↩" : $"↩{i + 1}";
                suffixRuns.Add(InlineRun.Link(
                    label,
                    MarkdownFootnoteHelper.BuildReferenceAnchor(block.NormalizedLabel, i + 1),
                    InlineStyle.None));
            }
        }

        return PresentInlineWithFootnotes(parsed, prefixText, suffixRuns);
    }

    private InlineParseResult PresentInlineWithFootnotes(
        InlineParseResult parsed,
        string prefixText,
        IReadOnlyList<InlineRun>? suffixRuns)
    {
        parsed ??= new InlineParseResult(string.Empty, Array.Empty<InlineRun>(), new[] { 0 }, new[] { 0 });

        int srcN = parsed.Text.Length;
        var sourceToVisual = new int[srcN + 1];
        var visualToSource = new List<int> { 0 };
        var runs = new List<InlineRun>();
        int outPos = 0;

        if (!string.IsNullOrEmpty(prefixText))
        {
            runs.Add(new InlineRun(prefixText, InlineStyle.None));
            for (int i = 0; i < prefixText.Length; i++)
            {
                outPos++;
                visualToSource.Add(0);
            }
        }

        sourceToVisual[0] = outPos;

        int srcCursor = 0;
        foreach (InlineRun run in parsed.Runs)
        {
            int oldLen = run.Text.Length;
            IReadOnlyList<InlineRun> presentedRuns = PresentRun(run);
            int newLen = presentedRuns.Sum(r => r.Text.Length);

            for (int offset = 0; offset <= oldLen; offset++)
            {
                sourceToVisual[srcCursor + offset] = outPos + ScaleBoundary(offset, oldLen, newLen);
            }

            foreach (InlineRun presentedRun in presentedRuns)
                runs.Add(presentedRun);

            for (int visualOffset = 1; visualOffset <= newLen; visualOffset++)
            {
                int srcOffset = oldLen == 0 ? 0 : ScaleBoundary(visualOffset, newLen, oldLen);
                visualToSource.Add(srcCursor + srcOffset);
            }

            srcCursor += oldLen;
            outPos += newLen;
        }

        int sourceEndVisual = outPos;
        sourceToVisual[srcN] = sourceEndVisual;

        if (suffixRuns is not null)
        {
            foreach (InlineRun suffixRun in suffixRuns)
            {
                if (string.IsNullOrEmpty(suffixRun.Text))
                    continue;

                runs.Add(suffixRun);
                for (int i = 0; i < suffixRun.Text.Length; i++)
                {
                    outPos++;
                    visualToSource.Add(srcN);
                }
            }
        }

        if (visualToSource.Count == 0)
            visualToSource.Add(srcN);
        else
            visualToSource[^1] = srcN;

        return new InlineParseResult(
            string.Concat(runs.Select(r => r.Text)),
            runs,
            visualToSource.ToArray(),
            sourceToVisual);
    }

    private IReadOnlyList<InlineRun> PresentRun(InlineRun run)
    {
        if (!run.IsFootnoteReference)
            return [run];

        if (!MarkdownFootnoteHelper.TryParseDefinitionAnchor(run.Href, out string normalizedLabel))
            return [run];

        if (!_footnoteDisplayNumbers.TryGetValue(normalizedLabel, out int number))
            return [run];

        return [InlineRun.FootnoteReference(number.ToString(), run.Href, run.Style)];
    }

    private static int ScaleBoundary(int offset, int fromLength, int toLength)
    {
        if (offset <= 0 || toLength <= 0)
            return 0;

        if (fromLength <= 0)
            return toLength;

        if (offset >= fromLength)
            return toLength;

        double scaled = offset * (double)toLength / fromLength;
        return Math.Clamp((int)Math.Round(scaled, MidpointRounding.AwayFromZero), 0, toLength);
    }

    private static bool SupportsInlineFormatting(MarkdownBlockKind kind) => kind switch
    {
        MarkdownBlockKind.Paragraph => true,
        MarkdownBlockKind.Heading => true,
        MarkdownBlockKind.Quote => true,
        MarkdownBlockKind.List => true,
        MarkdownBlockKind.FootnoteDefinition => true,
        _ => false
    };

    private static bool MightNeedInlineParsing(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (text.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return text.IndexOfAny(['`', '*', '_', '~', '[', '!', '\\']) >= 0;
    }

    private static IReadOnlyList<InlineRun> CreatePlainRuns(string text)
    {
        return string.IsNullOrEmpty(text)
            ? Array.Empty<InlineRun>()
            : new[] { new InlineRun(text, InlineStyle.None) };
    }

    private void BuildRealizedLinePresentation(
        int lineIdx,
        string source,
        LineSemantic sem,
        out VisualProjection proj,
        out IReadOnlyList<InlineRun> runs,
        out bool isImagePreview,
        out string imageAltText,
        out string imageSource)
    {
        bool forceRawThisLine = _forceRawLines.Contains(lineIdx);
        bool forceRawInlineThisLine = _forceRawInlineLines.Contains(lineIdx);
        bool isHorizontalRule = IsHorizontalRuleKind(sem.Kind);
        bool isAdmonitionMarkerLine =
            sem.Kind == MarkdownBlockKind.Quote &&
            sem.QuoteAdmonition != AdmonitionKind.None &&
            sem.IsAdmonitionMarkerLine;

        isImagePreview = false;
        imageAltText = string.Empty;
        imageSource = string.Empty;

        if (forceRawThisLine)
        {
            proj = CreateIdentityProjection(source);
            runs = CreatePlainRuns(proj.DisplayText);
            return;
        }

        if (isHorizontalRule)
        {
            proj = VisualProjection.HidePrefix(source, source.Length);
            runs = Array.Empty<InlineRun>();
            return;
        }

        if (sem.Kind == MarkdownBlockKind.CodeFence)
        {
            int spanIdx = _fenceSpanByLine[lineIdx];
            bool hasSpan = spanIdx >= 0;
            bool rawFence = false;

            if (hasSpan)
            {
                CodeFenceSpan span = _fenceSpans[spanIdx];
                rawFence = _forceRawCodeFenceStarts.Contains(span.StartLine);
            }

            if (!hasSpan || rawFence)
            {
                proj = CreateIdentityProjection(source);
            }
            else
            {
                CodeFenceSpan span = _fenceSpans[spanIdx];
                var hideRanges = new List<(int Start, int Length)>(2);

                if (lineIdx == span.StartLine && span.StartMarkerCol >= 0 && span.StartMarkerLen > 0)
                    hideRanges.Add((span.StartMarkerCol, span.StartMarkerLen));

                if (lineIdx == span.EndLine && span.EndMarkerCol >= 0 && span.EndMarkerLen > 0)
                    hideRanges.Add((span.EndMarkerCol, span.EndMarkerLen));

                proj = hideRanges.Count == 0
                    ? CreateIdentityProjection(source)
                    : BuildProjectionHidingRanges(source, hideRanges);
            }

            runs = CreatePlainRuns(proj.DisplayText);
            return;
        }

        if (isAdmonitionMarkerLine)
        {
            proj = VisualProjection.HidePrefix(source, source.Length);
            runs = Array.Empty<InlineRun>();
            return;
        }

        if (sem.Kind == MarkdownBlockKind.Image && _imageByLine.TryGetValue(lineIdx, out ImageBlock? image))
        {
            imageAltText = image.AltText;
            imageSource = image.Source;

            if (forceRawInlineThisLine)
            {
                proj = CreateIdentityProjection(source);
                runs = CreatePlainRuns(proj.DisplayText);
            }
            else
            {
                proj = VisualProjection.HidePrefix(source, source.Length);
                runs = Array.Empty<InlineRun>();
                isImagePreview = true;
            }

            return;
        }

        if (sem.Kind == MarkdownBlockKind.List && _listItemByLine.TryGetValue(lineIdx, out ListItem? listItem))
        {
            if (forceRawInlineThisLine)
            {
                proj = CreateIdentityProjection(source);
                runs = CreatePlainRuns(proj.DisplayText);
            }
            else
            {
                proj = BuildListProjection(source, listItem);
                if (MightNeedInlineParsing(proj.DisplayText))
                {
                    InlineParseResult inline = ApplyFootnotePresentation(ParseInlineCached(proj.DisplayText));
                    proj = ComposeProjection(proj, inline);
                    runs = inline.Runs;
                }
                else
                {
                    runs = CreatePlainRuns(proj.DisplayText);
                }
            }

            return;
        }

        if (sem.Kind == MarkdownBlockKind.FootnoteDefinition &&
            _footnoteLineBySource.TryGetValue(lineIdx, out FootnoteDefinitionLine? footnoteLine) &&
            _footnoteBlockBySourceLine.TryGetValue(lineIdx, out FootnoteDefinitionBlock? footnoteBlock))
        {
            if (forceRawThisLine)
            {
                proj = CreateIdentityProjection(source);
                runs = CreatePlainRuns(proj.DisplayText);
            }
            else
            {
                var hideRanges = new List<(int Start, int Length)>(1);
                if (footnoteLine.ContentStartColumn > 0)
                    hideRanges.Add((0, footnoteLine.ContentStartColumn));

                proj = hideRanges.Count == 0
                    ? CreateIdentityProjection(source)
                    : BuildProjectionHidingRanges(source, hideRanges);

                InlineParseResult inline = ApplyFootnoteDefinitionPresentation(
                    ParseInlineCached(proj.DisplayText),
                    footnoteBlock,
                    isFirstLine: footnoteLine.IsFirstLine,
                    isLastLine: lineIdx == footnoteBlock.EndLine);
                proj = ComposeProjection(proj, inline);
                runs = inline.Runs;
            }

            return;
        }

        VisualProjection prefixProj = ProjectionFactory.Build(sem.Kind, source);
        if (!SupportsInlineFormatting(sem.Kind) || forceRawInlineThisLine)
        {
            proj = prefixProj;
            runs = CreatePlainRuns(proj.DisplayText);
            return;
        }

        if (MightNeedInlineParsing(prefixProj.DisplayText))
        {
            InlineParseResult inline = ApplyFootnotePresentation(ParseInlineCached(prefixProj.DisplayText));
            proj = ComposeProjection(prefixProj, inline);
            runs = inline.Runs;
            return;
        }

        proj = prefixProj;
        runs = CreatePlainRuns(proj.DisplayText);
    }

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

        int contentStart = ResolveListContentStart(source, item);

        string indent = new string(' ', Math.Max(0, item.Level) * 2);
        string marker = item.MarkerKind == ListMarkerKind.Ordered
            ? $"{(item.OrderedNumber ?? 1)}. "
            : "• ";

        string taskGlyph = item.IsTask
            ? (item.IsChecked ? "☑ " : "☐ ")
            : string.Empty;

        string content = contentStart < n ? source[contentStart..] : string.Empty;
        string display = indent + marker + taskGlyph + content;

        int prefixLen = indent.Length + marker.Length + taskGlyph.Length;

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

    private static int ResolveListContentStart(string source, ListItem item)
    {
        source ??= string.Empty;
        int n = source.Length;

        int fallback = FindListContentStart(source);
        int candidate = fallback;

        if (item.ContentStartColumn >= 0 && item.ContentStartColumn <= n)
            candidate = item.ContentStartColumn;

        return Math.Clamp(candidate, 0, n);
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

    private static Size CalculateImagePreviewSize(
        string imageSource,
        Size viewport,
        int left,
        Func<string, Size?>? imageSizeProvider)
    {
        int availableWidth = Math.Max(120, viewport.Width - left - 24);
        int maxWidth = Math.Max(120, Math.Min(ImagePreviewMaxWidth, availableWidth));

        if (imageSizeProvider is not null && !string.IsNullOrWhiteSpace(imageSource))
        {
            Size? actual = imageSizeProvider(imageSource);
            if (actual is { Width: > 0, Height: > 0 })
            {
                float scale = Math.Min(
                    1f,
                    Math.Min(
                        maxWidth / (float)actual.Value.Width,
                        ImagePreviewMaxHeight / (float)actual.Value.Height));

                int width = Math.Max(1, (int)Math.Round(actual.Value.Width * scale));
                int height = Math.Max(1, (int)Math.Round(actual.Value.Height * scale));
                return new Size(width, height);
            }
        }

        return new Size(
            Math.Min(maxWidth, ImagePreviewPlaceholderWidth),
            ImagePreviewPlaceholderHeight);
    }

    private static int DistanceToY(Rectangle r, int y)
    {
        if (y < r.Top) return r.Top - y;
        if (y > r.Bottom) return y - r.Bottom;
        return 0;
    }

    private static int FindFirstVisibleIndex(int[] tops, int viewportTop)
    {
        if (tops.Length == 0)
            return 0;

        int idx = Array.BinarySearch(tops, viewportTop);
        if (idx < 0)
            idx = ~idx;

        return Math.Max(0, idx - 1);
    }

    private int FindNearestLineIndex(int y)
    {
        if (_lineTops.Length == 0)
            return 0;

        int idx = Array.BinarySearch(_lineTops, y);
        if (idx >= 0)
            return idx;

        idx = ~idx;
        if (idx <= 0)
            return 0;
        if (idx >= _lines.Count)
            return _lines.Count - 1;

        int previous = idx - 1;
        int next = idx;

        int previousDist = DistanceToY(_lines[previous].Bounds, y);
        int nextDist = DistanceToY(_lines[next].Bounds, y);
        return previousDist <= nextDist ? previous : next;
    }

    private static int ColumnFromX(float[] visualOffsets, float localX)
    {
        if (visualOffsets.Length == 0 || localX <= 0)
            return 0;

        int idx = Array.BinarySearch(visualOffsets, localX);
        if (idx >= 0)
            return idx;

        idx = ~idx;
        if (idx <= 0)
            return 0;
        if (idx >= visualOffsets.Length)
            return visualOffsets.Length - 1;

        float left = visualOffsets[idx - 1];
        float right = visualOffsets[idx];
        return (localX - left) <= (right - localX) ? idx - 1 : idx;
    }

    private int MeasureInlineRunsContentHeight(IReadOnlyList<InlineRun> runs, Font baseFont, Graphics graphics)
    {
        int height = MeasureHeight(graphics, baseFont);

        foreach (InlineRun run in runs)
        {
            if (!run.IsImage)
                continue;

            height = Math.Max(height, InlineImageMetrics.CalculateSize(run.Source, _imageSizeProvider).Height);
        }

        return height;
    }

    private bool ShouldRealizeLineEagerly(int sourceLine, int topY, Size viewport, int preferredSourceLine)
    {
        const int eagerViewportPadding = 1024;
        const int eagerLineRadius = 160;

        return topY <= viewport.Height + eagerViewportPadding
            || Math.Abs(sourceLine - preferredSourceLine) <= eagerLineRadius;
    }

    private void EnsureLineRealized(LayoutLine line)
    {
        if (line.IsRealized || _currentDoc is null)
            return;

        using var measurementGraphics = CreateMeasurementGraphics();

        LineSemantic sem = _semantics[line.SourceLine];
        string source = line.SourceText;

        if (sem.Kind == MarkdownBlockKind.Table)
        {
            sem.Kind = string.IsNullOrWhiteSpace(source)
                ? MarkdownBlockKind.Blank
                : MarkdownBlockKind.Paragraph;
            sem.HeadingLevel = 0;
        }

        Font measureFont = ResolveLineMeasureFont(line.Kind, line.HeadingLevel, line.FontRole, out bool disposeMeasureFont);
        try
        {
            BuildRealizedLinePresentation(
                line.SourceLine,
                source,
                sem,
                out VisualProjection projection,
                out IReadOnlyList<InlineRun> runs,
                out bool isImagePreview,
                out string imageAltText,
                out string imageSource);

            float[] visualOffsets = BuildVisualOffsets(projection.DisplayText, runs, measureFont, measurementGraphics, preferExactPlainTextMeasurement: line.Kind == MarkdownBlockKind.Heading);
            int lineHeight;
            int textWidth;

            if (isImagePreview)
            {
                Size imageSize = CalculateImagePreviewSize(imageSource, _lastViewport, line.TextX, _imageSizeProvider);
                textWidth = Math.Max(1, imageSize.Width);
                lineHeight = Math.Max(1, imageSize.Height + (ImagePreviewPaddingY * 2));
            }
            else
            {
                lineHeight = MeasureInlineRunsContentHeight(runs, measureFont, measurementGraphics) + 4;
                if (line.Kind == MarkdownBlockKind.Heading)
                    lineHeight += 2;
                else if (IsHorizontalRuleKind(line.Kind) && !_forceRawLines.Contains(line.SourceLine))
                    lineHeight = Math.Max(lineHeight, 14);

                textWidth = IsHorizontalRuleKind(line.Kind) && !_forceRawLines.Contains(line.SourceLine)
                    ? Math.Max(1, _lastViewport.Width - line.TextX - 12)
                    : Math.Max(1, (int)Math.Ceiling(GetVisualWidth(visualOffsets)));
            }

            ApplyRealizedLine(line, projection, runs, visualOffsets, textWidth, lineHeight);
            line.IsRealized = true;
        }
        finally
        {
            if (disposeMeasureFont)
                measureFont.Dispose();
        }
    }

    private void ApplyRealizedLine(
        LayoutLine line,
        VisualProjection projection,
        IReadOnlyList<InlineRun> runs,
        float[] visualOffsets,
        int textWidth,
        int lineHeight)
    {
        int oldHeight = line.Bounds.Height;
        int deltaHeight = lineHeight - oldHeight;

        line.Projection = projection;
        line.InlineRuns = runs;
        line.VisualOffsets = visualOffsets;
        line.TextWidth = textWidth;
        line.Bounds = new Rectangle(line.Bounds.X, line.Bounds.Y, Math.Max(1, textWidth), Math.Max(1, lineHeight));

        _maxContentWidth = Math.Max(_maxContentWidth, line.TextX + textWidth + 24);

        if (deltaHeight != 0 && _lineIndexBySource.TryGetValue(line.SourceLine, out int lineIndex))
        {
            for (int i = lineIndex + 1; i < _lines.Count; i++)
            {
                LayoutLine next = _lines[i];
                next.Bounds = new Rectangle(next.Bounds.X, next.Bounds.Y + deltaHeight, next.Bounds.Width, next.Bounds.Height);
                _lineTops[i] = next.Bounds.Top;
            }

            for (int i = 0; i < _tables.Count; i++)
            {
                TableLayout table = _tables[i];
                if (table.Bounds.Top <= line.Bounds.Top)
                    continue;

                table.ShiftY(deltaHeight);
                _tableTops[i] = table.Bounds.Top;
            }
        }

        if (_lineIndexBySource.TryGetValue(line.SourceLine, out int updatedIndex))
            _lineTops[updatedIndex] = line.Bounds.Top;

        int contentHeight = Math.Max(_lastViewport.Height, ContentSize.Height + deltaHeight);
        ContentSize = new Size(Math.Max(_lastViewport.Width, _maxContentWidth), contentHeight);
    }

    private bool CanFastMutateSimplePlainTextDocument()
    {
        if (_tables.Count != 0 ||
            _tableByStartLine.Count != 0 ||
            _tableSourceLines.Count != 0 ||
            _listItemByLine.Count != 0 ||
            _imageByLine.Count != 0 ||
            _footnoteLineBySource.Count != 0 ||
            _footnoteBlockBySourceLine.Count != 0 ||
            _fenceSpans.Count != 0 ||
            _forceRawCodeFenceStarts.Count != 0 ||
            _forceRawLines.Count != 0 ||
            _forceRawInlineLines.Count != 0)
        {
            return false;
        }

        if (_lines.Count != _sourceLineCount || _semantics.Length != _sourceLineCount)
            return false;

        foreach (LayoutLine line in _lines)
        {
            if (line.IsImagePreview ||
                line.IsTaskListItem ||
                line.IsAdmonitionMarkerLine ||
                line.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank))
            {
                return false;
            }
        }

        return true;
    }

    private LayoutLine CreateRealizedSimpleTextLine(int sourceLine, string sourceText, int topY, Graphics measurementGraphics)
    {
        const int left = 8;
        MarkdownBlockKind kind = string.IsNullOrWhiteSpace(sourceText)
            ? MarkdownBlockKind.Blank
            : MarkdownBlockKind.Paragraph;

        Font measureFont = ResolveLineMeasureFont(kind, 0, LineFontRole.Base, out bool disposeMeasureFont);
        try
        {
            VisualProjection projection = CreateIdentityProjection(sourceText);
            IReadOnlyList<InlineRun> runs = CreatePlainRuns(projection.DisplayText);
            float[] visualOffsets = BuildVisualOffsets(projection.DisplayText, runs, measureFont, measurementGraphics);
            int lineHeight = MeasureInlineRunsContentHeight(runs, measureFont, measurementGraphics) + 4;
            int textWidth = Math.Max(1, (int)Math.Ceiling(GetVisualWidth(visualOffsets)));

            return new LayoutLine
            {
                SourceLine = sourceLine,
                SourceText = sourceText,
                Projection = projection,
                VisualOffsets = visualOffsets,
                Bounds = new Rectangle(left, topY, Math.Max(1, textWidth), Math.Max(1, lineHeight)),
                TextX = left,
                TextWidth = textWidth,
                Kind = kind,
                HeadingLevel = 0,
                FontRole = LineFontRole.Base,
                InlineRuns = runs,
                IsRealized = true,
                IsImagePreview = false,
                ImageAltText = string.Empty,
                ImageSource = string.Empty,
                IsTaskListItem = false,
                IsTaskChecked = false,
                TaskMarkerSourceStart = -1,
                TaskMarkerSourceLength = 0,
                ListContentSourceStart = -1,
                QuoteAdmonition = AdmonitionKind.None,
                IsAdmonitionMarkerLine = false,
                QuoteStartLine = -1,
                QuoteEndLine = -1
            };
        }
        finally
        {
            if (disposeMeasureFont)
                measureFont.Dispose();
        }
    }

    private void RebuildSimplePlainTextSemantics()
    {
        _semantics = new LineSemantic[_sourceLineCount];

        for (int i = 0; i < _sourceLineCount; i++)
        {
            _semantics[i].Kind = string.IsNullOrWhiteSpace(_lines[i].SourceText)
                ? MarkdownBlockKind.Blank
                : MarkdownBlockKind.Paragraph;
            _semantics[i].HeadingLevel = 0;
            _semantics[i].IsCodeFenceStart = false;
            _semantics[i].IsCodeFenceEnd = false;
            _semantics[i].QuoteAdmonition = AdmonitionKind.None;
            _semantics[i].IsAdmonitionMarkerLine = false;
            _semantics[i].QuoteStartLine = -1;
            _semantics[i].QuoteEndLine = -1;
        }
    }

    private void RebuildLineIndexesAndMetrics()
    {
        _lineBySource.Clear();
        _lineIndexBySource.Clear();

        _lineTops = new int[_lines.Count];
        int maxWidth = 1;
        int contentBottom = 0;

        for (int i = 0; i < _lines.Count; i++)
        {
            LayoutLine line = _lines[i];
            _lineBySource[line.SourceLine] = line;
            _lineIndexBySource[line.SourceLine] = i;
            _lineTops[i] = line.Bounds.Top;

            maxWidth = Math.Max(maxWidth, line.TextX + line.TextWidth + 24);
            contentBottom = Math.Max(contentBottom, line.Bounds.Bottom);
        }

        _maxContentWidth = maxWidth;
        ContentSize = new Size(
            Math.Max(_lastViewport.Width, maxWidth),
            Math.Max(_lastViewport.Height, contentBottom + 6));
    }

    private bool ShouldMeasureVisualOffsetsEagerly(int sourceLine, int topY, Size viewport, int preferredSourceLine)
    {
        const int eagerViewportPadding = 1024;
        const int eagerLineRadius = 160;

        return topY <= viewport.Height + eagerViewportPadding
            || Math.Abs(sourceLine - preferredSourceLine) <= eagerLineRadius;
    }

    private float[] EnsureVisualOffsets(LayoutLine line)
    {
        EnsureLineRealized(line);

        if (line.VisualOffsets.Length > 0 || line.Projection.DisplayText.Length == 0)
            return line.VisualOffsets;

        using var measurementGraphics = CreateMeasurementGraphics();

        Font measureFont = ResolveLineMeasureFont(line.Kind, line.HeadingLevel, line.FontRole, out bool disposeMeasureFont);
        try
        {
            line.VisualOffsets = BuildVisualOffsets(line.Projection.DisplayText, line.InlineRuns, measureFont, measurementGraphics, preferExactPlainTextMeasurement: line.Kind == MarkdownBlockKind.Heading);
            return line.VisualOffsets;
        }
        finally
        {
            if (disposeMeasureFont)
                measureFont.Dispose();
        }
    }

    private float[] BuildVisualOffsets(string displayText, IReadOnlyList<InlineRun> runs, Font baseFont, Graphics graphics, bool preferExactPlainTextMeasurement = false)
    {
        int visualLength = displayText.Length;
        var offsets = new float[visualLength + 1];

        if (visualLength == 0)
            return offsets;

        if (IsSimplePlainTextLine(displayText, runs))
            return MeasurePrefixWidthsTextRenderer(graphics, displayText, baseFont, preferExactPlainTextMeasurement);

        if (runs.Count == 0)
        {
            offsets = MeasurePrefixWidths(graphics, displayText, baseFont);
            offsets[^1] = Math.Max(offsets[^1], MeasureRenderedLineWidth(displayText, runs, baseFont, graphics));
            return offsets;
        }

        int col = 0;
        float width = 0f;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (run.IsImage)
                {
                    col += Math.Max(1, run.VisualLength);
                    offsets[col] = width + InlineImageMetrics.CalculateSize(run.Source, _imageSizeProvider).Width;
                    width = offsets[col];
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text))
                    continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateRunFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference);

                if (isCode)
                    width += InlineCodeChipPadX;

                float[] runOffsets = MeasurePrefixWidths(graphics, run.Text, runFont);
                for (int i = 1; i < runOffsets.Length; i++)
                {
                    col++;
                    offsets[col] = width + runOffsets[i];
                }

                if (isCode)
                    offsets[col] += InlineCodeChipPadX;

                width = offsets[col];
            }
        }
        finally
        {
            foreach (var kv in cache)
                kv.Value.Dispose();
        }

        offsets[^1] = Math.Max(offsets[^1], MeasureRenderedLineWidth(displayText, runs, baseFont, graphics));
        return offsets;
    }

    private static bool IsSimplePlainTextLine(string displayText, IReadOnlyList<InlineRun> runs)
    {
        if (runs.Count != 1)
            return false;

        InlineRun run = runs[0];
        return !run.IsImage
            && !run.IsLink
            && !run.IsFootnoteReference
            && run.Style == InlineStyle.None
            && string.Equals(run.Text, displayText, StringComparison.Ordinal);
    }

    private static float GetVisualWidth(float[] offsets)
        => offsets.Length == 0 ? 0 : offsets[^1];

    private float MeasureRenderedLineWidth(string displayText, IReadOnlyList<InlineRun> runs, Font baseFont, Graphics graphics)
    {
        if (string.IsNullOrEmpty(displayText))
            return 0;

        if (runs.Count == 0)
            return Math.Max(
                MeasureAdvanceWidth(graphics, displayText, baseFont),
                MeasureInkWidth(graphics, displayText, baseFont));

        float width = 0f;
        var cache = new Dictionary<int, Font>();

        try
        {
            for (int i = 0; i < runs.Count; i++)
            {
                InlineRun run = runs[i];

                if (run.IsImage)
                {
                    width += InlineImageMetrics.CalculateSize(run.Source, _imageSizeProvider).Width;
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text))
                    continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateRunFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference);

                float advanceWidth = MeasureAdvanceWidth(graphics, run.Text, runFont);
                if (isCode)
                {
                    width += advanceWidth + InlineCodeChipPadX * 2;
                    continue;
                }

                if (i == runs.Count - 1)
                {
                    float inkWidth = MeasureInkWidth(graphics, run.Text, runFont);
                    width += Math.Max(advanceWidth, inkWidth);
                }
                else
                {
                    width += advanceWidth;
                }
            }
        }
        finally
        {
            foreach (var kv in cache)
                kv.Value.Dispose();
        }

        return width;
    }

    private Font GetOrCreateRunFont(Dictionary<int, Font> cache, Font baseFont, InlineStyle style, bool isCode, bool isFootnoteReference)
    {
        if (style == InlineStyle.None && !isFootnoteReference && !isCode)
            return baseFont;

        InlineStyle normalized = style & ~InlineStyle.Code;
        int key = ((int)normalized & 0xFF)
                  | (isCode ? 0x100 : 0)
                  | (isFootnoteReference ? 0x200 : 0)
                  | (((int)baseFont.Style & 0xFF) << 9);

        if (cache.TryGetValue(key, out var f))
            return f;

        Font seed = isCode ? _monoFont : baseFont;
        f = CreateRunDisplayFont(seed, normalized, isFootnoteReference);
        cache[key] = f;
        return f;
    }

    private static Font CreateRunDisplayFont(Font seed, InlineStyle normalized, bool isFootnoteReference)
    {
        Font styled = InlineMarkdown.CreateStyledFont(seed, normalized);
        if (!isFootnoteReference)
            return styled;

        try
        {
            return new Font(
                styled.FontFamily,
                Math.Max(1f, styled.Size * FootnoteFontScale),
                styled.Style,
                styled.Unit,
                styled.GdiCharSet,
                styled.GdiVerticalFont);
        }
        finally
        {
            styled.Dispose();
        }
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

    private static int MeasureWidth(Graphics graphics, string text, Font font)
        => (int)Math.Ceiling(MeasureAdvanceWidth(graphics, text, font));

    private static float MeasureAdvanceWidth(Graphics graphics, string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        using StringFormat format = (StringFormat)MeasureStringFormat.Clone();
        format.SetMeasurableCharacterRanges([new CharacterRange(0, text.Length)]);

        Region[] regions = graphics.MeasureCharacterRanges(text, font, new RectangleF(0, 0, 10000, 1000), format);
        try
        {
            RectangleF bounds = regions[0].GetBounds(graphics);
            return bounds.Width;
        }
        finally
        {
            foreach (Region region in regions)
                region.Dispose();
        }
    }

    private static float[] MeasurePrefixWidths(Graphics graphics, string text, Font font)
    {
        var offsets = new float[text.Length + 1];
        if (text.Length == 0)
            return offsets;

        const int maxRangesPerBatch = 32;
        float layoutHeight = Math.Max(64f, font.GetHeight(graphics) * 4f);
        var layoutRect = new RectangleF(0, 0, 100000f, layoutHeight);

        // Prefix ranges (0..n) drift on longer lines with GDI+.
        // Measuring individual glyph boxes and rebuilding boundaries from left/right edges
        // tracks the actual painted text much more reliably.
        for (int batchStart = 0; batchStart < text.Length; batchStart += maxRangesPerBatch)
        {
            int count = Math.Min(maxRangesPerBatch, text.Length - batchStart);
            var ranges = new CharacterRange[count];
            for (int i = 0; i < count; i++)
                ranges[i] = new CharacterRange(batchStart + i, 1);

            using StringFormat format = (StringFormat)MeasureStringFormat.Clone();
            format.SetMeasurableCharacterRanges(ranges);

            Region[] regions = graphics.MeasureCharacterRanges(text, font, layoutRect, format);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int idx = batchStart + i;
                    RectangleF bounds = regions[i].GetBounds(graphics);

                    if (idx == 0)
                        offsets[0] = 0f;

                    offsets[idx] = Math.Max(offsets[idx], bounds.Left);
                    offsets[idx + 1] = Math.Max(offsets[idx + 1], bounds.Right);
                }
            }
            finally
            {
                foreach (Region region in regions)
                region.Dispose();
            }
        }

        for (int i = 1; i < offsets.Length; i++)
            offsets[i] = Math.Max(offsets[i], offsets[i - 1]);

        return offsets;
    }

    private static float[] MeasurePrefixWidthsTextRenderer(Graphics graphics, string text, Font font, bool preferExactLoop = false)
    {
        var offsets = new float[text.Length + 1];
        if (text.Length == 0)
            return offsets;

        if (!preferExactLoop && TryMeasurePrefixWidthsGdi(graphics, text, font, offsets))
            return offsets;

        Size proposed = new(100000, 1000);
        int anchorWidth = TextRenderer.MeasureText("|", font, proposed, PlainTextMeasureFlags).Width;
        offsets[0] = 0f;
        for (int i = 1; i <= text.Length; i++)
        {
            int width = TextRenderer.MeasureText("|" + text[..i], font, proposed, PlainTextMeasureFlags).Width - anchorWidth;
            offsets[i] = Math.Max(offsets[i - 1], width);
        }

        return offsets;
    }

    private static bool TryMeasurePrefixWidthsGdi(Graphics graphics, string text, Font font, float[] offsets)
    {
        IntPtr hdc = IntPtr.Zero;
        IntPtr hfont = IntPtr.Zero;
        IntPtr oldFont = IntPtr.Zero;

        try
        {
            hdc = graphics.GetHdc();
            hfont = font.ToHfont();
            oldFont = SelectObject(hdc, hfont);

            if (oldFont == IntPtr.Zero || oldFont == new IntPtr(-1))
                return false;

            var extents = new int[text.Length];
            if (!GetTextExtentExPointW(hdc, text, text.Length, int.MaxValue, out _, extents, out _))
                return false;

            offsets[0] = 0f;
            for (int i = 1; i <= text.Length; i++)
                offsets[i] = Math.Max(offsets[i - 1], extents[i - 1]);

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (hdc != IntPtr.Zero)
            {
                if (oldFont != IntPtr.Zero && oldFont != new IntPtr(-1))
                    SelectObject(hdc, oldFont);

                graphics.ReleaseHdc(hdc);
            }

            if (hfont != IntPtr.Zero)
                DeleteObject(hfont);
        }
    }

    private static float MeasureInkWidth(Graphics graphics, string text, Font font)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        using var path = new GraphicsPath();
        float emSize = font.SizeInPoints * graphics.DpiY / 72f;
        path.AddString(
            text,
            font.FontFamily,
            (int)font.Style,
            emSize,
            PointF.Empty,
            StringFormat.GenericTypographic);

        RectangleF bounds = path.GetBounds();
        return bounds.Width;
    }

    private static int MeasureHeight(Graphics graphics, Font font)
        => (int)Math.Ceiling(font.GetHeight(graphics));

    private static float MeasureAverageCharWidth(Graphics graphics, Font font)
    {
        const string sample = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        float width = MeasureAdvanceWidth(graphics, sample, font);
        return width / sample.Length;
    }

    private static int EstimateTextWidth(string displayText, float avgCharWidth)
    {
        if (string.IsNullOrEmpty(displayText))
            return 1;

        return (int)Math.Ceiling(displayText.Length * avgCharWidth) + 8;
    }

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

            sem[i].QuoteAdmonition = AdmonitionKind.None;
            sem[i].IsAdmonitionMarkerLine = false;
            sem[i].QuoteStartLine = -1;
            sem[i].QuoteEndLine = -1;
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
                    {
                        int start = Math.Max(0, q.StartLine);
                        int end = Math.Min(n - 1, q.EndLine);

                        for (int i = start; i <= end; i++)
                        {
                            sem[i].Kind = MarkdownBlockKind.Quote;
                            sem[i].HeadingLevel = 0;

                            sem[i].QuoteAdmonition = q.Admonition;
                            sem[i].QuoteStartLine = q.StartLine;
                            sem[i].QuoteEndLine = q.EndLine;
                            sem[i].IsAdmonitionMarkerLine =
                                q.Admonition != AdmonitionKind.None &&
                                i == q.AdmonitionMarkerLine;
                        }
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

        public AdmonitionKind QuoteAdmonition;
        public bool IsAdmonitionMarkerLine;
        public int QuoteStartLine;
        public int QuoteEndLine;
    }
}
