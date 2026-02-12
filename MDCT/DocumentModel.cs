using System;
using System.Collections.Generic;

namespace MarkdownGdi;

public sealed class DocumentModel
{
    private readonly List<string> _lines = new() { string.Empty };

    public IReadOnlyList<string> Lines => _lines;
    public IReadOnlyList<MarkdownBlock> Blocks { get; private set; } = Array.Empty<MarkdownBlock>();
    public int LineCount => _lines.Count;

    public void LoadMarkdown(string markdown)
    {
        string normalized = NormalizeLineEndings(markdown ?? string.Empty);

        _lines.Clear();

        // Wichtig: Split behält bei trailing '\n' eine letzte leere Zeile,
        // was für den Editor korrekt ist.
        string[] split = normalized.Split('\n');
        if (split.Length == 0)
            _lines.Add(string.Empty);
        else
            _lines.AddRange(split);

        EnsureAtLeastOneLine();
        ReparseAll();
    }

    public string ToMarkdown() => string.Join('\n', _lines);

    public string GetLine(int line)
    {
        if (line < 0 || line >= _lines.Count)
            return string.Empty;

        return _lines[line];
    }

    public int GetLineLength(int line) => GetLine(line).Length;

    public MarkdownPosition ClampPosition(MarkdownPosition p)
    {
        EnsureAtLeastOneLine();

        int line = Math.Clamp(p.Line, 0, _lines.Count - 1);
        int col = Math.Clamp(p.Column, 0, _lines[line].Length);
        return new MarkdownPosition(line, col);
    }

    public MarkdownPosition InsertText(MarkdownPosition pos, string text)
    {
        pos = ClampPosition(pos);
        if (string.IsNullOrEmpty(text))
            return pos;

        string normalized = NormalizeLineEndings(text);

        string current = _lines[pos.Line];
        string left = current[..pos.Column];
        string right = current[pos.Column..];

        string[] parts = normalized.Split('\n');

        // Einzeilig
        if (parts.Length == 1)
        {
            _lines[pos.Line] = left + parts[0] + right;
            return new MarkdownPosition(pos.Line, pos.Column + parts[0].Length);
        }

        // Mehrzeilig
        _lines[pos.Line] = left + parts[0];
        int insertAt = pos.Line + 1;

        for (int i = 1; i < parts.Length - 1; i++)
            _lines.Insert(insertAt++, parts[i]);

        _lines.Insert(insertAt, parts[^1] + right);

        // Caret am Ende des eingefügten letzten Teils (vor ehem. right)
        return new MarkdownPosition(pos.Line + parts.Length - 1, parts[^1].Length);
    }

    public void DeleteRange(MarkdownPosition start, MarkdownPosition end)
    {
        start = ClampPosition(start);
        end = ClampPosition(end);

        if (end < start)
            (start, end) = (end, start);

        if (start == end)
            return;

        // Gleiche Zeile
        if (start.Line == end.Line)
        {
            string line = _lines[start.Line];
            _lines[start.Line] = line[..start.Column] + line[end.Column..];
            EnsureAtLeastOneLine();
            return;
        }

        // Mehrere Zeilen: Prefix der ersten + Suffix der letzten
        string first = _lines[start.Line];
        string last = _lines[end.Line];

        _lines[start.Line] = first[..start.Column] + last[end.Column..];

        // Entferne alle Zeilen zwischen start.Line+1 bis inkl. end.Line
        int removeCount = end.Line - start.Line;
        _lines.RemoveRange(start.Line + 1, removeCount);

        EnsureAtLeastOneLine();
    }

    public MarkdownPosition Backspace(MarkdownPosition caret)
    {
        caret = ClampPosition(caret);

        // Dokumentanfang
        if (caret.Line == 0 && caret.Column == 0)
            return caret;

        // Innerhalb Zeile
        if (caret.Column > 0)
        {
            var start = new MarkdownPosition(caret.Line, caret.Column - 1);
            DeleteRange(start, caret);
            return start;
        }

        // Zeilenmerge mit vorheriger Zeile
        int prevLine = caret.Line - 1;
        int prevCol = _lines[prevLine].Length;

        _lines[prevLine] += _lines[caret.Line];
        _lines.RemoveAt(caret.Line);

        EnsureAtLeastOneLine();
        return new MarkdownPosition(prevLine, prevCol);
    }

    public MarkdownPosition DeleteForward(MarkdownPosition caret)
    {
        caret = ClampPosition(caret);

        string line = _lines[caret.Line];

        // Zeichen rechts löschen
        if (caret.Column < line.Length)
        {
            var end = new MarkdownPosition(caret.Line, caret.Column + 1);
            DeleteRange(caret, end);
            return caret;
        }

        // Letzte Zeile -> nichts
        if (caret.Line >= _lines.Count - 1)
            return caret;

        // Merge mit nächster Zeile
        _lines[caret.Line] += _lines[caret.Line + 1];
        _lines.RemoveAt(caret.Line + 1);

        EnsureAtLeastOneLine();
        return caret;
    }

    public MarkdownPosition InsertNewLine(MarkdownPosition caret)
    {
        caret = ClampPosition(caret);

        string line = _lines[caret.Line];
        string left = line[..caret.Column];
        string right = line[caret.Column..];

        _lines[caret.Line] = left;
        _lines.Insert(caret.Line + 1, right);

        return new MarkdownPosition(caret.Line + 1, 0);
    }

    public string GetText(MarkdownPosition start, MarkdownPosition end)
    {
        start = ClampPosition(start);
        end = ClampPosition(end);

        if (end < start)
            (start, end) = (end, start);

        if (start == end)
            return string.Empty;

        // Gleiche Zeile
        if (start.Line == end.Line)
        {
            string line = _lines[start.Line];
            return line[start.Column..end.Column];
        }

        var parts = new List<string>
        {
            _lines[start.Line][start.Column..]
        };

        for (int i = start.Line + 1; i < end.Line; i++)
            parts.Add(_lines[i]);

        parts.Add(_lines[end.Line][..end.Column]);

        return string.Join('\n', parts);
    }

    /// <summary>
    /// Ersetzt Zeilenbereich [startLine..endLine] durch newLines.
    /// Unterstützt auch "Append" mit startLine == LineCount und endLine == startLine - 1.
    /// </summary>
    public void ReplaceLines(int startLine, int endLine, IReadOnlyList<string> newLines)
    {
        EnsureAtLeastOneLine();

        // Append-Modus: Insert am Ende (Index == Count), nichts entfernen.
        // Wichtig: startLine darf hier NICHT auf Count-1 geclamped werden.
        if (startLine >= _lines.Count)
        {
            startLine = _lines.Count;
            endLine = startLine - 1;
        }
        else
        {
            startLine = Math.Clamp(startLine, 0, _lines.Count - 1);
            endLine = Math.Clamp(endLine, startLine - 1, _lines.Count - 1);
        }

        // Nur entfernen, wenn Bereich gültig ist
        if (endLine >= startLine)
        {
            int removeCount = endLine - startLine + 1;
            _lines.RemoveRange(startLine, removeCount);
        }

        if (newLines is not null && newLines.Count > 0)
            _lines.InsertRange(startLine, newLines);
        else
            _lines.Insert(startLine, string.Empty);

        EnsureAtLeastOneLine();
    }

    public void ReparseDirtyBlocks() => ReparseAll();

    public void ReparseAll()
    {
        EnsureAtLeastOneLine();
        Blocks = MarkdownParser.Parse(_lines);
    }

    private void EnsureAtLeastOneLine()
    {
        if (_lines.Count == 0)
            _lines.Add(string.Empty);
    }

    private static string NormalizeLineEndings(string input)
        => input.Replace("\r\n", "\n").Replace('\r', '\n');
}
