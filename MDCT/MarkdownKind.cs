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
/// Erweiterung für Quote-Alerts/Admonitions (z.B. [!NOTE]).
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
/// Listentyp für einzelne Einträge.
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
/// Quote-Block, optional als Admonition (GitHub-Style):
/// > [!NOTE]
/// > Text...
/// </summary>
/// <param name="StartLine">Erste Quellzeile des Quote-Blocks.</param>
/// <param name="EndLine">Letzte Quellzeile des Quote-Blocks.</param>
/// <param name="Admonition">Art der Admonition; None = normaler Quote-Block.</param>
/// <param name="AdmonitionMarkerLine">Quellzeile des Markers (z.B. [!NOTE]); -1 wenn keiner.</param>
/// <param name="AdmonitionMarkerText">Original-Text des Markers ohne führendes '>' (z.B. "[!NOTE]").</param>
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
    /// Standard-Titel für die UI-Darstellung.
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
/// Ein einzelner Listeneintrag (inkl. Nested-Informationen).
/// </summary>
/// <param name="SourceLine">Quellzeile des Listeneintrags.</param>
/// <param name="Indent">Einrückung (Anzahl führender Spaces; Tabs ggf. vorher normalisieren).</param>
/// <param name="Level">Verschachtelungsebene (0 = top-level).</param>
/// <param name="MarkerKind">Ordered oder Unordered.</param>
/// <param name="UnorderedMarker">Originalmarker bei Unordered ('-', '*', '+'), sonst null.</param>
/// <param name="OrderedNumber">Aus Quelltext gelesene Nummer bei Ordered (z.B. 1), sonst null.</param>
/// <param name="Text">Inhaltstext des Eintrags (ohne Marker).</param>
public sealed record ListItem(
    int SourceLine,
    int Indent,
    int Level,
    ListMarkerKind MarkerKind,
    char? UnorderedMarker,
    int? OrderedNumber,
    string Text);

/// <summary>
/// Listenblock kann gemischt/nested sein.
/// IsOrdered bleibt für Legacy/Kompatibilität erhalten:
/// - true: alle Top-Level-Items ordered
/// - false: sonst (mixed oder unordered)
/// </summary>
public sealed record ListBlock(
    int StartLine,
    int EndLine,
    IReadOnlyList<ListItem> Items,
    bool IsOrdered = false)
    : MarkdownBlock(StartLine, EndLine, MarkdownBlockKind.List)
{
    /// <summary>
    /// Legacy-Alias, damit alter Code mit ".Ordered" weiter kompiliert.
    /// </summary>
    public bool Ordered => IsOrdered;
}

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
