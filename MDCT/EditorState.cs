using System;
using System.Collections.Generic;

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
            targetCol = (Caret.Column == visualEnd) ? hardEnd : visualEnd;
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

    // ------------------------------------------------------------
    // Optional helpers for list editing (Tab / Shift+Tab)
    // ------------------------------------------------------------

    /// <summary>
    /// Indent current line or all selected lines by inserting leading spaces.
    /// Returns true if document changed.
    /// </summary>
    public bool IndentLines(DocumentModel doc, int spaces = 2)
    {
        spaces = Math.Max(1, spaces);

        if (!TryGetAffectedLineRange(doc, out int lineStart, out int lineEnd))
            return false;

        string prefix = new(' ', spaces);

        for (int line = lineStart; line <= lineEnd; line++)
            doc.InsertText(new MarkdownPosition(line, 0), prefix);

        if (HasSelection)
        {
            var (s, e) = GetSelection();

            // Shift anchor/caret only for affected lines.
            MarkdownPosition shiftedA = ShiftColumnIfAffected(s, lineStart, lineEnd, +spaces);
            MarkdownPosition shiftedB = ShiftColumnIfAffected(e, lineStart, lineEnd, +spaces);

            Anchor = doc.ClampPosition(shiftedA);
            Caret = doc.ClampPosition(shiftedB);
            NormalizeCollapsedSelection();
        }
        else
        {
            Caret = doc.ClampPosition(new MarkdownPosition(Caret.Line, Caret.Column + spaces));
        }

        _preferredColumn = null;
        return true;
    }

    /// <summary>
    /// Unindent current line or all selected lines by removing up to 'spaces' leading spaces (or a leading tab).
    /// Returns true if document changed.
    /// </summary>
    public bool UnindentLines(DocumentModel doc, int spaces = 2)
    {
        spaces = Math.Max(1, spaces);

        if (!TryGetAffectedLineRange(doc, out int lineStart, out int lineEnd))
            return false;

        bool changed = false;
        var removedByLine = new Dictionary<int, int>();

        for (int line = lineStart; line <= lineEnd; line++)
        {
            string text = doc.GetLine(line);
            if (text.Length == 0) continue;

            int removeCount = CountRemovableIndent(text, spaces);
            if (removeCount <= 0) continue;

            doc.DeleteRange(new MarkdownPosition(line, 0), new MarkdownPosition(line, removeCount));
            removedByLine[line] = removeCount;
            changed = true;
        }

        if (!changed) return false;

        if (HasSelection)
        {
            var (s, e) = GetSelection();

            MarkdownPosition shiftedA = ShiftColumnWithPerLineRemoval(s, removedByLine);
            MarkdownPosition shiftedB = ShiftColumnWithPerLineRemoval(e, removedByLine);

            Anchor = doc.ClampPosition(shiftedA);
            Caret = doc.ClampPosition(shiftedB);
            NormalizeCollapsedSelection();
        }
        else
        {
            int removed = removedByLine.TryGetValue(Caret.Line, out int r) ? r : 0;
            Caret = doc.ClampPosition(new MarkdownPosition(Caret.Line, Math.Max(0, Caret.Column - removed)));
        }

        _preferredColumn = null;
        return true;
    }

    private static MarkdownPosition ShiftColumnIfAffected(MarkdownPosition pos, int lineStart, int lineEnd, int delta)
    {
        if (pos.Line < lineStart || pos.Line > lineEnd)
            return pos;

        int col = Math.Max(0, pos.Column + delta);
        return new MarkdownPosition(pos.Line, col);
    }

    private static MarkdownPosition ShiftColumnWithPerLineRemoval(
        MarkdownPosition pos,
        IReadOnlyDictionary<int, int> removedByLine)
    {
        if (!removedByLine.TryGetValue(pos.Line, out int removed) || removed <= 0)
            return pos;

        int col = Math.Max(0, pos.Column - removed);
        return new MarkdownPosition(pos.Line, col);
    }

    private static int CountRemovableIndent(string lineText, int maxSpaces)
    {
        if (string.IsNullOrEmpty(lineText)) return 0;

        // Prefer one tab as one indentation step
        if (lineText[0] == '\t')
            return 1;

        int count = 0;
        while (count < lineText.Length && count < maxSpaces && lineText[count] == ' ')
            count++;

        return count;
    }

    private bool TryGetAffectedLineRange(DocumentModel doc, out int startLine, out int endLine)
    {
        if (doc.LineCount <= 0)
        {
            startLine = endLine = 0;
            return false;
        }

        if (!HasSelection)
        {
            startLine = Math.Clamp(Caret.Line, 0, doc.LineCount - 1);
            endLine = startLine;
            return true;
        }

        var (s, e) = GetSelection();
        startLine = s.Line;
        endLine = e.Line;

        // If selection ends exactly at col 0, do not include that final line
        if (e.Column == 0 && endLine > startLine)
            endLine--;

        startLine = Math.Clamp(startLine, 0, doc.LineCount - 1);
        endLine = Math.Clamp(endLine, 0, doc.LineCount - 1);

        return endLine >= startLine;
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
