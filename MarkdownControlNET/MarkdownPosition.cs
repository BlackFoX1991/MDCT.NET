namespace MarkdownGdi;

public readonly record struct MarkdownPosition(int Line, int Column) : IComparable<MarkdownPosition>
{
    public int CompareTo(MarkdownPosition other)
    {
        int c = Line.CompareTo(other.Line);
        return c != 0 ? c : Column.CompareTo(other.Column);
    }

    public static bool operator <(MarkdownPosition a, MarkdownPosition b) => a.CompareTo(b) < 0;
    public static bool operator >(MarkdownPosition a, MarkdownPosition b) => a.CompareTo(b) > 0;
    public static bool operator <=(MarkdownPosition a, MarkdownPosition b) => a.CompareTo(b) <= 0;
    public static bool operator >=(MarkdownPosition a, MarkdownPosition b) => a.CompareTo(b) >= 0;
}
