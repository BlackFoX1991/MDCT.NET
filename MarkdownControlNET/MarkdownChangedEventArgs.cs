namespace MarkdownGdi;

public sealed class MarkdownChangedEventArgs : EventArgs
{
    public string Markdown { get; }
    public MarkdownChangedEventArgs(string markdown) => Markdown = markdown;
}
