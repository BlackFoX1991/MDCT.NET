namespace MarkdownGdi;

public sealed record FindOptions
{
    public bool WholeWord { get; init; } = false;
    public bool CaseSensitive { get; init; } = false;

    // Wenn true: Suchtext wie "\n", "\t", "\\" wird als Escape-Sequenz interpretiert
    public bool InterpretEscapeSequences { get; init; } = false;

    public EscapeSearchMode EscapeMode { get; init; } = EscapeSearchMode.Any;
    public bool WrapAround { get; init; } = true;
}
