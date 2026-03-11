namespace MarkdownGdi;

public sealed class LinkActivatedEventArgs : EventArgs
{
    public LinkActivatedEventArgs(
        string displayText,
        string target,
        string resolvedTarget,
        string fragment,
        bool isWebLink,
        bool isMarkdownDocument,
        bool isCurrentDocument)
    {
        DisplayText = displayText ?? string.Empty;
        Target = target ?? string.Empty;
        ResolvedTarget = resolvedTarget ?? string.Empty;
        Fragment = fragment ?? string.Empty;
        IsWebLink = isWebLink;
        IsMarkdownDocument = isMarkdownDocument;
        IsCurrentDocument = isCurrentDocument;
    }

    public string DisplayText { get; }
    public string Target { get; }
    public string ResolvedTarget { get; }
    public string Fragment { get; }
    public bool IsWebLink { get; }
    public bool IsMarkdownDocument { get; }
    public bool IsCurrentDocument { get; }
    public bool HasFragment => !string.IsNullOrWhiteSpace(Fragment);
}
