namespace MarkdownGdi;

public sealed class ThemeChangedEventArgs : EventArgs
{
    public ThemeChangedEventArgs(bool oldIsDarkTheme, bool newIsDarkTheme)
    {
        OldIsDarkTheme = oldIsDarkTheme;
        NewIsDarkTheme = newIsDarkTheme;
    }

    public bool OldIsDarkTheme { get; }
    public bool NewIsDarkTheme { get; }
}
