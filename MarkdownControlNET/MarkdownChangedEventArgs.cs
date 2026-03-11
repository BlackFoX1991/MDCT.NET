namespace MarkdownGdi;

public sealed class MarkdownChangedEventArgs : EventArgs
{
    private readonly Func<string>? _markdownFactory;
    private string? _markdown;

    public string Markdown => _markdown ??= _markdownFactory?.Invoke() ?? string.Empty;

    public MarkdownChangedEventArgs(string markdown)
    {
        _markdown = markdown ?? string.Empty;
    }

    public MarkdownChangedEventArgs(Func<string> markdownFactory)
    {
        _markdownFactory = markdownFactory ?? throw new ArgumentNullException(nameof(markdownFactory));
    }
}
