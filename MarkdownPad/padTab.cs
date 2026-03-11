using System.ComponentModel;
using MarkdownGdi;

namespace MarkdownPad;

public sealed class padTab : TabPage
{
    private readonly PageCanvasPanel _pageCanvas = new();
    private readonly PageSurfacePanel _pageSurface = new();
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

        SuspendLayout();

        UseVisualStyleBackColor = false;
        Padding = Padding.Empty;

        _pageCanvas.Dock = DockStyle.Fill;
        _pageCanvas.Resize += PageCanvas_Resize;

        _pageSurface.Padding = new Padding(
            ScaleLogical(44),
            ScaleLogical(28),
            ScaleLogical(44),
            ScaleLogical(28));

        Editor.Dock = DockStyle.Fill;
        Editor.Margin = Padding.Empty;
        Editor.MarkdownChanged += Editor_MarkdownChanged;
        Editor.ThemeChanged += Editor_ThemeChanged;
        Editor.ThemeMode = themeMode;

        _pageSurface.Controls.Add(Editor);
        _pageCanvas.Controls.Add(_pageSurface);
        Controls.Add(_pageCanvas);

        ApplyEditorChrome();
        LayoutPageSurface();
        ResumeLayout(performLayout: true);
        UpdatePresentation();
    }

    public void LoadDocument(string markdown, string? filePath)
    {
        ApplyDocumentState(markdown, filePath, modified: false);
    }

    public void RestoreDocument(string markdown, string? filePath, bool modified)
    {
        ApplyDocumentState(markdown, filePath, modified);
    }

    public void MarkSaved(string? filePath = null)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
            FilePath = Path.GetFullPath(filePath);

        Editor.DocumentBasePath = FilePath is null ? null : Path.GetDirectoryName(FilePath);
        Modified = false;
        UpdatePresentation();
    }

    private void ApplyDocumentState(string markdown, string? filePath, bool modified)
    {
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath);
        Editor.LoadDocument(markdown, FilePath is null ? null : Path.GetDirectoryName(FilePath), resetUndoStacks: true);
        Modified = modified;
        UpdatePresentation();
    }

    public void ApplyTheme(EditorThemeMode themeMode)
    {
        Editor.ThemeMode = themeMode;
        ApplyEditorChrome();
        UpdatePresentation(raiseEvent: false);
    }

    private void Editor_ThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        ApplyEditorChrome();
    }

    private void PageCanvas_Resize(object? sender, EventArgs e)
    {
        LayoutPageSurface();
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

    private void LayoutPageSurface()
    {
        if (_pageCanvas.ClientSize.Width <= 0 || _pageCanvas.ClientSize.Height <= 0)
            return;

        int pageWidth = Math.Min(ScaleLogical(980), Math.Max(1, _pageCanvas.ClientSize.Width - ScaleLogical(12)));
        pageWidth = Math.Min(pageWidth, _pageCanvas.ClientSize.Width);

        int pageHeight = Math.Max(ScaleLogical(160), _pageCanvas.ClientSize.Height - ScaleLogical(24));
        pageHeight = Math.Min(pageHeight, _pageCanvas.ClientSize.Height);

        int x = Math.Max(0, (_pageCanvas.ClientSize.Width - pageWidth) / 2);
        int y = Math.Max(0, (_pageCanvas.ClientSize.Height - pageHeight) / 2);

        _pageSurface.Bounds = new Rectangle(x, y, pageWidth, pageHeight);
        _pageCanvas.PageBounds = _pageSurface.Bounds;
    }

    private void ApplyEditorChrome()
    {
        bool darkTheme = Editor.IsDarkTheme;

        Color canvasColor = darkTheme
            ? Color.FromArgb(34, 36, 41)
            : Color.FromArgb(236, 239, 244);

        Color borderColor = darkTheme
            ? Color.FromArgb(81, 87, 98)
            : Color.FromArgb(214, 220, 228);

        Color shadowColor = darkTheme
            ? Color.FromArgb(36, 0, 0, 0)
            : Color.FromArgb(26, 34, 41, 51);

        BackColor = canvasColor;
        _pageCanvas.BackColor = canvasColor;
        _pageCanvas.ShadowColor = shadowColor;
        _pageSurface.BackColor = Editor.BackColor;
        _pageSurface.BorderColor = borderColor;

        _pageCanvas.Invalidate();
        _pageSurface.Invalidate();
    }

    private int ScaleLogical(int value) => LogicalToDeviceUnits(value);
}
