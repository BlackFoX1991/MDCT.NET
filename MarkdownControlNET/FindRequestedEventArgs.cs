namespace MarkdownGdi;

public sealed class FindRequestedEventArgs : EventArgs
{
    public FindRequestedEventArgs(string? currentQuery, FindOptions currentOptions)
    {
        CurrentQuery = currentQuery;
        CurrentOptions = currentOptions;
    }

    public string? CurrentQuery { get; }
    public FindOptions CurrentOptions { get; }
}
