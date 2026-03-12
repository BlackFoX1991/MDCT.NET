namespace MarkdownGdi;

public sealed class ViewScaleRequestedEventArgs : EventArgs
{
    public ViewScaleRequestedEventArgs(int delta)
    {
        Delta = delta;
    }

    public int Delta { get; }
}
