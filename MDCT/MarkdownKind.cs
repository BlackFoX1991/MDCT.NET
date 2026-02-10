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
    HorizontalRule, // neu
    Table
}

public enum TableAlignment
{
    None,
    Left,
    Center,
    Right
}

public abstract record MarkdownBlock(int StartLine, int EndLine, MarkdownBlockKind Kind);

public sealed record BlankBlock(int Line)
    : MarkdownBlock(Line, Line, MarkdownBlockKind.Blank);

public sealed record ParagraphBlock(int StartLine, int EndLine)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Paragraph);

public sealed record HeadingBlock(int Line, int Level, string Text)
    : MarkdownBlock(Line, Line, MarkdownBlockKind.Heading);

public sealed record QuoteBlock(int StartLine, int EndLine)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Quote);

public sealed record ListBlock(int StartLine, int EndLine, bool Ordered)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.List);

public sealed record CodeFenceBlock(int StartLine, int EndLine, string Fence, string Language)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.CodeFence);

/// <summary>
/// Markdown Horizontal Rule (thematic break), z. B.:
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
    IReadOnlyList<TableRow> Rows,              // Header + Body rows (ohne Delimiter-Zeile)
    IReadOnlyList<TableAlignment> Alignments)  // pro Spalte
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.Table);
