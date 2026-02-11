namespace MarkdownGdi;

public sealed class VisualProjection
{
    public string DisplayText { get; }
    public int[] VisualToSource { get; } // vis col -> src col
    public int[] SourceToVisual { get; } // src col -> vis col

    private VisualProjection(string displayText, int[] visualToSource, int[] sourceToVisual)
    {
        DisplayText = displayText;
        VisualToSource = visualToSource;
        SourceToVisual = sourceToVisual;
    }

    public static VisualProjection Create(string displayText, int[] visualToSource, int[] sourceToVisual)
    {
        displayText ??= string.Empty;
        visualToSource ??= new[] { 0 };
        sourceToVisual ??= new[] { 0 };
        return new VisualProjection(displayText, visualToSource, sourceToVisual);
    }

    public static VisualProjection Identity(string source)
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

        return new VisualProjection(source, v2s, s2v);
    }

    public static VisualProjection HidePrefix(string source, int prefixLen)
    {
        source ??= string.Empty;

        prefixLen = Math.Clamp(prefixLen, 0, source.Length);
        string display = source[prefixLen..];

        int visN = display.Length;
        int srcN = source.Length;

        int[] v2s = new int[visN + 1];
        int[] s2v = new int[srcN + 1];

        for (int v = 0; v <= visN; v++)
            v2s[v] = v + prefixLen;

        for (int s = 0; s <= prefixLen; s++)
            s2v[s] = 0;

        for (int s = prefixLen + 1; s <= srcN; s++)
            s2v[s] = s - prefixLen;

        return new VisualProjection(display, v2s, s2v);
    }
}

internal static class ProjectionFactory
{
    public static VisualProjection Build(MarkdownBlockKind kind, string source)
    {
        source ??= string.Empty;
        if (source.Length == 0)
            return VisualProjection.Identity(source);

        if (kind == MarkdownBlockKind.Quote)
            return BuildQuoteProjection(source);

        if (kind == MarkdownBlockKind.Heading)
            return BuildHeadingProjection(source);

        // List handling is intentionally NOT done here anymore.
        // It is handled in LayoutEngine with parser-provided ListItem metadata
        // (ordered/unordered + nested level + stable mapping).
        return VisualProjection.Identity(source);
    }

    private static VisualProjection BuildQuoteProjection(string source)
    {
        int ws = LeadingWhitespace(source);
        if (ws < source.Length && source[ws] == '>')
        {
            int prefix = ws + 1;
            if (prefix < source.Length && source[prefix] == ' ')
                prefix++;

            return VisualProjection.HidePrefix(source, prefix);
        }

        return VisualProjection.Identity(source);
    }

    private static VisualProjection BuildHeadingProjection(string source)
    {
        int ws = LeadingWhitespace(source);
        int i = ws;

        while (i < source.Length && source[i] == '#')
            i++;

        if (i > ws && i < source.Length && source[i] == ' ')
            return VisualProjection.HidePrefix(source, i + 1);

        return VisualProjection.Identity(source);
    }

    private static int LeadingWhitespace(string s)
    {
        int i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i]))
            i++;

        return i;
    }
}
