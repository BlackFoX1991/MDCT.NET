using System;

namespace MarkdownGdi;

public sealed class EditorState
{
    public MarkdownPosition Caret { get; private set; } = new(0, 0);
    public MarkdownPosition? Anchor { get; private set; }

    // Keeps the desired visual column while moving up/down repeatedly.
    private int? _preferredColumn;

    public bool HasSelection => Anchor.HasValue && Anchor.Value != Caret;

    public void Restore(MarkdownPosition caret, MarkdownPosition? anchor, DocumentModel doc)
    {
        Caret = doc.ClampPosition(caret);
        Anchor = anchor.HasValue ? doc.ClampPosition(anchor.Value) : null;

        NormalizeCollapsedSelection();
        _preferredColumn = null;
    }

    public void SetCaret(MarkdownPosition position, bool shift, DocumentModel doc)
    {
        position = doc.ClampPosition(position);

        if (shift)
        {
            Anchor ??= Caret; // anchor starts where selection begins
            Caret = position;
            NormalizeCollapsedSelection();
        }
        else
        {
            Caret = position;
            Anchor = null;
        }

        _preferredColumn = null;
    }

    public (MarkdownPosition Start, MarkdownPosition End) GetSelection()
    {
        if (!HasSelection) return (Caret, Caret);

        var a = Anchor!.Value;
        var b = Caret;
        return a <= b ? (a, b) : (b, a);
    }

    public string GetSelectedText(DocumentModel doc)
    {
        if (!HasSelection) return string.Empty;
        var (s, e) = GetSelection();
        return doc.GetText(s, e);
    }

    public bool DeleteSelection(DocumentModel doc)
    {
        if (!HasSelection) return false;

        var (s, e) = GetSelection();
        doc.DeleteRange(s, e);
        Caret = s;
        Anchor = null;
        _preferredColumn = null;
        return true;
    }

    public bool InsertText(DocumentModel doc, string text)
    {
        if (text is null) return false;

        DeleteSelection(doc);
        Caret = doc.InsertText(Caret, text);
        Anchor = null;
        _preferredColumn = null;
        return true;
    }

    public bool NewLine(DocumentModel doc)
    {
        DeleteSelection(doc);
        Caret = doc.InsertNewLine(Caret);
        Anchor = null;
        _preferredColumn = null;
        return true;
    }

    public bool Backspace(DocumentModel doc)
    {
        if (DeleteSelection(doc)) return true;

        var old = Caret;
        Caret = doc.Backspace(Caret);
        Anchor = null;
        _preferredColumn = null;
        return Caret != old;
    }

    public bool Delete(DocumentModel doc)
    {
        if (DeleteSelection(doc)) return true;

        var old = Caret;
        Caret = doc.DeleteForward(Caret);
        Anchor = null;
        _preferredColumn = null;
        return Caret != old;
    }

    public bool MoveLeft(DocumentModel doc, bool shift)
    {
        if (!shift && HasSelection)
        {
            var (s, _) = GetSelection();
            SetCaret(s, false, doc);
            return true;
        }

        MarkdownPosition target;
        if (Caret.Column > 0)
        {
            target = new MarkdownPosition(Caret.Line, Caret.Column - 1);
        }
        else if (Caret.Line > 0)
        {
            int prevLine = Caret.Line - 1;
            target = new MarkdownPosition(prevLine, doc.GetLineLength(prevLine));
        }
        else
        {
            return false;
        }

        SetCaret(target, shift, doc);
        return true;
    }

    public bool MoveRight(DocumentModel doc, bool shift)
    {
        if (!shift && HasSelection)
        {
            var (_, e) = GetSelection();
            SetCaret(e, false, doc);
            return true;
        }

        int len = doc.GetLineLength(Caret.Line);
        MarkdownPosition target;

        if (Caret.Column < len)
        {
            target = new MarkdownPosition(Caret.Line, Caret.Column + 1);
        }
        else if (Caret.Line < doc.LineCount - 1)
        {
            target = new MarkdownPosition(Caret.Line + 1, 0);
        }
        else
        {
            return false;
        }

        SetCaret(target, shift, doc);
        return true;
    }

    public bool MoveUp(DocumentModel doc, bool shift)
    {
        if (Caret.Line <= 0) return false;

        _preferredColumn ??= Caret.Column;

        int targetLine = Caret.Line - 1;
        int targetCol = Math.Min(_preferredColumn.Value, doc.GetLineLength(targetLine));

        SetCaretVertical(new MarkdownPosition(targetLine, targetCol), shift, doc);
        return true;
    }

    public bool MoveDown(DocumentModel doc, bool shift)
    {
        if (Caret.Line >= doc.LineCount - 1) return false;

        _preferredColumn ??= Caret.Column;

        int targetLine = Caret.Line + 1;
        int targetCol = Math.Min(_preferredColumn.Value, doc.GetLineLength(targetLine));

        SetCaretVertical(new MarkdownPosition(targetLine, targetCol), shift, doc);
        return true;
    }

    // Backward-compatible overload (old call sites still compile)
    public bool MoveHome(DocumentModel doc, bool shift)
        => MoveHome(doc, shift, null);

    /// <summary>
    /// Move caret to visual line start (first visible text column) and toggle with true source line start (column 0).
    /// Typical behavior:
    /// - from middle -> visual start
    /// - from visual start -> column 0
    /// - from column 0 -> visual start (if different)
    /// </summary>
    public bool MoveHome(DocumentModel doc, bool shift, Func<int, int>? getVisualStartSourceColumn)
    {
        int lineLen = doc.GetLineLength(Caret.Line);
        int visualStart = 0;

        if (getVisualStartSourceColumn is not null)
            visualStart = ClampColumn(getVisualStartSourceColumn(Caret.Line), lineLen);

        int targetCol;
        if (visualStart <= 0)
        {
            targetCol = 0;
        }
        else
        {
            if (Caret.Column == 0) targetCol = visualStart;
            else if (Caret.Column == visualStart) targetCol = 0;
            else targetCol = visualStart;
        }

        var target = new MarkdownPosition(Caret.Line, targetCol);
        if (target == Caret && !shift) return false;

        SetCaret(target, shift, doc);
        return true;
    }

    // Backward-compatible overload (old call sites still compile)
    public bool MoveEnd(DocumentModel doc, bool shift)
        => MoveEnd(doc, shift, null);

    /// <summary>
    /// Move caret to visual line end and optionally toggle with true source line end if they differ.
    /// (Currently mostly equal for prefix-only projections, but this is future-proof.)
    /// </summary>
    public bool MoveEnd(DocumentModel doc, bool shift, Func<int, int>? getVisualEndSourceColumn)
    {
        int hardEnd = doc.GetLineLength(Caret.Line);
        int visualEnd = hardEnd;

        if (getVisualEndSourceColumn is not null)
            visualEnd = ClampColumn(getVisualEndSourceColumn(Caret.Line), hardEnd);

        int targetCol;
        if (visualEnd == hardEnd)
        {
            targetCol = hardEnd;
        }
        else
        {
            // symmetric toggle
            if (Caret.Column == visualEnd) targetCol = hardEnd;
            else targetCol = visualEnd;
        }

        var target = new MarkdownPosition(Caret.Line, targetCol);
        if (target == Caret && !shift) return false;

        SetCaret(target, shift, doc);
        return true;
    }

    public void SelectAll(DocumentModel doc)
    {
        int last = Math.Max(0, doc.LineCount - 1);
        Anchor = new MarkdownPosition(0, 0);
        Caret = new MarkdownPosition(last, doc.GetLineLength(last));
        NormalizeCollapsedSelection();
        _preferredColumn = null;
    }

    private void SetCaretVertical(MarkdownPosition position, bool shift, DocumentModel doc)
    {
        position = doc.ClampPosition(position);

        if (shift)
        {
            Anchor ??= Caret;
            Caret = position;
            NormalizeCollapsedSelection();
        }
        else
        {
            Caret = position;
            Anchor = null;
        }

        // Keep preferred column for further Up/Down steps.
        _preferredColumn ??= Caret.Column;
    }

    private static int ClampColumn(int column, int lineLength)
        => Math.Clamp(column, 0, Math.Max(0, lineLength));

    private void NormalizeCollapsedSelection()
    {
        if (Anchor == Caret)
            Anchor = null;
    }
}
