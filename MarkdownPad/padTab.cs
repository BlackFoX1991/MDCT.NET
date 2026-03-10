using System.ComponentModel;
using MarkdownGdi;

namespace MarkdownPad;

public sealed class padTab : TabPage
{
    private readonly string _defaultName;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public MarkdownGdiEditor Editor { get; } = new();

    public event EventHandler? DocumentStateChanged;

    public string? FilePath { get; private set; }

    public bool Modified { get; private set; }

    public string DocumentName => string.IsNullOrWhiteSpace(FilePath)
        ? _defaultName
        : Path.GetFileName(FilePath);

    public string DisplayName => Modified ? $"{DocumentName} *" : DocumentName;

    public bool IsUntitled => string.IsNullOrWhiteSpace(FilePath);

    public padTab(string defaultName, EditorThemeMode themeMode)
    {
        _defaultName = defaultName;

        Editor.Dock = DockStyle.Fill;
        Editor.MarkdownChanged += Editor_MarkdownChanged;
        Editor.ThemeMode = themeMode;

        Controls.Add(Editor);
        UpdatePresentation();
    }

    public void LoadDocument(string markdown, string? filePath)
    {
        Editor.Markdown = markdown;
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
        Modified = false;
        UpdatePresentation();
    }

    public void MarkSaved(string? filePath = null)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
            FilePath = Path.GetFullPath(filePath);

        Modified = false;
        UpdatePresentation();
    }

    public void ApplyTheme(EditorThemeMode themeMode)
    {
        Editor.ThemeMode = themeMode;
        UpdatePresentation(raiseEvent: false);
    }

    private void Editor_MarkdownChanged(object? sender, MarkdownChangedEventArgs e)
    {
        if (Modified)
            return;

        Modified = true;
        UpdatePresentation();
    }

    private void UpdatePresentation(bool raiseEvent = true)
    {
        Text = DisplayName;
        ToolTipText = FilePath ?? _defaultName;

        if (raiseEvent)
            DocumentStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
