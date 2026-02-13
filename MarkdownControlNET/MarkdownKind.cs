using System.Collections.Generic;

namespace MarkdownGdi;

public enum MarkdownBlockKind
{
    Blank,
    Paragraph,
    Heading,
    Quote,
    List,
    CodeFence,
    HorizontalRule,
    Table
}

/// <summary>
/// Extension for quote alerts/admonitions (e.g. [!NOTE]).
/// </summary>
public enum AdmonitionKind
{
    None,
    Note,
    Tip,
    Important,
    Warning,
    Caution
}

public enum TableAlignment
{
    None,
    Left,
    Center,
    Right
}

/// <summary>
/// Marker type for list items.
/// </summary>
public enum ListMarkerKind
{
    Unordered, // -, *, +
    Ordered    // 1., 2., ...
}

public abstract record MarkdownBlock(int StartLine, int EndLine, MarkdownBlockKind Kind);

public sealed record BlankBlock(int Line)
    : MarkdownBlock(Line, Line, MarkdownBlockKind.Blank);

public sealed record ParagraphBlock(int StartLine, int EndLine)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Paragraph);

public sealed record HeadingBlock(int Line, int Level, string Text)
    : MarkdownBlock(Line, Line, MarkdownBlockKind.Heading);

/// <summary>
/// Quote block, optionally as admonition (GitHub style):
/// > [!NOTE]
/// > Text...
/// </summary>
/// <param name="StartLine">First source line of the quote block.</param>
/// <param name="EndLine">Last source line of the quote block.</param>
/// <param name="Admonition">Admonition kind; None = normal quote block.</param>
/// <param name="AdmonitionMarkerLine">Source line of marker (e.g. [!NOTE]); -1 if none.</param>
/// <param name="AdmonitionMarkerText">Original marker text without leading '&gt;' (e.g. "[!NOTE]").</param>
public sealed record QuoteBlock(
    int StartLine,
    int EndLine,
    AdmonitionKind Admonition = AdmonitionKind.None,
    int AdmonitionMarkerLine = -1,
    string AdmonitionMarkerText = "")
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Quote)
{
    public bool IsAdmonition => Admonition != AdmonitionKind.None;

    /// <summary>
    /// Default title for UI rendering.
    /// </summary>
    public string AdmonitionTitle => Admonition switch
    {
        AdmonitionKind.Note => "Note",
        AdmonitionKind.Tip => "Tip",
        AdmonitionKind.Important => "Important",
        AdmonitionKind.Warning => "Warning",
        AdmonitionKind.Caution => "Caution",
        _ => string.Empty
    };
}

/// <summary>
/// Single list item (including nested information and optional task metadata).
/// </summary>
/// <param name="SourceLine">Source line index of this list item.</param>
/// <param name="Indent">Indent width (leading spaces; tabs may be normalized before).</param>
/// <param name="Level">Nesting level (0 = top level).</param>
/// <param name="MarkerKind">Ordered or unordered marker kind.</param>
/// <param name="UnorderedMarker">Original unordered marker ('-', '*', '+'), else null.</param>
/// <param name="OrderedNumber">Parsed ordered number (e.g. 1), else null.</param>
/// <param name="Text">Item content text (without list marker and without task marker).</param>
/// <param name="IsTask">True if this list item is a GFM task item (e.g. - [ ] / - [x]).</param>
/// <param name="IsChecked">Task checked state; only meaningful when IsTask is true.</param>
/// <param name="TaskMarkerStartColumn">Source column of '[' in [ ] / [x], -1 if unavailable.</param>
/// <param name="TaskMarkerLength">Length of task marker token, typically 3 ("[ ]" / "[x]").</param>
/// <param name="ContentStartColumn">
/// Source column where item content starts (after list marker + spaces + optional task marker + trailing space).
/// -1 if unavailable.
/// </param>
public sealed record ListItem(
    int SourceLine,
    int Indent,
    int Level,
    ListMarkerKind MarkerKind,
    char? UnorderedMarker,
    int? OrderedNumber,
    string Text,
    bool IsTask = false,
    bool IsChecked = false,
    int TaskMarkerStartColumn = -1,
    int TaskMarkerLength = 0,
    int ContentStartColumn = -1)
{
    public bool HasTaskMarkerSpan => IsTask && TaskMarkerStartColumn >= 0 && TaskMarkerLength > 0;

    public string TaskMarkerText => !IsTask
        ? string.Empty
        : (IsChecked ? "[x]" : "[ ]");
}

/// <summary>
/// List block may be mixed/nested.
/// IsOrdered remains for legacy compatibility:
/// - true: all top-level items are ordered
/// - false: otherwise (mixed or unordered)
/// </summary>
public sealed record ListBlock(
    int StartLine,
    int EndLine,
    IReadOnlyList<ListItem> Items,
    bool IsOrdered = false)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.List)
{
    /// <summary>
    /// Legacy alias so old code using ".Ordered" keeps compiling.
    /// </summary>
    public bool Ordered => IsOrdered;
}

public sealed record CodeFenceBlock(int StartLine, int EndLine, string Fence, string Language)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.CodeFence);

/// <summary>
/// Markdown horizontal rule (thematic break), e.g.:
/// ---
/// ***
/// ___
/// </summary>
public sealed record HorizontalRuleBlock(int Line, string Marker)
    : MarkdownBlock(Line, Line, MarkdownBlockKind.HorizontalRule);

public sealed record TableRow(int SourceLine, IReadOnlyList<string> Cells);

public sealed record TableBlock(
    int StartLine,
    int EndLine,
    IReadOnlyList<TableRow> Rows,              // Header + body rows (without delimiter row)
    IReadOnlyList<TableAlignment> Alignments)  // per column
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Table);
