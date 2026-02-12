using System;
using System.Collections.Generic;
using System.Linq;

namespace MarkdownGdi;

internal sealed class TableModel
{
    // Row 0 = Header
    public List<List<string>> Rows { get; } = new();
    public List<TableAlignment> Alignments { get; } = new();

    public int RowCount => Rows.Count;
    public int ColumnCount => Math.Max(
        Alignments.Count,
        Rows.Count == 0 ? 0 : Rows.Max(r => r.Count));

    public static TableModel FromBlock(TableBlock b)
    {
        var m = new TableModel();

        m.Alignments.AddRange(b.Alignments);

        foreach (var row in b.Rows)
            m.Rows.Add(row.Cells.Select(c => c ?? string.Empty).ToList());

        if (m.Rows.Count == 0)
            m.Rows.Add(new List<string>());

        m.Normalize();
        return m;
    }

    public void Normalize()
    {
        if (Rows.Count == 0)
            Rows.Add(new List<string>());

        int cols = Math.Max(1, ColumnCount);

        while (Alignments.Count < cols)
            Alignments.Add(TableAlignment.None);

        foreach (var row in Rows)
        {
            while (row.Count < cols)
                row.Add(string.Empty);
        }
    }

    public void AddBodyRow()
    {
        Normalize();
        int cols = ColumnCount;
        Rows.Add(Enumerable.Range(0, cols).Select(_ => string.Empty).ToList());
    }

    public IReadOnlyList<string> ToMarkdownLines()
    {
        Normalize();
        int cols = ColumnCount;
        var lines = new List<string>();

        string header = "| " + string.Join(" | ", Rows[0].Take(cols).Select(EscapeCell)) + " |";
        lines.Add(header);

        string delim = "| " + string.Join(" | ", Enumerable.Range(0, cols).Select(i => AlignmentToDelimiter(Alignments[i]))) + " |";
        lines.Add(delim);

        for (int r = 1; r < Rows.Count; r++)
        {
            string body = "| " + string.Join(" | ", Rows[r].Take(cols).Select(EscapeCell)) + " |";
            lines.Add(body);
        }

        return lines;
    }

    private static string EscapeCell(string s)
        => (s ?? string.Empty).Replace("|", "\\|");

    private static string AlignmentToDelimiter(TableAlignment a) => a switch
    {
        TableAlignment.Left => ":---",
        TableAlignment.Center => ":---:",
        TableAlignment.Right => "---:",
        _ => "---"
    };
}
