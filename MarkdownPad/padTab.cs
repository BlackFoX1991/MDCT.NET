using System.ComponentModel;
using MarkdownGdi;

namespace MarkdownPad;

public sealed class padTab : TabPage
{
    private const float DefaultViewScale = 1f;
    private const float MinViewScale = 0.75f;
    private const float MaxViewScale = 2.5f;
    private const int BasePageWidth = 980;
    private const int BaseMinPageHeight = 160;
    private const int BasePageHorizontalMargin = 12;
    private const int BasePageVerticalMargin = 24;
    private const int BasePagePaddingX = 44;
    private const int BasePagePaddingY = 28;

    private readonly PageCanvasPanel _pageCanvas = new();
    private readonly PageSurfacePanel _pageSurface = new();
    private readonly string _defaultName;
    private readonly Font _baseEditorFont;
    private Font? _scaledEditorFont;
    private float _viewScale = DefaultViewScale;

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ViewScale => _viewScale;

    public padTab(string defaultName, EditorThemeMode themeMode, float viewScale = DefaultViewScale)
    {
        _defaultName = defaultName;
        _baseEditorFont = (Font)Editor.Font.Clone();

        SuspendLayout();

        UseVisualStyleBackColor = false;
        Padding = Padding.Empty;

        _pageCanvas.AutoScroll = true;
        _pageCanvas.Dock = DockStyle.Fill;
        _pageCanvas.Resize += PageCanvas_Resize;

        Editor.Dock = DockStyle.Fill;
        //Editor.CanSideScroll = true;
        Editor.Margin = Padding.Empty;
        Editor.MarkdownChanged += Editor_MarkdownChanged;
        Editor.ThemeChanged += Editor_ThemeChanged;
        Editor.ThemeMode = themeMode;

        _pageSurface.Controls.Add(Editor);
        _pageCanvas.Controls.Add(_pageSurface);
        Controls.Add(_pageCanvas);

        ApplyViewScale(viewScale);
        ApplyEditorChrome();
        LayoutPageSurface();
        ResumeLayout(performLayout: true);
        UpdatePresentation();
    }

    protected override void Dispose(bool disposing)
    {
        Font? scaledEditorFont = null;

        if (disposing)
        {
            scaledEditorFont = _scaledEditorFont;
            _scaledEditorFont = null;
        }

        base.Dispose(disposing);

        if (!disposing)
            return;

        scaledEditorFont?.Dispose();
        _baseEditorFont.Dispose();
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

    public void ApplyViewScale(float viewScale)
    {
        float normalizedScale = NormalizeViewScale(viewScale);
        if (_scaledEditorFont is not null && Math.Abs(_viewScale - normalizedScale) < 0.001f)
            return;

        _viewScale = normalizedScale;

        _pageSurface.Padding = new Padding(
            ScaleViewLogical(BasePagePaddingX),
            ScaleViewLogical(BasePagePaddingY),
            ScaleViewLogical(BasePagePaddingX),
            ScaleViewLogical(BasePagePaddingY));

        ApplyScaledEditorFont();
        LayoutPageSurface();
        _pageCanvas.PerformLayout();
        _pageCanvas.Invalidate();
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

        int horizontalMargin = Math.Max(ScaleLogical(6), ScaleViewLogical(BasePageHorizontalMargin));
        int verticalMargin = Math.Max(ScaleLogical(12), ScaleLogical(BasePageVerticalMargin / 2));
        int pageWidth = Math.Max(1, ScaleViewLogical(BasePageWidth));

        bool requiresHorizontalScroll = pageWidth + horizontalMargin * 2 > _pageCanvas.ClientSize.Width;
        int reservedScrollHeight = requiresHorizontalScroll ? SystemInformation.HorizontalScrollBarHeight : 0;
        int availableHeight = Math.Max(1, _pageCanvas.ClientSize.Height - reservedScrollHeight);
        int pageHeight = Math.Max(ScaleViewLogical(BaseMinPageHeight), availableHeight - ScaleLogical(BasePageVerticalMargin));

        int contentWidth = Math.Max(_pageCanvas.ClientSize.Width, pageWidth + horizontalMargin * 2);
        int contentHeight = Math.Max(availableHeight, pageHeight + verticalMargin * 2);

        _pageCanvas.AutoScrollMinSize = new Size(contentWidth, contentHeight);

        Rectangle displayRectangle = _pageCanvas.DisplayRectangle;
        int x = displayRectangle.X + Math.Max(horizontalMargin, (contentWidth - pageWidth) / 2);
        int y = displayRectangle.Y + Math.Max(verticalMargin, (contentHeight - pageHeight) / 2);

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

    private void ApplyScaledEditorFont()
    {
        Font nextFont = CreateScaledFont(_baseEditorFont, _viewScale);
        Font? previousOwnedFont = _scaledEditorFont;

        _scaledEditorFont = nextFont;
        Editor.Font = nextFont;

        previousOwnedFont?.Dispose();
    }

    private int ScaleViewLogical(int value)
        => ScaleLogical(Math.Max(1, (int)Math.Round(value * _viewScale, MidpointRounding.AwayFromZero)));

    private static float NormalizeViewScale(float viewScale)
    {
        if (float.IsNaN(viewScale) || float.IsInfinity(viewScale))
            return DefaultViewScale;

        float rounded = (float)Math.Round(viewScale, 2, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, MinViewScale, MaxViewScale);
    }

    private static Font CreateScaledFont(Font font, float viewScale)
    {
        float scaledSize = Math.Max(1f, font.Size * viewScale);
        return new Font(
            font.FontFamily,
            scaledSize,
            font.Style,
            font.Unit,
            font.GdiCharSet,
            font.GdiVerticalFont);
    }
}
