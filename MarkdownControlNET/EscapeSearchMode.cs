namespace MarkdownGdi;

public enum EscapeSearchMode
{
    Any,            // egal ob escaped oder nicht
    OnlyEscaped,    // nur Treffer auf escaped Zeichen (z. B. \*)
    OnlyUnescaped   // nur unescaped Treffer
}
