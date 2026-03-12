using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;



namespace MarkdownGdi;


public sealed class MarkdownGdiEditor : ScrollableControl, ISupportInitialize
{
    // ... deine bestehenden Felder ...

    private static readonly HttpClient ImageHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private bool _isInitializing;
    private bool _pendingInitialRefresh = true;

    // ... restliche Felder unverändert ...

    private readonly DocumentModel _doc = new();
    private readonly LayoutEngine _layout = new();
    private readonly EditorState _state = new();

    private static readonly Regex TaskMarkerRegex =
    new(@"\[(?<mark>[ xX])\]", RegexOptions.Compiled);

    private static readonly Regex QuotePrefixRegex =
        new(@"^(?<indent>\s*)>\s?", RegexOptions.Compiled);


    // Tables can be displayed non-destructively as raw source
    private readonly HashSet<int> _rawTableStartLines = new();
    private readonly Dictionary<int, Font> _headingFontCache = new();
    private readonly Dictionary<string, ImageCacheEntry> _imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _imageCacheSync = new();

    private readonly List<UndoRecord> _undo = new();
    private readonly List<UndoRecord> _redo = new();


    private readonly System.Windows.Forms.Timer _caretTimer;
    private bool _caretVisible = true;
    private bool _mouseSelecting;
    private int? _preferredCaretContentX;

    private Font _boldFont;
    private Font _monoFont;

    private TextBox? _cellEditor;
    private ActiveTableSession? _activeTable;
    private bool _suppressCellLostFocus;

    private bool _layoutContextInitialized;
    private int _layoutContextDocumentVersion = -1;
    private int? _layoutContextRawCodeFenceStart;
    private int? _layoutContextRawInlineLine;
    private int _layoutContextRawSourceStart = -1;
    private int _layoutContextRawSourceEnd = -1;
    private int[] _layoutContextRawTableStarts = Array.Empty<int>();

    private const int MaxUndo = 250;

    private int? _rawCodeFenceStartLine;

    private static readonly Regex TableDelimiterCellRegex =
        new(@"^:?-{3,}:?$", RegexOptions.Compiled);

    // Draft delimiter-only line (no body content)
    private static readonly Regex DelimiterDraftLineRegex =
        new(@"^\s*\|[\|\:\-\s]*\|?\s*$", RegexOptions.Compiled);


    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color SelectionColor
    {
        get => _selectionColor;
        set { _selectionColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color QuoteBarColor
    {
        get => _quoteBarColor;
        set { _quoteBarColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color CodeBackgroundColor
    {
        get => _codeBg;
        set { _codeBg = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color TableHeaderBackgroundColor
    {
        get => _tableHeaderBg;
        set { _tableHeaderBg = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color TableCellBackgroundColor
    {
        get => _tableCellBg;
        set { _tableCellBg = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color TableGridColor
    {
        get => _tableGrid;
        set { _tableGrid = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color InlineCodeBackgroundColor
    {
        get => _inlineCodeBg;
        set { _inlineCodeBg = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color InlineCodeBorderColor
    {
        get => _inlineCodeBorder;
        set { _inlineCodeBorder = value; Invalidate(); }
    }

    public void SetTheme(bool darkTheme)
    {
        AllowAutoThemeChange = false;
        ThemeMode = darkTheme ? EditorThemeMode.Dark : EditorThemeMode.Light;
    }

    private bool IsInDesigner
        => LicenseManager.UsageMode == LicenseUsageMode.Designtime || (Site?.DesignMode ?? false);

    private static bool TryReadWindowsAppsUseDarkTheme(out bool darkTheme)
    {
        darkTheme = false;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);

            object? v = key?.GetValue("AppsUseLightTheme");
            switch (v)
            {
                case int i:
                    darkTheme = i == 0;
                    return true;
                case long l:
                    darkTheme = l == 0L;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void RefreshSystemThemePreference()
    {
        if (TryReadWindowsAppsUseDarkTheme(out bool darkTheme))
        {
            _systemDarkThemeKnown = true;
            _systemDarkTheme = darkTheme;
        }
        else
        {
            _systemDarkThemeKnown = false;
            _systemDarkTheme = false;
        }
    }

    private void UpdateSystemThemeSubscription()
    {
        bool shouldHook = !IsDisposed
            && !IsInDesigner
            && AllowAutoThemeChange
            && _themeMode == EditorThemeMode.System;

        if (shouldHook && !_systemThemeEventsHooked)
        {
            try
            {
                SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
                _systemThemeEventsHooked = true;
            }
            catch
            {
                // ignore
            }
        }
        else if (!shouldHook && _systemThemeEventsHooked)
        {
            UnsubscribeSystemThemeEvents();
        }
    }

    private void UnsubscribeSystemThemeEvents()
    {
        if (!_systemThemeEventsHooked) return;

        try
        {
            SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
        }
        catch
        {
            // ignore
        }

        _systemThemeEventsHooked = false;
    }

    private void OnSystemUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (!AllowAutoThemeChange || IsDisposed || _themeMode != EditorThemeMode.System) return;

        if (e.Category is not (UserPreferenceCategory.General
                               or UserPreferenceCategory.Color
                               or UserPreferenceCategory.VisualStyle))
            return;

        if (!IsHandleCreated) return;

        void Apply()
        {
            RefreshSystemThemePreference();
            ApplyTheme(raiseEvent: true);
        }

        try
        {
            if (InvokeRequired)
                BeginInvoke((MethodInvoker)Apply);
            else
                Apply();
        }
        catch
        {
            // ignore (handle may be gone)
        }
    }



    private Color _selectionColor = Color.FromArgb(120, 51, 153, 255);
    private Color _quoteBarColor = Color.Silver;
    private Color _codeBg = Color.FromArgb(245, 245, 245);
    private Color _tableHeaderBg = Color.FromArgb(240, 244, 250);
    private Color _tableCellBg = Color.White;
    private Color _tableGrid = Color.Gainsboro;
    private Color _inlineCodeBg = Color.FromArgb(236, 240, 244);
    private Color _inlineCodeBorder = Color.FromArgb(210, 216, 224);
    private const int InlineCodePadX = 4;
    private const int InlineCodePadY = 1;
    private const int FootnoteRaiseY = 4;
    private const float FootnoteFontScale = 0.82f;

    private EditorThemeMode _themeMode = EditorThemeMode.System;
    private bool _isDarkThemeEffective;
    private bool _systemDarkThemeKnown;
    private bool _systemDarkTheme;

    private Color _hrColor = Color.Silver;
    private Color _tableOuterBorderColor = Color.Silver;
    private Color _tableDraftBg = Color.FromArgb(255, 252, 242);
    private Color _tableDraftBorder = Color.FromArgb(230, 190, 120);
    private Color _tableDraftHint = Color.DarkGoldenrod;
    private Color _cellEditorBack = Color.White;
    private Color _cellEditorFore = Color.Black;
    private Color _linkColor = Color.FromArgb(9, 105, 218);
    private Color _imagePlaceholderBack = Color.FromArgb(248, 249, 251);
    private Color _imagePlaceholderBorder = Color.FromArgb(214, 220, 228);
    private Color _imagePlaceholderText = Color.FromArgb(92, 101, 112);
    private bool _canSideScroll;
    private string? _documentBasePath;

    private const int ImagePreviewPaddingY = 8;
    private const int SvgRasterMaxWidth = 1600;
    private const int SvgRasterMaxHeight = 1600;

    private sealed class ImageCacheEntry
    {
        public Image? Image { get; set; }
        public bool IsLoading { get; set; }
        public string? Error { get; set; }
        public string DisplaySource { get; init; } = string.Empty;
    }

    private readonly record struct LinkHit(string DisplayText, string Target);


    [Category("Appearance")]
    [DefaultValue(EditorThemeMode.System)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public EditorThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value) return;
            _themeMode = value;
            if (_themeMode == EditorThemeMode.System)
                RefreshSystemThemePreference();

            UpdateSystemThemeSubscription();
            ApplyTheme();
        }
    }

    public void SetLightTheme() => ThemeMode = EditorThemeMode.Light;
    public void SetDarkTheme() => ThemeMode = EditorThemeMode.Dark;
    public void SetSystemTheme() => ThemeMode = EditorThemeMode.System;


    private void ApplyTheme(bool rebuildLayout = true, bool raiseEvent = true)
    {
        bool old = _isDarkThemeEffective;
        bool dark = ResolveEffectiveDarkMode();
        _isDarkThemeEffective = dark;

        if (dark)
            ApplyDarkPaletteCore();
        else
            ApplyLightPaletteCore();

        if (_cellEditor is not null)
        {
            _cellEditor.BackColor = _cellEditorBack;
            _cellEditor.ForeColor = _cellEditorFore;
        }

        if (!rebuildLayout)
        {
            Invalidate();
        }
        else if (!TryApplyPendingInitialRefresh())
        {
            RefreshLayoutForCaretContext(force: true);
            RepositionCellEditor();
            Invalidate();
        }

        if (raiseEvent && old != dark)
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(old, dark));
    }

    // --- Theme state ---
    private bool _allowAutoThemeChange = true;
    private bool _systemThemeEventsHooked;
    

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    [Category("Appearance")]
    [DefaultValue(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool AllowAutoThemeChange
    {
        get => _allowAutoThemeChange;
        set
        {
            if (_allowAutoThemeChange == value) return;

            _allowAutoThemeChange = value;

            UpdateSystemThemeSubscription();

            if (_allowAutoThemeChange && _themeMode == EditorThemeMode.System)
            {
                RefreshSystemThemePreference();
                ApplyTheme();
            }
        }
    }

    [Browsable(false)]
    public bool IsDarkTheme => _isDarkThemeEffective;

    [Category("Layout")]
    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool CanSideScroll
    {
        get => _canSideScroll;
        set
        {
            if (_canSideScroll == value)
                return;

            _canSideScroll = value;

            if (!IsHandleCreated)
                return;

            InvalidateLayoutContext();
            RefreshLayoutForCaretContext(force: true);

            if (!_canSideScroll)
            {
                int scrollY = Math.Max(0, -AutoScrollPosition.Y);
                AutoScrollPosition = new Point(0, scrollY);
            }

            RepositionCellEditor();
            Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? DocumentBasePath
    {
        get => _documentBasePath;
        set
        {
            string? normalized = NormalizeDocumentBasePath(value);

            if (string.Equals(_documentBasePath, normalized, StringComparison.OrdinalIgnoreCase))
                return;

            _documentBasePath = normalized;
            ClearImageCache();

            if (!IsHandleCreated)
                return;

            InvalidateLayoutContext();
            RefreshLayoutForCaretContext(force: true);
            RepositionCellEditor();
            Invalidate();
        }
    }

    private static string? NormalizeDocumentBasePath(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Path.GetFullPath(value);
    }


    private bool ResolveEffectiveDarkMode()
    {
        return _themeMode switch
        {
            EditorThemeMode.Dark => true,
            EditorThemeMode.Light => false,
            _ => _systemDarkThemeKnown
                ? _systemDarkTheme
                : IsDarkColor(Parent?.BackColor ?? SystemColors.Window)
        };
    }

    private static bool IsDarkColor(Color c)
    {
        // Relative luminance approximation
        double r = c.R / 255.0;
        double g = c.G / 255.0;
        double b = c.B / 255.0;
        double luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;
        return luma < 0.5;
    }

    private void ApplyLightPaletteCore()
    {
        BackColor = Color.White;
        ForeColor = Color.Black;

        _selectionColor = Color.FromArgb(120, 51, 153, 255);
        _quoteBarColor = Color.Silver;
        _codeBg = Color.FromArgb(245, 245, 245);
        _tableHeaderBg = Color.FromArgb(240, 244, 250);
        _tableCellBg = Color.White;
        _tableGrid = Color.Gainsboro;
        _inlineCodeBg = Color.FromArgb(236, 240, 244);
        _inlineCodeBorder = Color.FromArgb(210, 216, 224);

        _hrColor = Color.Silver;
        _tableOuterBorderColor = Color.Silver;
        _tableDraftBg = Color.FromArgb(255, 252, 242);
        _tableDraftBorder = Color.FromArgb(230, 190, 120);
        _tableDraftHint = Color.DarkGoldenrod;

        _cellEditorBack = Color.White;
        _cellEditorFore = Color.Black;
        _linkColor = Color.FromArgb(9, 105, 218);
        _imagePlaceholderBack = Color.FromArgb(248, 249, 251);
        _imagePlaceholderBorder = Color.FromArgb(214, 220, 228);
        _imagePlaceholderText = Color.FromArgb(92, 101, 112);
    }

    private void ApplyDarkPaletteCore()
    {
        BackColor = Color.FromArgb(30, 32, 36);
        ForeColor = Color.FromArgb(232, 235, 241);

        _selectionColor = Color.FromArgb(120, 76, 151, 255);
        _quoteBarColor = Color.FromArgb(118, 125, 136);
        _codeBg = Color.FromArgb(42, 45, 52);
        _tableHeaderBg = Color.FromArgb(52, 57, 66);
        _tableCellBg = Color.FromArgb(38, 41, 48);
        _tableGrid = Color.FromArgb(82, 88, 99);
        _inlineCodeBg = Color.FromArgb(56, 61, 72);
        _inlineCodeBorder = Color.FromArgb(98, 106, 120);

        _hrColor = Color.FromArgb(110, 116, 126);
        _tableOuterBorderColor = Color.FromArgb(132, 138, 148);
        _tableDraftBg = Color.FromArgb(62, 56, 44);
        _tableDraftBorder = Color.FromArgb(164, 136, 82);
        _tableDraftHint = Color.FromArgb(236, 198, 118);

        _cellEditorBack = Color.FromArgb(43, 47, 56);
        _cellEditorFore = ForeColor;
        _linkColor = Color.FromArgb(124, 184, 255);
        _imagePlaceholderBack = Color.FromArgb(39, 43, 50);
        _imagePlaceholderBorder = Color.FromArgb(82, 88, 99);
        _imagePlaceholderText = Color.FromArgb(185, 192, 203);
    }

    private void EnsureSystemThemeSync()
    {
        if (_themeMode != EditorThemeMode.System || !AllowAutoThemeChange)
            return;

        bool shouldBeDark = ResolveEffectiveDarkMode();
        if (shouldBeDark != _isDarkThemeEffective)
            ApplyTheme(rebuildLayout: false);
    }


    [Browsable(false)]
    public bool EffectiveDarkMode => _isDarkThemeEffective;


    private static readonly StringFormat DrawStringFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap,
        Trimming = StringTrimming.None,
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Near
    };

    private static readonly StringFormat MeasureStringFormat = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces,
        Trimming = StringTrimming.None,
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Near
    };

    private const TextFormatFlags PlainTextDrawFlags =
        TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine |
        TextFormatFlags.PreserveGraphicsClipping |
        TextFormatFlags.PreserveGraphicsTranslateTransform;

    private const TextFormatFlags PlainTextMeasureFlags =
        TextFormatFlags.NoPadding |
        TextFormatFlags.NoPrefix |
        TextFormatFlags.SingleLine;

    private readonly record struct AdmonitionPalette(
        Color Bar,
        Color Background,
        Color Border,
        Color TitleColor,
        string Icon);

    private AdmonitionPalette GetAdmonitionPalette(AdmonitionKind kind)
    {
        if (!_isDarkThemeEffective)
        {
            return kind switch
            {
                AdmonitionKind.Note => new(
                    Color.FromArgb(41, 128, 185),
                    Color.FromArgb(242, 248, 255),
                    Color.FromArgb(189, 222, 246),
                    Color.FromArgb(26, 83, 121),
                    "ⓘ"),
                AdmonitionKind.Tip => new(
                    Color.FromArgb(39, 174, 96),
                    Color.FromArgb(240, 252, 245),
                    Color.FromArgb(183, 232, 202),
                    Color.FromArgb(24, 108, 60),
                    "💡"),
                AdmonitionKind.Important => new(
                    Color.FromArgb(142, 68, 173),
                    Color.FromArgb(248, 242, 252),
                    Color.FromArgb(224, 201, 238),
                    Color.FromArgb(90, 40, 112),
                    "❗"),
                AdmonitionKind.Warning => new(
                    Color.FromArgb(243, 156, 18),
                    Color.FromArgb(255, 249, 236),
                    Color.FromArgb(245, 216, 164),
                    Color.FromArgb(140, 88, 10),
                    "⚠"),
                AdmonitionKind.Caution => new(
                    Color.FromArgb(231, 76, 60),
                    Color.FromArgb(255, 242, 240),
                    Color.FromArgb(244, 196, 190),
                    Color.FromArgb(138, 41, 31),
                    "⛔"),
                _ => new(Color.Silver, Color.White, Color.Gainsboro, Color.Gray, string.Empty)
            };
        }

        // Dark palettes
        return kind switch
        {
            AdmonitionKind.Note => new(
                Color.FromArgb(88, 166, 255),
                Color.FromArgb(34, 46, 61),
                Color.FromArgb(58, 94, 132),
                Color.FromArgb(180, 220, 255),
                "ⓘ"),
            AdmonitionKind.Tip => new(
                Color.FromArgb(76, 175, 122),
                Color.FromArgb(33, 52, 43),
                Color.FromArgb(55, 96, 76),
                Color.FromArgb(175, 230, 196),
                "💡"),
            AdmonitionKind.Important => new(
                Color.FromArgb(170, 124, 220),
                Color.FromArgb(50, 39, 63),
                Color.FromArgb(94, 72, 120),
                Color.FromArgb(220, 198, 244),
                "❗"),
            AdmonitionKind.Warning => new(
                Color.FromArgb(242, 176, 73),
                Color.FromArgb(61, 51, 34),
                Color.FromArgb(118, 95, 57),
                Color.FromArgb(255, 222, 160),
                "⚠"),
            AdmonitionKind.Caution => new(
                Color.FromArgb(232, 114, 102),
                Color.FromArgb(63, 39, 37),
                Color.FromArgb(120, 71, 66),
                Color.FromArgb(255, 196, 190),
                "⛔"),
            _ => new(
                Color.FromArgb(110, 118, 130),
                Color.FromArgb(36, 39, 46),
                Color.FromArgb(74, 79, 89),
                Color.FromArgb(220, 220, 220),
                string.Empty)
        };
    }

    public event EventHandler<MarkdownChangedEventArgs>? MarkdownChanged;

    [Category("Data")]
    [Localizable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(UITypeEditor))]
    public string Markdown
    {
        get => _doc.ToMarkdown();
        set => SetMarkdownCore(value, resetUndoStacks: true);
    }

    /// <summary>
    /// Hidden from designer/property grid. Keep Text mapped to Markdown for compatibility.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Bindable(false)]
    [AllowNull] // wichtig für CS8765 (Setter akzeptiert null wie im Basistyp)
    public override string Text
    {
        get => Markdown;
        set => Markdown = value ?? string.Empty;
    }


    public override void ResetText()
    {
        Markdown = string.Empty;
    }

    public void LoadDocument(string? markdown, string? documentBasePath, bool resetUndoStacks = true)
    {
        string? normalizedBasePath = NormalizeDocumentBasePath(documentBasePath);
        bool basePathChanged = !string.Equals(_documentBasePath, normalizedBasePath, StringComparison.OrdinalIgnoreCase);

        if (basePathChanged)
        {
            _documentBasePath = normalizedBasePath;
            ClearImageCache();
            InvalidateLayoutContext();
        }

        SetMarkdownCore(markdown, resetUndoStacks);
    }

    // verhindert Designer-Serialisierung von Text zusätzlich
    private bool ShouldSerializeText() => false;

    // Designer-Serialization-Helfer
    private bool ShouldSerializeMarkdown() => !string.IsNullOrEmpty(Markdown);
    private void ResetMarkdown() => Markdown = string.Empty;

    void ISupportInitialize.BeginInit()
    {
        _isInitializing = true;
    }

    // NEW: find state
    private string? _lastFindQuery;
    private FindOptions _lastFindOptions = new();
    private int _lastFindStartGlobal = -1;
    private int _lastFindLength = 0;

    // NEW: Host-UI kann darauf reagieren und eigenen Find-Dialog anzeigen
    public event EventHandler<FindRequestedEventArgs>? FindRequested;
    public event EventHandler<LinkActivatedEventArgs>? LinkActivated;
    public event EventHandler<ViewScaleRequestedEventArgs>? ViewScaleRequested;


    [Browsable(false)]
    public bool CanCopy => _cellEditor is not null ? _cellEditor.SelectionLength > 0 : _state.HasSelection;

    [Browsable(false)]
    public bool CanCut => CanCopy;

    [Browsable(false)]
    public bool CanPaste => TryClipboardContainsText();

    [Browsable(false)]
    public bool CanSelectAll => _cellEditor is not null ? _cellEditor.TextLength > 0 : _doc.LineCount > 0;

    [Browsable(false)]
    public bool CanUndo => _undo.Count > 0;

    [Browsable(false)]
    public bool CanRedo => _redo.Count > 0;

    [Browsable(false)]
    public bool CanFindNext => !string.IsNullOrEmpty(_lastFindQuery);

    [Browsable(false)]
    public MarkdownPosition CaretPosition => _state.Caret;

    [Browsable(false)]
    public bool HasSelection => _cellEditor is not null ? _cellEditor.SelectionLength > 0 : _state.HasSelection;

    [Browsable(false)]
    public string SelectedText => _cellEditor is not null
        ? _cellEditor.SelectedText
        : _state.GetSelectedText(_doc);

    public void SelectAllCommand()
    {
        if (_cellEditor is not null)
        {
            _cellEditor.SelectAll();
            return;
        }

        _state.SelectAll(_doc);
        RefreshLayoutForCaretContext();
        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    public void CopyCommand()
    {
        if (_cellEditor is not null)
        {
            if (_cellEditor.SelectionLength > 0)
                _cellEditor.Copy();
            return;
        }

        string sel = _state.GetSelectedText(_doc);
        if (!string.IsNullOrEmpty(sel))
            TryClipboardSetText(sel);
    }

    public void CutCommand()
    {
        if (_cellEditor is not null)
        {
            if (_cellEditor.SelectionLength > 0)
                _cellEditor.Cut();
            return;
        }

        if (!_state.HasSelection) return;

        string sel = _state.GetSelectedText(_doc);
        if (string.IsNullOrEmpty(sel)) return;
        if (!TryClipboardSetText(sel)) return; // wenn Clipboard gesperrt: nichts löschen

        ApplyDocumentEdit(() => _state.DeleteSelection(_doc));
    }

    public void PasteCommand()
    {
        if (_cellEditor is not null)
        {
            _cellEditor.Paste();
            return;
        }

        if (!TryClipboardGetText(out string text) || string.IsNullOrEmpty(text))
            return;

        ApplyDocumentEdit(() => _state.InsertText(_doc, text));
    }

    public void InsertMarkdownSnippetCommand(string markdown)
    {
        string normalized = markdown ?? string.Empty;
        if (string.IsNullOrEmpty(normalized))
            return;

        if (_cellEditor is not null)
        {
            _cellEditor.SelectedText = normalized;
            return;
        }

        ApplyDocumentEdit(() => _state.InsertText(_doc, normalized));
    }

    public bool NavigateToMarkdownAnchor(string anchor)
    {
        if (TryFindFootnoteDefinitionForAnchor(anchor, out FootnoteDefinitionBlock footnote))
        {
            FootnoteDefinitionLine targetLine = footnote.Lines.Count > 0
                ? footnote.Lines[0]
                : new FootnoteDefinitionLine(footnote.StartLine, 0, -1, 0, true);

            int sourceLine = Math.Clamp(targetLine.SourceLine, 0, _doc.LineCount - 1);
            string sourceText = _doc.GetLine(sourceLine);
            int targetColumn = Math.Clamp(targetLine.ContentStartColumn, 0, sourceText.Length);
            return NavigateToSourcePosition(sourceLine, targetColumn);
        }

        if (TryFindFootnoteReferenceForAnchor(anchor, out MarkdownPosition referencePosition))
            return NavigateToSourcePosition(referencePosition.Line, referencePosition.Column);

        return NavigateToHeadingAnchor(anchor);
    }

    public bool NavigateToHeadingAnchor(string anchor)
    {
        if (!TryFindHeadingForAnchor(anchor, out HeadingBlock heading))
            return false;

        string sourceLine = _doc.GetLine(heading.Line);
        int targetColumn = Math.Clamp(GetHeadingMarkerLength(sourceLine), 0, sourceLine.Length);
        return NavigateToSourcePosition(heading.Line, targetColumn);
    }

    private bool NavigateToSourcePosition(int line, int column)
    {
        _state.SetCaret(new MarkdownPosition(line, column), shift: false, _doc);
        RefreshLayoutForCaretContext(force: true);
        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
        return true;
    }

    /// <summary>
    /// Öffnet keinen Dialog selbst, sondern signalisiert dem Host (Form), dass Find angefordert wurde.
    /// </summary>
    public void RequestFind()
    {
        FindRequested?.Invoke(this, new FindRequestedEventArgs(_lastFindQuery, _lastFindOptions));
    }

    /// <summary>
    /// Startet eine neue Suche ab aktueller Caret-Position.
    /// </summary>
    public bool Find(string query, FindOptions? options = null)
    {
        options ??= new FindOptions();
        return FindCore(query, options, continueFromLastMatch: false);
    }

    /// <summary>
    /// Sucht den nächsten Treffer mit den letzten Suchparametern.
    /// </summary>
    public bool FindNext()
    {
        if (string.IsNullOrEmpty(_lastFindQuery))
            return false;

        return FindCore(_lastFindQuery, _lastFindOptions, continueFromLastMatch: true);
    }

    public void UndoCommand() => Undo();

    public void RedoCommand() => Redo();

    public void ApplyHeadingCommand(int level)
    {
        level = Math.Clamp(level, 1, 6);

        ApplyDocumentEdit(() =>
        {
            if (!TryGetAffectedLineRange(out int startLine, out int endLine))
                return false;

            bool hadSelection = _state.HasSelection;
            MarkdownPosition beforeCaret = _state.Caret;
            var originalLines = new List<string>();
            var updatedLines = new List<string>();
            bool changed = false;

            for (int line = startLine; line <= endLine; line++)
            {
                string original = _doc.GetLine(line);
                string updated = ApplyHeadingLevel(original, level);

                originalLines.Add(original);
                updatedLines.Add(updated);
                changed |= !string.Equals(original, updated, StringComparison.Ordinal);
            }

            if (!changed)
                return false;

            _doc.ReplaceLines(startLine, endLine, updatedLines);

            if (hadSelection)
            {
                SetSelectionToLineRange(startLine, updatedLines);
                return true;
            }

            int targetIndex = beforeCaret.Line - startLine;
            string originalTargetLine = originalLines[targetIndex];
            string updatedTargetLine = updatedLines[targetIndex];
            int originalContentStart = GetHeadingContentStart(originalTargetLine);
            int newPrefixLength = GetHeadingPrefixLength(updatedTargetLine);
            int newColumn = beforeCaret.Column <= originalContentStart
                ? newPrefixLength
                : newPrefixLength + (beforeCaret.Column - originalContentStart);

            _state.Restore(
                new MarkdownPosition(beforeCaret.Line, Math.Clamp(newColumn, 0, updatedTargetLine.Length)),
                null,
                _doc);

            return true;
        });
    }

    public void ToggleQuoteBlockCommand()
    {
        ApplyDocumentEdit(() =>
        {
            if (!TryGetAffectedLineRange(out int startLine, out int endLine))
                return false;

            bool hadSelection = _state.HasSelection;
            MarkdownPosition beforeCaret = _state.Caret;
            var originalLines = new List<string>();
            var updatedLines = new List<string>();

            for (int line = startLine; line <= endLine; line++)
                originalLines.Add(_doc.GetLine(line));

            bool removeQuote = originalLines.All(line => TryGetQuotePrefixRange(line, out _, out _));
            bool changed = false;

            foreach (string original in originalLines)
            {
                string updated = removeQuote
                    ? RemoveQuotePrefix(original)
                    : AddQuotePrefix(original);

                updatedLines.Add(updated);
                changed |= !string.Equals(original, updated, StringComparison.Ordinal);
            }

            if (!changed)
                return false;

            _doc.ReplaceLines(startLine, endLine, updatedLines);

            if (hadSelection)
            {
                SetSelectionToLineRange(startLine, updatedLines);
                return true;
            }

            int targetIndex = beforeCaret.Line - startLine;
            string originalTargetLine = originalLines[targetIndex];
            string updatedTargetLine = updatedLines[targetIndex];
            int newColumn = removeQuote
                ? AdjustColumnForQuoteRemoval(beforeCaret.Column, originalTargetLine)
                : AdjustColumnForQuoteAddition(beforeCaret.Column, originalTargetLine);

            _state.Restore(
                new MarkdownPosition(beforeCaret.Line, Math.Clamp(newColumn, 0, updatedTargetLine.Length)),
                null,
                _doc);

            return true;
        });
    }

    public void WrapSelectionInCodeFenceCommand()
    {
        ApplyDocumentEdit(() =>
        {
            if (!TryGetAffectedLineRange(out int startLine, out int endLine))
                return false;

            bool hadSelection = _state.HasSelection;
            MarkdownPosition beforeCaret = _state.Caret;
            var sourceLines = new List<string>();

            for (int line = startLine; line <= endLine; line++)
                sourceLines.Add(_doc.GetLine(line));

            string fence = CreateFenceMarker(string.Join('\n', sourceLines));
            var replacementLines = new List<string>(sourceLines.Count + 2) { fence };
            replacementLines.AddRange(sourceLines);
            replacementLines.Add(fence);

            _doc.ReplaceLines(startLine, endLine, replacementLines);

            if (hadSelection)
            {
                MarkdownPosition selectionStart = new(startLine + 1, 0);
                MarkdownPosition selectionEnd = new(startLine + sourceLines.Count, sourceLines[^1].Length);
                _state.Restore(selectionEnd, selectionStart, _doc);
                return true;
            }

            int targetIndex = beforeCaret.Line - startLine;
            int targetColumn = Math.Min(beforeCaret.Column, sourceLines[targetIndex].Length);
            _state.Restore(new MarkdownPosition(beforeCaret.Line + 1, targetColumn), null, _doc);
            return true;
        });
    }

    public void InsertTableCommand(string markdown)
    {
        string normalized = NormalizeMarkdownInsertion(markdown);
        if (string.IsNullOrEmpty(normalized))
            return;

        string[] tableLines = normalized.Split('\n');

        ApplyDocumentEdit(() =>
        {
            if (_state.HasSelection)
                return _state.InsertText(_doc, normalized);

            int lineIndex = _state.Caret.Line;
            if (string.IsNullOrWhiteSpace(_doc.GetLine(lineIndex)))
            {
                _doc.ReplaceLines(lineIndex, lineIndex, tableLines);
                _state.Restore(new MarkdownPosition(lineIndex, 0), null, _doc);
                return true;
            }

            var blockLines = new List<string>(tableLines.Length + 2) { string.Empty };
            blockLines.AddRange(tableLines);
            blockLines.Add(string.Empty);

            _doc.ReplaceLines(lineIndex + 1, lineIndex, blockLines);
            _state.Restore(new MarkdownPosition(lineIndex + 2, 0), null, _doc);
            return true;
        });
    }


    void ISupportInitialize.EndInit()
    {
        _isInitializing = false;
        _pendingInitialRefresh = true;
        TryApplyPendingInitialRefresh();
    }

    private void SetMarkdownCore(string? value, bool resetUndoStacks)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        _doc.LoadMarkdown(value ?? string.Empty);
        EnsureTrailingEditableLineAfterTerminalTable();

        _rawTableStartLines.Clear();
        _rawCodeFenceStartLine = null;

        _state.Restore(new MarkdownPosition(0, 0), null, _doc);

        if (resetUndoStacks)
        {
            _undo.Clear();
            _redo.Clear();
        }

        _pendingInitialRefresh = true;

        if (!_isInitializing)
        {
            if (!TryApplyPendingInitialRefresh())
                Invalidate();
        }
    }

    private bool CanApplyInitialLayout()
        => IsHandleCreated
           && Visible
           && ClientSize.Width > 0
           && ClientSize.Height > 0;

    private bool TryApplyPendingInitialRefresh()
    {
        if (!_pendingInitialRefresh) return false;
        if (_isInitializing) return false;
        if (!CanApplyInitialLayout()) return false;

        _pendingInitialRefresh = false;

        RefreshLayoutForCaretContext(force: true);
        RepositionCellEditor();
        Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);

        return true;
    }


    public MarkdownGdiEditor()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        DoubleBuffered = true;
        TabStop = true;
        AutoScroll = true;

        _boldFont = new Font(Font, FontStyle.Bold);
        _monoFont = CreateMonoFont(Font.Size);

        _doc.LoadMarkdown(string.Empty);
        _pendingInitialRefresh = true;

        if (!IsInDesigner)
            RefreshSystemThemePreference();

        ApplyTheme(rebuildLayout: false, raiseEvent: false);

        _caretTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _caretTimer.Tick += (_, _) =>
        {
            if (!Focused) return;
            _caretVisible = !_caretVisible;
            Invalidate();
        };

        Cursor = Cursors.IBeam;
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeSystemThemeEvents();

            _caretTimer.Dispose();
            _boldFont.Dispose();
            _monoFont.Dispose();
            ClearHeadingFontCache();
            ClearImageCache();
            _cellEditor?.Dispose();
        }

        base.Dispose(disposing);
    }


    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (!IsInDesigner)
        {
            if (_themeMode == EditorThemeMode.System)
                RefreshSystemThemePreference();

            UpdateSystemThemeSubscription();
        }

        ApplyTheme(rebuildLayout: false, raiseEvent: false);
        TryApplyPendingInitialRefresh();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (!RecreatingHandle)
            UnsubscribeSystemThemeEvents();

        base.OnHandleDestroyed(e);
    }


    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        TryApplyPendingInitialRefresh();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
            TryApplyPendingInitialRefresh();
    }


    protected override bool IsInputKey(Keys keyData)
    {
        Keys k = keyData & Keys.KeyCode;
        return k is Keys.Left or Keys.Right or Keys.Up or Keys.Down
            or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            or Keys.Tab or Keys.Enter
            || base.IsInputKey(keyData);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        _boldFont.Dispose();
        _monoFont.Dispose();
        ClearHeadingFontCache();

        _boldFont = new Font(Font, FontStyle.Bold);
        _monoFont = CreateMonoFont(Font.Size);

        if (!TryApplyPendingInitialRefresh())
        {
            RefreshLayoutForCaretContext(force: true);
            RepositionCellEditor();
            Invalidate();
        }
    }


    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (!TryApplyPendingInitialRefresh())
        {
            RefreshLayoutForCaretContext(force: true);
            RepositionCellEditor();
            Invalidate();
        }
    }


    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        RepositionCellEditor();
        Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ViewScaleRequested is not null && (ModifierKeys & Keys.Control) == Keys.Control)
        {
            ViewScaleRequested.Invoke(this, new ViewScaleRequestedEventArgs(e.Delta));

            if (e is HandledMouseEventArgs handledMouseEventArgs)
                handledMouseEventArgs.Handled = true;

            return;
        }

        base.OnMouseWheel(e);
        RepositionCellEditor();
        Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _caretVisible = true;
        _caretTimer.Start();
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);

        if (_cellEditor is null)
        {
            _caretTimer.Stop();
            _caretVisible = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        ResetPreferredCaretContentX();
        Focus();
        Point content = ClientToContent(e.Location);

        if (_cellEditor is not null)
            EndCellEdit(discard: false, move: CellMove.None);

        if (TryActivateLinkAtPoint(content))
        {
            _mouseSelecting = false;
            return;
        }

        if (_layout.TryHitTestTableCell(content, out var th))
        {
            _mouseSelecting = false;
            BeginTableCellEdit(th.Table, th.Row, th.Col);
            return;
        }

        // Task checkbox toggle (☐ / ☑) by click
        if (TryToggleTaskCheckboxAtPoint(content))
        {
            _mouseSelecting = false;
            return;
        }

        _mouseSelecting = true;
        var pos = _layout.HitTestText(content);
        bool shift = (ModifierKeys & Keys.Shift) != 0;

        _state.SetCaret(pos, shift, _doc);

        RefreshLayoutForCaretContext();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_cellEditor is null && !_mouseSelecting)
        {
            Point content = ClientToContent(e.Location);
            Cursor = IsPointOverTaskCheckbox(content) || IsPointOverImagePreview(content) || IsPointOverLink(content)
                ? Cursors.Hand
                : Cursors.IBeam;
        }

        if (!_mouseSelecting) return;
        if (_cellEditor is not null) return;

        var pos = _layout.HitTestText(ClientToContent(e.Location));
        _state.SetCaret(pos, shift: true, _doc);

        RefreshLayoutForCaretContext();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    private bool TryToggleTaskCheckboxAtPoint(Point contentPoint)
    {
        if (!TryBuildTaskCheckboxHit(contentPoint, out LayoutLine line, out _))
            return false;

        // Place caret near marker and clear selection before toggling
        int caretCol = line.TaskMarkerSourceStart >= 0 ? line.TaskMarkerSourceStart + 1 : 0;
        caretCol = Math.Clamp(caretCol, 0, _doc.GetLineLength(line.SourceLine));
        _state.SetCaret(new MarkdownPosition(line.SourceLine, caretCol), shift: false, _doc);

        ApplyDocumentEdit(() =>
            ToggleTaskMarkerOnLine(
                line.SourceLine,
                line.TaskMarkerSourceStart,
                line.TaskMarkerSourceLength));

        return true;
    }



    private bool IsPointOverTaskCheckbox(Point contentPoint)
    {
        return TryBuildTaskCheckboxHit(contentPoint, out _, out _);
    }

    private bool IsPointOverImagePreview(Point contentPoint)
    {
        var pos = _layout.HitTestText(contentPoint);
        LayoutLine? line = _layout.GetPreparedLine(pos.Line);
        if (line is null || !line.IsImagePreview)
            return false;

        return GetImagePreviewRect(line).Contains(contentPoint);
    }

    private bool IsPointOverLink(Point contentPoint)
        => TryBuildLinkHit(contentPoint, out _);

    private bool TryActivateLinkAtPoint(Point contentPoint)
    {
        if (LinkActivated is null || !TryBuildLinkActivation(contentPoint, out LinkActivatedEventArgs args))
            return false;

        LinkActivated.Invoke(this, args);
        return true;
    }

    private bool TryBuildLinkActivation(Point contentPoint, out LinkActivatedEventArgs args)
    {
        args = null!;

        if (!TryBuildLinkHit(contentPoint, out LinkHit hit))
            return false;

        if (!TryResolveLinkTarget(
            hit.Target,
            out string resolvedTarget,
            out string fragment,
            out bool isWebLink,
            out bool isMarkdownDocument,
            out bool isCurrentDocument))
        {
            return false;
        }

        args = new LinkActivatedEventArgs(
            hit.DisplayText,
            hit.Target,
            resolvedTarget,
            fragment,
            isWebLink,
            isMarkdownDocument,
            isCurrentDocument);
        return true;
    }

    private bool TryResolveLinkTarget(
        string target,
        out string resolvedTarget,
        out string fragment,
        out bool isWebLink,
        out bool isMarkdownDocument,
        out bool isCurrentDocument)
    {
        resolvedTarget = string.Empty;
        fragment = string.Empty;
        isWebLink = false;
        isMarkdownDocument = false;
        isCurrentDocument = false;

        string raw = (target ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (Uri.TryCreate(raw, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                resolvedTarget = absoluteUri.AbsoluteUri;
                isWebLink = true;
                return true;
            }

            if (absoluteUri.IsFile)
            {
                resolvedTarget = Path.GetFullPath(absoluteUri.LocalPath);
                fragment = absoluteUri.Fragment;
                isMarkdownDocument = IsMarkdownDocumentPath(resolvedTarget);
                return true;
            }
        }

        int fragmentIndex = raw.IndexOf('#');
        string rawPath = fragmentIndex >= 0 ? raw[..fragmentIndex] : raw;
        fragment = fragmentIndex >= 0 ? raw[fragmentIndex..] : string.Empty;

        if (string.IsNullOrWhiteSpace(rawPath))
        {
            if (string.IsNullOrWhiteSpace(fragment))
                return false;

            isMarkdownDocument = true;
            isCurrentDocument = true;
            return true;
        }

        string basePath = _documentBasePath ?? Environment.CurrentDirectory;
        string combined = Path.IsPathRooted(rawPath)
            ? rawPath
            : Path.Combine(basePath, rawPath);

        resolvedTarget = Path.GetFullPath(combined);
        isMarkdownDocument = IsMarkdownDocumentPath(resolvedTarget);
        return true;
    }

    private static bool IsMarkdownDocumentPath(string path)
        => string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase);

    private bool TryFindHeadingForAnchor(string anchor, out HeadingBlock heading)
    {
        heading = null!;

        if (!MarkdownAnchorHelper.TryParseHeadingAnchor(anchor, out int? requestedLevel, out string lookupText, out string lookupSlug))
            return false;

        MarkdownHeadingAnchor? fallbackHeading = null;

        foreach (MarkdownHeadingAnchor candidate in MarkdownAnchorHelper.BuildHeadingAnchors(_doc.Blocks.OfType<HeadingBlock>()))
        {
            string candidateText = MarkdownAnchorHelper.NormalizeHeadingText(candidate.Heading.Text);
            if (string.Equals(candidateText, lookupText, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Slug, lookupSlug, StringComparison.OrdinalIgnoreCase))
            {
                if (!requestedLevel.HasValue || candidate.Heading.Level == requestedLevel.Value)
                {
                    heading = candidate.Heading;
                    return true;
                }

                fallbackHeading ??= candidate;
            }
        }

        if (fallbackHeading.HasValue)
        {
            heading = fallbackHeading.Value.Heading;
            return true;
        }

        return false;
    }

    private bool TryFindFootnoteDefinitionForAnchor(string anchor, out FootnoteDefinitionBlock block)
    {
        block = null!;

        if (!MarkdownFootnoteHelper.TryParseDefinitionAnchor(anchor, out string normalizedLabel))
            return false;

        foreach (FootnoteDefinitionBlock candidate in _doc.Blocks.OfType<FootnoteDefinitionBlock>())
        {
            if (string.Equals(candidate.NormalizedLabel, normalizedLabel, StringComparison.OrdinalIgnoreCase))
            {
                block = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindFootnoteReferenceForAnchor(string anchor, out MarkdownPosition position)
    {
        position = default;

        if (!MarkdownFootnoteHelper.TryParseReferenceAnchor(anchor, out string normalizedLabel, out int occurrence))
            return false;

        MarkdownFootnoteIndex index = MarkdownFootnoteHelper.BuildIndex(_doc.Lines);
        return index.TryGetReferencePosition(normalizedLabel, occurrence, out position);
    }

    private bool TryBuildLinkHit(Point contentPoint, out LinkHit hit)
    {
        return TryBuildTableLinkHit(contentPoint, out hit) ||
               TryBuildLineLinkHit(contentPoint, out hit);
    }

    private bool TryBuildTableLinkHit(Point contentPoint, out LinkHit hit)
    {
        hit = default;

        if (!_layout.TryHitTestTableCell(contentPoint, out TableHit tableHit))
            return false;

        if (_rawTableStartLines.Contains(tableHit.Table.StartLine))
            return false;

        IReadOnlyList<InlineRun> runs = tableHit.Table.GetCellRuns(tableHit.Row, tableHit.Col);
        if (runs.Count == 0)
            return false;

        using var bmp = new Bitmap(1, 1);
        bmp.SetResolution(Math.Max(1f, DeviceDpi), Math.Max(1f, DeviceDpi));
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle rect = tableHit.Table.GetCellRect(tableHit.Row, tableHit.Col);
        Rectangle textRect = Rectangle.Inflate(rect, -8, -5);
        Font baseFont = tableHit.Row == 0 ? _boldFont : Font;

        int textWidth = MeasureInlineRunsWidthForTable(g, runs, baseFont);
        int x = textRect.Left;
        string align = tableHit.Table.GetColumnAlignment(tableHit.Col).ToString();

        if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
            x = textRect.Left + Math.Max(0, (textRect.Width - textWidth) / 2);
        else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
            x = textRect.Right - textWidth;

        x = Math.Max(textRect.Left, x);
        return TryHitLinkRun(contentPoint, runs, baseFont, x, textRect.Top, textRect.Height, out hit);
    }

    private bool TryBuildLineLinkHit(Point contentPoint, out LinkHit hit)
    {
        hit = default;

        MarkdownPosition pos = _layout.HitTestText(contentPoint);
        LayoutLine? line = _layout.GetPreparedLine(pos.Line);
        if (line is null || line.IsImagePreview)
            return false;

        if (contentPoint.Y < line.Bounds.Top || contentPoint.Y > line.Bounds.Bottom)
            return false;

        if (line.Segments.Count > 0)
        {
            foreach (LayoutSegment segment in line.Segments)
            {
                if (!segment.Bounds.Contains(contentPoint))
                    continue;

                return TryHitLinkRun(
                    contentPoint,
                    segment.InlineRuns,
                    GetRenderFont(line),
                    segment.Bounds.X,
                    segment.Bounds.Top,
                    segment.Bounds.Height,
                    out hit);
            }

            return false;
        }

        return TryHitLinkRun(
            contentPoint,
            line.InlineRuns,
            GetRenderFont(line),
            line.TextX,
            line.Bounds.Top,
            line.Bounds.Height,
            out hit);
    }

    private bool TryHitLinkRun(
        Point contentPoint,
        IReadOnlyList<InlineRun> runs,
        Font baseFont,
        int startX,
        int top,
        int height,
        out LinkHit hit)
    {
        hit = default;
        if (runs.Count == 0)
            return false;

        int x = startX;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (InlineRun run in runs)
            {
                int runWidth = MeasureInlineRunWidth(run, baseFont, cache);
                if (run.IsLink && runWidth > 0)
                {
                    Rectangle hitRect = new(x, top, runWidth, Math.Max(1, height));
                    if (hitRect.Contains(contentPoint))
                    {
                        hit = new LinkHit(run.Text, run.Href);
                        return true;
                    }
                }

                x += runWidth;
            }
        }
        finally
        {
            foreach (Font font in cache.Values)
                font.Dispose();
        }

        return false;
    }

    private int MeasureInlineRunWidth(InlineRun run, Font baseFont, Dictionary<int, Font> cache)
    {
        if (run.IsImage)
            return GetInlineImageSize(run.Source).Width;

        if (string.IsNullOrEmpty(run.Text))
            return 0;

        bool isCode = (run.Style & InlineStyle.Code) != 0;
        Font runFont = GetOrCreateInlineFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference, _monoFont);
        int width = MeasureWidth(run.Text, runFont);

        if (isCode)
            width += InlineCodePadX * 2;

        return width;
    }

    private bool TryBuildTaskCheckboxHit(Point contentPoint, out LayoutLine line, out Rectangle hitRect)
    {
        line = null!;
        hitRect = Rectangle.Empty;

        var pos = _layout.HitTestText(contentPoint);
        LayoutLine? candidate = _layout.GetPreparedLine(pos.Line);
        if (candidate is null) return false;

        if (contentPoint.Y < candidate.Bounds.Top - 1 || contentPoint.Y > candidate.Bounds.Bottom + 1)
            return false;

        if (!TryGetTaskCheckboxRect(candidate, out hitRect))
            return false;

        line = candidate;
        return hitRect.Contains(contentPoint);
    }

    private bool ToggleTaskMarkerOnLine(int sourceLine, int markerStart, int markerLength)
    {
        if (sourceLine < 0 || sourceLine >= _doc.LineCount)
            return false;

        string source = _doc.GetLine(sourceLine);
        if (string.IsNullOrEmpty(source))
            return false;

        if (TryToggleTaskMarkerAtKnownRange(source, markerStart, markerLength, out string updated))
        {
            _doc.ReplaceLines(sourceLine, sourceLine, new[] { updated });
            return true;
        }

        // Fallback if cached marker range is stale
        int idx = FindTaskMarkerInListSource(source);
        if (idx < 0 || idx + 2 >= source.Length)
            return false;

        char current = source[idx + 1];
        if (current != ' ' && current != 'x' && current != 'X')
            return false;

        char next = (current == 'x' || current == 'X') ? ' ' : 'x';
        string fallback = source[..(idx + 1)] + next + source[(idx + 2)..];

        if (string.Equals(fallback, source, StringComparison.Ordinal))
            return false;

        _doc.ReplaceLines(sourceLine, sourceLine, new[] { fallback });
        return true;
    }

    private static bool TryToggleTaskMarkerAtKnownRange(
        string source,
        int markerStart,
        int markerLength,
        out string updated)
    {
        updated = source;
        if (string.IsNullOrEmpty(source)) return false;
        if (markerLength < 3) return false;
        if (markerStart < 0 || markerStart + 2 >= source.Length) return false;

        if (source[markerStart] != '[' || source[markerStart + 2] != ']')
            return false;

        char current = source[markerStart + 1];
        if (current != ' ' && current != 'x' && current != 'X')
            return false;

        char next = (current == 'x' || current == 'X') ? ' ' : 'x';
        updated = source[..(markerStart + 1)] + next + source[(markerStart + 2)..];

        return !string.Equals(updated, source, StringComparison.Ordinal);
    }

    private static int FindTaskMarkerInListSource(string source)
    {
        if (string.IsNullOrEmpty(source)) return -1;
        Match m = TaskMarkerRegex.Match(source);
        return m.Success ? m.Index : -1;
    }


    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            _mouseSelecting = false;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left) return;

        Focus();
        Point content = ClientToContent(e.Location);

        if (_layout.TryHitTestTableCell(content, out var th))
            EnterRawTableSourceFromGrid(th.Table, th.Row, th.Col);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (_cellEditor is not null) return;
        if (char.IsControl(e.KeyChar)) return;

        ResetPreferredCaretContentX();

        if (e.KeyChar == '>' && IsCaretAtVisualStart())
        {
            ApplyDocumentEdit(() => _state.InsertText(_doc, "> "));
            e.Handled = true;
            return;
        }

        if (!TryApplyFastSingleLineInsert(e.KeyChar.ToString()))
            ApplyDocumentEdit(() => _state.InsertText(_doc, e.KeyChar.ToString()));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_cellEditor is not null) return;

        if (_cellEditor is null && e.KeyCode == Keys.F3 && !e.Control && !e.Alt)
        {
            FindNext();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }



        /*if (HandleShortcuts(e))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }*/

        if (e.Control && e.KeyCode == Keys.Enter)
        {
            if (HandleCtrlEnterExitRawTableMode())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }

        if (HandleQuoteStructuralKeys(e))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        bool shift = e.Shift;
        bool movedCaret = false;
        bool handled = true;
        bool keepPreferredCaretX = e.KeyCode is Keys.Up or Keys.Down;

        if (!keepPreferredCaretX)
            ResetPreferredCaretContentX();

        switch (e.KeyCode)
        {
            case Keys.Left:
                movedCaret = _state.MoveLeft(_doc, shift);
                break;
            case Keys.Right:
                movedCaret = _state.MoveRight(_doc, shift);
                break;
            case Keys.Up:
                movedCaret = MoveCaretVertically(-1, shift);
                break;
            case Keys.Down:
                movedCaret = MoveCaretVertically(+1, shift);
                break;
            case Keys.Home:
                movedCaret = _state.MoveHome(_doc, shift, GetVisualLineStartSourceColumn);
                break;
            case Keys.End:
                movedCaret = _state.MoveEnd(_doc, shift, GetVisualLineEndSourceColumn);
                break;
            case Keys.Back:
                if (HandleBackspaceEnterTableRawSourceMode())
                    break;
                if (!TryApplyFastSingleLineBackspace())
                    ApplyDocumentEdit(() => _state.Backspace(_doc));
                break;
            case Keys.Delete:
                if (!TryApplyFastSingleLineDelete())
                    ApplyDocumentEdit(() => _state.Delete(_doc));
                break;
            case Keys.Enter:
                if (!TryApplyFastPlainTextEnter())
                    ApplyDocumentEdit(() => _state.NewLine(_doc));
                break;
            case Keys.Tab:
                ApplyDocumentEdit(() => shift
                    ? _state.UnindentLines(_doc, spaces: 2)
                    : _state.IndentLines(_doc, spaces: 2));
                break;
            default:
                handled = false;
                break;
        }

        if (movedCaret)
        {
            RefreshLayoutForCaretContext();
            ResetCaretBlink();
            EnsureCaretVisible();
            Invalidate();
        }

        if (handled)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }
    private bool FindCore(string query, FindOptions options, bool continueFromLastMatch)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        if (string.IsNullOrEmpty(query))
            return false;

        string needle = NormalizeFindNeedle(query, options.InterpretEscapeSequences);
        if (string.IsNullOrEmpty(needle))
            return false;

        string text = _doc.ToMarkdown();
        if (string.IsNullOrEmpty(text))
            return false;

        bool sameSearch =
            string.Equals(_lastFindQuery, query, StringComparison.Ordinal) &&
            _lastFindOptions == options;

        int caretGlobal = _doc.ToGlobalIndex(_state.Caret);

        int startGlobal =
            (continueFromLastMatch && sameSearch && _lastFindStartGlobal >= 0)
                ? _lastFindStartGlobal + Math.Max(1, _lastFindLength)
                : caretGlobal;

        startGlobal = Math.Clamp(startGlobal, 0, text.Length);

        StringComparison cmp = options.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (!TryFindMatch(text, needle, startGlobal, cmp, options, out int hit))
            return false;

        SelectMatchByGlobalRange(hit, hit + needle.Length);

        _lastFindQuery = query;
        _lastFindOptions = options;
        _lastFindStartGlobal = hit;
        _lastFindLength = needle.Length;

        return true;
    }

    private static string NormalizeFindNeedle(string query, bool interpretEscapes)
    {
        if (!interpretEscapes) return query;

        try
        {
            return Regex.Unescape(query);
        }
        catch
        {
            return query;
        }
    }

    private static bool TryFindMatch(
        string text,
        string needle,
        int startIndex,
        StringComparison comparison,
        FindOptions options,
        out int hit)
    {
        hit = -1;
        if (string.IsNullOrEmpty(needle)) return false;

        if (TryFindMatchInRange(text, needle, startIndex, text.Length, comparison, options, out hit))
            return true;

        if (options.WrapAround && startIndex > 0)
            return TryFindMatchInRange(text, needle, 0, startIndex, comparison, options, out hit);

        return false;
    }

    private static bool TryFindMatchInRange(
        string text,
        string needle,
        int rangeStart,
        int rangeEndExclusive,
        StringComparison comparison,
        FindOptions options,
        out int hit)
    {
        hit = -1;

        int i = Math.Max(0, rangeStart);
        int maxStart = rangeEndExclusive - needle.Length;
        if (maxStart < i) return false;

        while (i <= maxStart)
        {
            int idx = text.IndexOf(needle, i, comparison);
            if (idx < 0 || idx + needle.Length > rangeEndExclusive)
                return false;

            if (IsAcceptedMatch(text, idx, needle.Length, options))
            {
                hit = idx;
                return true;
            }

            i = idx + 1;
        }

        return false;
    }

    private static bool IsAcceptedMatch(string text, int start, int length, FindOptions options)
    {
        // Whole word
        if (options.WholeWord)
        {
            bool leftOk = start == 0 || !IsWordChar(text[start - 1]);
            int after = start + length;
            bool rightOk = after >= text.Length || !IsWordChar(text[after]);

            if (!leftOk || !rightOk)
                return false;
        }

        // Escaped / unescaped filter
        if (options.EscapeMode != EscapeSearchMode.Any)
        {
            bool escaped = IsEscapedAt(text, start);

            if (options.EscapeMode == EscapeSearchMode.OnlyEscaped && !escaped)
                return false;

            if (options.EscapeMode == EscapeSearchMode.OnlyUnescaped && escaped)
                return false;
        }

        return true;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool IsEscapedAt(string text, int index)
    {
        int bs = 0;
        for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
            bs++;

        return (bs % 2) == 1;
    }

    private void SelectMatchByGlobalRange(int start, int end)
    {
        int totalLength = _doc.GetTotalTextLength();
        start = Math.Clamp(start, 0, totalLength);
        end = Math.Clamp(end, start, totalLength);

        MarkdownPosition s = _doc.GlobalToPosition(start);
        MarkdownPosition e = _doc.GlobalToPosition(end);

        _state.Restore(e, s, _doc);

        RefreshLayoutForCaretContext();
        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    private static bool TryClipboardContainsText()
    {
        try { return Clipboard.ContainsText(TextDataFormat.Text); }
        catch { return false; }
    }

    private static bool TryClipboardGetText(out string text)
    {
        text = string.Empty;
        try
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text))
                return false;

            text = Clipboard.GetText(TextDataFormat.Text) ?? string.Empty;
            return !string.IsNullOrEmpty(text);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryClipboardSetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        try
        {
            Clipboard.SetText(text, TextDataFormat.Text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private readonly record struct FenceMarker(int Col, int Len, char Char);

    private static bool IsSupportedFenceLen(char ch, int len)
        => (ch == '`' || ch == '~') && len >= 3;


    private static bool IsSupportedFenceLen(int len) => len >= 3;

    private static bool IsEscaped(string s, int index)
    {
        int backslashes = 0;
        for (int i = index - 1; i >= 0 && s[i] == '\\'; i--)
            backslashes++;
        return (backslashes % 2) == 1;
    }

    private static bool TryFindFenceMarker(string line, int fromIndex, out FenceMarker marker)
    {
        marker = default;
        if (string.IsNullOrEmpty(line)) return false;

        int from = Math.Clamp(fromIndex, 0, line.Length);

        for (int i = from; i < line.Length; i++)
        {
            char ch = line[i];
            if ((ch != '`' && ch != '~') || IsEscaped(line, i))
                continue;

            int j = i;
            while (j < line.Length && line[j] == ch) j++;

            int len = j - i;
            if (IsSupportedFenceLen(ch, len))
            {
                marker = new FenceMarker(i, len, ch);
                return true;
            }

            i = j - 1;
        }

        return false;
    }


    private static bool TryFindFenceCloser(
     string line,
     char expectedChar,
     int minOpenLen,
     int fromIndex,
     out FenceMarker close)
    {
        close = default;
        if (string.IsNullOrEmpty(line)) return false;

        int from = Math.Clamp(fromIndex, 0, line.Length);

        for (int i = from; i < line.Length; i++)
        {
            if (line[i] != expectedChar || IsEscaped(line, i))
                continue;

            int j = i;
            while (j < line.Length && line[j] == expectedChar) j++;

            int len = j - i;
            if (!IsSupportedFenceLen(expectedChar, len) || len < minOpenLen)
            {
                i = j - 1;
                continue;
            }

            bool onlyWhitespaceAfter = true;
            for (int k = j; k < line.Length; k++)
            {
                if (!char.IsWhiteSpace(line[k]))
                {
                    onlyWhitespaceAfter = false;
                    break;
                }
            }

            if (onlyWhitespaceAfter)
            {
                close = new FenceMarker(i, len, expectedChar);
                return true;
            }

            i = j - 1;
        }

        return false;
    }


    private int? GetContainingCodeFenceStartLine(int sourceLine)
    {
        if (sourceLine < 0 || sourceLine >= _doc.LineCount)
            return null;

        bool inFence = false;
        int currentStart = -1;
        char fenceChar = '\0';
        int fenceLen = 0;

        for (int line = 0; line < _doc.LineCount; line++)
        {
            string s = _doc.GetLine(line);

            if (!inFence)
            {
                if (!TryFindFenceMarker(s, 0, out var open))
                    continue;

                currentStart = line;
                inFence = true;
                fenceChar = open.Char;
                fenceLen = open.Len;

                if (line == sourceLine)
                    return currentStart;

                if (TryFindFenceCloser(s, fenceChar, fenceLen, open.Col + open.Len, out _))
                {
                    inFence = false;
                    currentStart = -1;
                    fenceChar = '\0';
                    fenceLen = 0;
                }
            }
            else
            {
                if (line == sourceLine)
                    return currentStart;

                if (TryFindFenceCloser(s, fenceChar, fenceLen, 0, out _))
                {
                    inFence = false;
                    currentStart = -1;
                    fenceChar = '\0';
                    fenceLen = 0;
                }
            }
        }

        return null;
    }

    private bool SyncCodeFenceRawModeWithCaret()
    {
        int? next = GetContainingCodeFenceStartLine(_state.Caret.Line);
        if (_rawCodeFenceStartLine == next) return false;

        _rawCodeFenceStartLine = next;
        return true;
    }

    private IReadOnlySet<int>? GetRawCodeFenceStarts()
        => _rawCodeFenceStartLine.HasValue ? new HashSet<int> { _rawCodeFenceStartLine.Value } : null;

    // NEW: raw-source lines for any block under caret (heading/quote/list/hr)
    private IReadOnlySet<int>? GetRawSourceLinesForCaretBlock()
    {
        if (!TryGetContainingRawSourceBlockRange(_state.Caret.Line, out int startLine, out int endLine))
            return null;

        var lines = new HashSet<int>();
        for (int i = startLine; i <= endLine; i++)
            lines.Add(i);

        return lines;
    }

    private bool TryGetContainingRawSourceBlockRange(int caretLine, out int startLine, out int endLine)
    {
        startLine = -1;
        endLine = -1;

        if (caretLine < 0 || caretLine >= _doc.LineCount)
            return false;

        foreach (var b in _doc.Blocks)
        {
            if (caretLine < b.StartLine || caretLine > b.EndLine)
                continue;

            // Tabellen bleiben bei bestehender Tabellenlogik
            if (b is TableBlock)
                return false;

            // CodeFence über bestehende Fence-Logik
            if (b is CodeFenceBlock)
                return false;

            // Für diese Blöcke Source zeigen, solange Caret im Block ist
            if (b is HeadingBlock or QuoteBlock or ListBlock or FootnoteDefinitionBlock || b.Kind == MarkdownBlockKind.HorizontalRule)
            {
                startLine = Math.Max(0, b.StartLine);
                endLine = Math.Min(_doc.LineCount - 1, b.EndLine);
                return true;
            }

            return false;
        }

        return false;
    }
    private IReadOnlySet<int>? GetRawInlineLinesForCaret()
    {
        int line = _state.Caret.Line;
        if (line < 0 || line >= _doc.LineCount) return null;

        // Keine Inline-Raw-Umschaltung in Tabellen
        if (IsInsideTable(line)) return null;

        // Keine Inline-Raw-Umschaltung in Codefences
        if (GetContainingCodeFenceStartLine(line).HasValue) return null;

        // Nur aktuelle Caret-Zeile als Raw-Inline
        return new HashSet<int> { line };
    }
    private bool IsInsideTable(int sourceLine)
    {
        foreach (var b in _doc.Blocks)
        {
            if (b is TableBlock t && sourceLine >= t.StartLine && sourceLine <= t.EndLine)
                return true;
        }
        return false;
    }


    private bool HandleCtrlEnterExitRawTableMode()
    {
        if (!TryGetContainingTableByLine(_state.Caret.Line, out var table))
            return false;

        if (!_rawTableStartLines.Contains(table.StartLine))
            return false;

        int caretLine = _state.Caret.Line;
        int caretCol = _state.Caret.Column;
        string sourceLine = _doc.GetLine(caretLine);

        _rawTableStartLines.Remove(table.StartLine);
        RefreshLayoutForCaretContext(force: true);

        if (_layout.TryGetTableByStartLine(table.StartLine, out var tableLayout))
        {
            int sourceDelta = caretLine - table.StartLine;
            int targetRow = sourceDelta switch
            {
                <= 0 => 0,
                1 => 0,
                _ => sourceDelta - 1
            };
            targetRow = Math.Clamp(targetRow, 0, tableLayout.Rows - 1);

            int targetCol = GuessTableColumnFromSource(sourceLine, caretCol, tableLayout.Cols);
            BeginTableCellEdit(tableLayout, targetRow, targetCol);

            ResetCaretBlink();
            EnsureCaretVisible();
            Invalidate();
            return true;
        }

        RefreshLayoutForCaretContext();
        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
        return true;
    }

    private bool TryGetContainingTableByLine(int sourceLine, out TableBlock table)
    {
        foreach (var b in _doc.Blocks)
        {
            if (b is TableBlock t && sourceLine >= t.StartLine && sourceLine <= t.EndLine)
            {
                table = t;
                return true;
            }
        }

        table = null!;
        return false;
    }

    private static int GuessTableColumnFromSource(string line, int caretCol, int cols)
    {
        if (cols <= 1) return 0;
        if (string.IsNullOrEmpty(line)) return 0;

        caretCol = Math.Clamp(caretCol, 0, line.Length);
        var pipes = FindUnescapedPipePositions(line);

        if (pipes.Count >= 2)
        {
            int cellCount = Math.Min(cols, pipes.Count - 1);
            for (int cell = 0; cell < cellCount; cell++)
            {
                int right = pipes[cell + 1];
                if (caretCol <= right) return cell;
            }
            return cellCount - 1;
        }

        int separatorsBefore = 0;
        for (int i = 0; i < caretCol; i++)
        {
            if (line[i] == '\\' && i + 1 < caretCol && line[i + 1] == '|')
            {
                i++;
                continue;
            }
            if (line[i] == '|') separatorsBefore++;
        }

        int idx = Math.Max(0, separatorsBefore - 1);
        return Math.Clamp(idx, 0, cols - 1);
    }

    private static List<int> FindUnescapedPipePositions(string line)
    {
        var result = new List<int>();
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\' && i + 1 < line.Length && line[i + 1] == '|')
            {
                i++;
                continue;
            }
            if (line[i] == '|') result.Add(i);
        }
        return result;
    }

    private enum PendingTableDraftRole { None, Header, Delimiter }

    private readonly record struct PendingTableDraftInfo(
        bool IsPending,
        PendingTableDraftRole Role,
        int ExpectedCols,
        int CurrentCols);

    private PendingTableDraftInfo GetPendingTableDraftInfo(int sourceLine)
    {
        if (sourceLine + 1 < _doc.LineCount)
        {
            var info = BuildPendingDraftInfo(sourceLine, sourceLine + 1, PendingTableDraftRole.Header);
            if (info.IsPending) return info;
        }

        if (sourceLine - 1 >= 0)
        {
            var info = BuildPendingDraftInfo(sourceLine - 1, sourceLine, PendingTableDraftRole.Delimiter);
            if (info.IsPending) return info;
        }

        return default;
    }

    private PendingTableDraftInfo BuildPendingDraftInfo(int headerLineIndex, int delimiterLineIndex, PendingTableDraftRole role)
    {
        if (IsInsideRealTable(headerLineIndex) || IsInsideRealTable(delimiterLineIndex))
            return default;

        int caretLine = _state.Caret.Line;
        if (caretLine != headerLineIndex && caretLine != delimiterLineIndex)
            return default;

        string header = _doc.GetLine(headerLineIndex);
        string delimiter = _doc.GetLine(delimiterLineIndex);

        if (!IsHeaderCandidate(header) || !IsDelimiterCandidate(delimiter))
            return default;

        List<string> headerCells = ParsePipeCellsLoose(header);
        int expectedCols = headerCells.Count;
        if (expectedCols < 2) return default;

        List<string> delimiterCells = ParsePipeCellsLoose(delimiter);
        int currentCols = delimiterCells.Count;

        bool isComplete = IsDelimiterComplete(delimiter, expectedCols, delimiterCells);
        if (isComplete) return default;

        if (HasLikelyBodyRowBelowDelimiter(delimiterLineIndex, expectedCols))
            return default;

        return new PendingTableDraftInfo(true, role, expectedCols, currentCols);
    }

    private static bool IsHeaderCandidate(string line)
    {
        if (!IsPipeBounded(line)) return false;

        var cells = ParsePipeCellsLoose(line);
        if (cells.Count < 2) return false;

        bool allDelimiterLike = cells.All(c => TableDelimiterCellRegex.IsMatch(c.Trim()));
        return !allDelimiterLike;
    }

    private static bool IsDelimiterCandidate(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        if (!DelimiterDraftLineRegex.IsMatch(line)) return false;
        return line.TrimStart().StartsWith("|", StringComparison.Ordinal);
    }

    private bool IsInsideRealTable(int sourceLine)
    {
        foreach (var b in _doc.Blocks)
        {
            if (b is TableBlock t && sourceLine >= t.StartLine && sourceLine <= t.EndLine)
                return true;
        }
        return false;
    }

    private bool HasLikelyBodyRowBelowDelimiter(int delimiterLine, int expectedCols)
    {
        int bodyLine = delimiterLine + 1;
        if (bodyLine >= _doc.LineCount) return false;

        string line = _doc.GetLine(bodyLine);
        if (string.IsNullOrWhiteSpace(line)) return false;
        if (!IsPipeBounded(line)) return false;

        var cells = ParsePipeCellsLoose(line);
        return cells.Count == expectedCols;
    }

    private static bool IsDelimiterComplete(string delimiterLine, int expectedCols, List<string> delimiterCells)
    {
        if (!IsPipeBounded(delimiterLine)) return false;
        if (delimiterCells.Count != expectedCols) return false;

        foreach (string raw in delimiterCells)
        {
            string cell = raw.Trim();
            if (!TableDelimiterCellRegex.IsMatch(cell))
                return false;
        }

        return true;
    }

    private static bool IsPipeBounded(string line)
    {
        string s = line.Trim();
        return s.StartsWith("|", StringComparison.Ordinal) && s.EndsWith("|", StringComparison.Ordinal);
    }

    private static List<string> ParsePipeCellsLoose(string line)
    {
        string s = line.Trim();

        if (s.StartsWith("|", StringComparison.Ordinal)) s = s[1..];
        if (s.EndsWith("|", StringComparison.Ordinal)) s = s[..^1];

        var cells = new List<string>();
        var sb = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (ch == '\\' && i + 1 < s.Length && s[i + 1] == '|')
            {
                sb.Append('|');
                i++;
                continue;
            }

            if (ch == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        cells.Add(sb.ToString().Trim());
        return cells;
    }

    private bool TryGetTableEndingAtLine(int endLine, out TableBlock table)
    {
        foreach (var block in _doc.Blocks)
        {
            if (block is TableBlock t && t.EndLine == endLine)
            {
                table = t;
                return true;
            }
        }

        table = null!;
        return false;
    }

    private bool HandleBackspaceEnterTableRawSourceMode()
    {
        if (_state.HasSelection) return false;

        var caret = _state.Caret;

        if (caret.Column != 0 || caret.Line <= 0) return false;
        if (!string.IsNullOrWhiteSpace(_doc.GetLine(caret.Line))) return false;
        if (caret.Line != _doc.LineCount - 1) return false;

        if (!TryGetTableEndingAtLine(caret.Line - 1, out var table))
            return false;

        if (_doc.Blocks.LastOrDefault() is not TableBlock last || last.StartLine != table.StartLine)
            return false;

        _rawTableStartLines.Add(table.StartLine);

        RefreshLayoutForCaretContext(force: true);

        int targetLine = table.StartLine + 1;
        int targetCol = _doc.GetLineLength(targetLine);
        _state.SetCaret(new MarkdownPosition(targetLine, targetCol), false, _doc);

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
        return true;
    }

    private void EnsureTrailingEditableLineAfterTerminalTable()
    {
        if (_doc.LineCount == 0)
        {
            _doc.LoadMarkdown(string.Empty);
            return;
        }

        if (_doc.Blocks.Count == 0)
            _doc.ReparseAll();

        if (_doc.Blocks.Count == 0)
            return;

        if (_doc.Blocks[^1] is TableBlock t && t.EndLine == _doc.LineCount - 1)
        {
            int lastLine = _doc.LineCount - 1;
            int lastCol = _doc.GetLineLength(lastLine);
            _doc.InsertText(new MarkdownPosition(lastLine, lastCol), "\n");
            _doc.ReparseAll();
        }
    }

    private void CleanupRawTableModes()
    {
        if (_rawTableStartLines.Count == 0) return;

        var validStarts = _doc.Blocks
            .OfType<TableBlock>()
            .Select(t => t.StartLine)
            .ToHashSet();

        _rawTableStartLines.RemoveWhere(start => !validStarts.Contains(start));
    }

    private bool ExitRawModesIfCaretOutside()
    {
        if (_rawTableStartLines.Count == 0) return false;

        int caretLine = _state.Caret.Line;
        List<int> remove = new();

        foreach (int start in _rawTableStartLines)
        {
            var table = _doc.Blocks.OfType<TableBlock>().FirstOrDefault(t => t.StartLine == start);
            if (table is null)
            {
                remove.Add(start);
                continue;
            }

            if (caretLine < table.StartLine || caretLine > table.EndLine)
                remove.Add(start);
        }

        if (remove.Count == 0) return false;

        foreach (int r in remove)
            _rawTableStartLines.Remove(r);

        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        EnsureSystemThemeSync();
        TryApplyPendingInitialRefresh();
        base.OnPaint(e);

        e.Graphics.ResetTransform();
        e.Graphics.PageUnit = GraphicsUnit.Pixel;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle dirtyClient = Rectangle.Intersect(e.ClipRectangle, new Rectangle(Point.Empty, ClientSize));
        if (dirtyClient.Width <= 0 || dirtyClient.Height <= 0)
            return;

        using (var backBrush = new SolidBrush(BackColor))
            e.Graphics.FillRectangle(backBrush, dirtyClient);

        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        Rectangle viewport = new(
            -AutoScrollPosition.X,
            -AutoScrollPosition.Y,
            ClientSize.Width,
            ClientSize.Height);

        Rectangle paintBounds = Rectangle.Intersect(
            viewport,
            new Rectangle(
                dirtyClient.X - AutoScrollPosition.X,
                dirtyClient.Y - AutoScrollPosition.Y,
                dirtyClient.Width,
                dirtyClient.Height));

        if (paintBounds.Width <= 0 || paintBounds.Height <= 0)
            return;

        e.Graphics.SetClip(paintBounds);

        // Draw content first
        foreach (var line in _layout.GetVisibleLines(paintBounds))
            DrawLine(e.Graphics, line);

        foreach (var table in _layout.GetVisibleTables(paintBounds))
            DrawTable(e.Graphics, table);

        // Then selection overlay
        DrawSelection(e.Graphics, paintBounds);

        if (Focused && _caretVisible && _cellEditor is null &&
            TryGetCaretContentRectangle(_state.Caret, out Rectangle caretRect) &&
            caretRect.IntersectsWith(paintBounds))
        {
            DrawCaret(e.Graphics);
        }
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        if (_themeMode == EditorThemeMode.System && AllowAutoThemeChange)
        {
            RefreshSystemThemePreference();
            ApplyTheme();
        }
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        if (_themeMode == EditorThemeMode.System && AllowAutoThemeChange)
            ApplyTheme();
    }

    protected override void OnParentForeColorChanged(EventArgs e)
    {
        base.OnParentForeColorChanged(e);
        if (_themeMode == EditorThemeMode.System && AllowAutoThemeChange)
            ApplyTheme();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_themeMode == EditorThemeMode.System && AllowAutoThemeChange)
            ApplyTheme();
    }


    private void DrawLine(Graphics g, LayoutLine line)
    {
        if (IsHorizontalRule(line.Kind))
        {
            int x1 = line.TextX;
            int x2 = Math.Max(line.TextX + 64, AutoScrollMinSize.Width - 16);
            int y = line.Bounds.Top + (line.Bounds.Height / 2);

            using var p = new Pen(_hrColor, 1f);
            g.DrawLine(p, x1, y, x2, y);
            return;
        }

        if (line.IsImagePreview)
        {
            DrawImagePreview(g, line);
            return;
        }

        string display = line.Projection.DisplayText;

        bool isQuote = line.Kind == MarkdownBlockKind.Quote;
        bool isAdmonition = isQuote && line.IsAdmonition;
        AdmonitionPalette ad = default;

        if (isAdmonition)
        {
            ad = GetAdmonitionPalette(line.QuoteAdmonition);

            int bgLeft = 8;
            int bgRight = Math.Max(bgLeft + 24, AutoScrollMinSize.Width - 16);
            int bgWidth = Math.Max(24, bgRight - bgLeft);

            var bgRect = new Rectangle(bgLeft, line.Bounds.Top, bgWidth, line.Bounds.Height);

            using var bgBrush = new SolidBrush(ad.Background);
            g.FillRectangle(bgBrush, bgRect);

            using var borderPen = new Pen(ad.Border, 1f);
            if (line.IsQuoteStart)
                g.DrawLine(borderPen, bgRect.Left, bgRect.Top, bgRect.Right, bgRect.Top);

            if (line.IsQuoteEnd)
                g.DrawLine(borderPen, bgRect.Left, bgRect.Bottom - 1, bgRect.Right, bgRect.Bottom - 1);
        }

        PendingTableDraftInfo draft = GetPendingTableDraftInfo(line.SourceLine);
        if (draft.IsPending)
        {
            using var draftBg = new SolidBrush(_tableDraftBg);
            int w = Math.Max(line.TextWidth + 10, 56);
            var draftRect = new Rectangle(line.TextX - 5, line.Bounds.Top, w, line.Bounds.Height);
            g.FillRectangle(draftBg, draftRect);

            using var draftBorder = new Pen(_tableDraftBorder, 1f);
            g.DrawRectangle(draftBorder, draftRect);

            if (draft.Role == PendingTableDraftRole.Delimiter)
            {
                string hint = $"Table draft: {draft.CurrentCols}/{draft.ExpectedCols}";
                using var hintFont = new Font(Font, FontStyle.Italic);
                DrawTextGdiPlus(g, hint, hintFont, new Point(line.TextX + w + 8, line.Bounds.Top + 1), _tableDraftHint);
            }
        }

        if (line.Kind == MarkdownBlockKind.CodeFence)
        {
            using var b = new SolidBrush(_codeBg);

            int bgLeft = line.TextX - 4;
            int bgRight = Math.Max(bgLeft + 24, AutoScrollMinSize.Width - 16);
            int bgWidth = Math.Max(24, bgRight - bgLeft);

            g.FillRectangle(b, new Rectangle(bgLeft, line.Bounds.Top, bgWidth, line.Bounds.Height));
        }

        if (line.Kind == MarkdownBlockKind.Quote)
        {
            Color barColor = isAdmonition ? ad.Bar : _quoteBarColor;
            using var p = new Pen(barColor, 3f);
            g.DrawLine(p, 12, line.Bounds.Top + 2, 12, line.Bounds.Bottom - 2);
        }

        bool drawAdmonitionHeader = line.IsAdmonitionMarkerLine && string.IsNullOrEmpty(display);
        if (drawAdmonitionHeader)
        {
            string title = string.IsNullOrWhiteSpace(ad.Icon)
                ? line.AdmonitionTitle
                : $"{ad.Icon}  {line.AdmonitionTitle}";

            DrawTextGdiPlus(g, title, _boldFont, new Point(line.TextX, line.Bounds.Top + 1), ad.TitleColor);
        }
        else if (!string.IsNullOrEmpty(display))
        {
            DrawInlineRuns(g, line);
            DrawTaskCheckboxOverlay(g, line);
        }
    }

    private void DrawImagePreview(Graphics g, LayoutLine line)
    {
        Rectangle frame = GetImagePreviewRect(line);

        ImageCacheEntry entry = GetOrQueueImage(line.ImageSource);
        if (entry.Image is not null)
        {
            DrawLoadedImage(g, entry.Image, frame);
            return;
        }

        string label = entry.IsLoading
            ? $"Loading image: {GetImageCaption(line)}"
            : !string.IsNullOrWhiteSpace(entry.Error)
                ? $"Image unavailable: {GetImageCaption(line)}"
                : $"Image: {GetImageCaption(line)}";

        DrawImagePlaceholder(
            g,
            frame,
            label,
            TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
    }

    private Rectangle GetImagePreviewRect(LayoutLine line)
    {
        int top = line.Bounds.Top + ImagePreviewPaddingY;
        int height = Math.Max(1, line.Bounds.Height - (ImagePreviewPaddingY * 2));
        return new Rectangle(line.TextX, top, Math.Max(1, line.TextWidth), height);
    }

    private static Rectangle GetAspectFitRect(Size imageSize, Rectangle bounds)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
            return bounds;

        float scale = Math.Min(
            bounds.Width / (float)imageSize.Width,
            bounds.Height / (float)imageSize.Height);

        scale = Math.Min(1f, scale);

        int width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        int height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        int x = bounds.X + Math.Max(0, (bounds.Width - width) / 2);
        int y = bounds.Y + Math.Max(0, (bounds.Height - height) / 2);

        return new Rectangle(x, y, width, height);
    }

    private string GetImageCaption(LayoutLine line)
    {
        if (!string.IsNullOrWhiteSpace(line.ImageAltText))
            return line.ImageAltText;

        if (!string.IsNullOrWhiteSpace(line.ImageSource))
            return line.ImageSource;

        return "image";
    }

    private string GetImageCaption(InlineRun run)
    {
        if (!string.IsNullOrWhiteSpace(run.AltText))
            return run.AltText;

        if (!string.IsNullOrWhiteSpace(run.Source))
            return run.Source;

        return "image";
    }

    private Size GetInlineImageSize(string source)
        => InlineImageMetrics.CalculateSize(source, TryGetCachedImageSize);

    private void DrawInlineImage(Graphics g, InlineRun run, Rectangle bounds)
    {
        ImageCacheEntry entry = GetOrQueueImage(run.Source);
        if (entry.Image is not null)
        {
            DrawLoadedImage(g, entry.Image, bounds);
            return;
        }

        string label = entry.IsLoading
            ? $"Loading {GetImageCaption(run)}"
            : !string.IsNullOrWhiteSpace(entry.Error)
                ? "Image unavailable"
                : GetImageCaption(run);

        DrawImagePlaceholder(
            g,
            bounds,
            label,
            TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }

    private void DrawLoadedImage(Graphics g, Image image, Rectangle bounds)
    {
        Rectangle imageRect = GetAspectFitRect(image.Size, bounds);
        var previousMode = g.InterpolationMode;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(image, imageRect);
        g.InterpolationMode = previousMode;
    }

    private void DrawImagePlaceholder(Graphics g, Rectangle bounds, string label, TextFormatFlags flags)
    {
        using var borderPen = new Pen(_imagePlaceholderBorder, 1f);
        using var backBrush = new SolidBrush(_imagePlaceholderBack);
        g.FillRectangle(backBrush, bounds);
        g.DrawRectangle(borderPen, bounds);

        Rectangle textRect = Rectangle.Inflate(bounds, -6, -6);
        if (textRect.Width <= 0 || textRect.Height <= 0)
            return;

        TextRenderer.DrawText(
            g,
            label,
            Font,
            textRect,
            _imagePlaceholderText,
            flags);
    }

    private void DrawLinkText(Graphics g, string text, Font font, Point location)
    {
        if (string.IsNullOrEmpty(text))
            return;

        DrawTextGdiPlus(g, text, font, location, _linkColor);

        int width = MeasureWidth(g, text, font);
        int height = MeasureHeight(g, font);
        int underlineY = location.Y + Math.Max(1, height - 1);

        using var pen = new Pen(_linkColor, 1f);
        g.DrawLine(pen, location.X, underlineY, location.X + Math.Max(1, width), underlineY);
    }

    private void DrawFootnoteReferenceText(Graphics g, string text, Font font, Point location)
    {
        if (string.IsNullOrEmpty(text))
            return;

        DrawTextGdiPlus(g, text, font, new Point(location.X, location.Y - FootnoteRaiseY), _linkColor);
    }

    private Size? TryGetCachedImageSize(string source)
    {
        ImageCacheEntry entry = GetOrQueueImage(source);
        return entry.Image?.Size;
    }

    private ImageCacheEntry GetOrQueueImage(string source)
    {
        if (!TryResolveImageLocation(source, out string cacheKey, out Uri? remoteUri, out string? localPath))
        {
            return new ImageCacheEntry
            {
                DisplaySource = source,
                Error = "Unsupported image source."
            };
        }

        lock (_imageCacheSync)
        {
            if (_imageCache.TryGetValue(cacheKey, out var existing))
                return existing;

            var created = new ImageCacheEntry
            {
                DisplaySource = source,
                IsLoading = true
            };

            _imageCache[cacheKey] = created;

            if (remoteUri is not null)
                _ = Task.Run(() => LoadRemoteImageAsync(cacheKey, remoteUri));
            else if (!string.IsNullOrWhiteSpace(localPath))
                _ = Task.Run(() => LoadLocalImage(cacheKey, localPath));
            else
                created.Error = "Image source could not be resolved.";

            return created;
        }
    }

    private bool TryResolveImageLocation(
        string source,
        out string cacheKey,
        out Uri? remoteUri,
        out string? localPath)
    {
        cacheKey = string.Empty;
        remoteUri = null;
        localPath = null;

        string raw = (source ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        if (Uri.TryCreate(raw, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (absoluteUri.IsFile)
            {
                localPath = Path.GetFullPath(absoluteUri.LocalPath);
                cacheKey = localPath;
                return true;
            }

            if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                remoteUri = absoluteUri;
                cacheKey = absoluteUri.AbsoluteUri;
                return true;
            }
        }

        string basePath = _documentBasePath ?? Environment.CurrentDirectory;
        string combined = Path.IsPathRooted(raw)
            ? raw
            : Path.Combine(basePath, raw);

        localPath = Path.GetFullPath(combined);
        cacheKey = localPath;
        return true;
    }

    private async Task LoadRemoteImageAsync(string cacheKey, Uri uri)
    {
        try
        {
            byte[] bytes = await ImageHttpClient.GetByteArrayAsync(uri);
            Image image = LoadImageFromBytes(bytes, uri.AbsoluteUri);
            CompleteImageLoad(cacheKey, image, error: null);
        }
        catch (Exception ex)
        {
            CompleteImageLoad(cacheKey, image: null, error: ex.Message);
        }
    }

    private void LoadLocalImage(string cacheKey, string localPath)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                CompleteImageLoad(cacheKey, image: null, error: "File not found.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(localPath);
            Image image = LoadImageFromBytes(bytes, localPath);
            CompleteImageLoad(cacheKey, image, error: null);
        }
        catch (Exception ex)
        {
            CompleteImageLoad(cacheKey, image: null, error: ex.Message);
        }
    }

    private static Image LoadImageFromBytes(byte[] bytes, string sourceHint)
    {
        if (bytes.Length == 0)
            throw new InvalidOperationException("Image stream is empty.");

        if (LooksLikeSvg(sourceHint, bytes))
            return LoadSvgBitmap(bytes);

        using var stream = new MemoryStream(bytes, writable: false);
        using var loaded = Image.FromStream(stream);
        return new Bitmap(loaded);
    }

    private static bool LooksLikeSvg(string sourceHint, byte[] bytes)
    {
        if (!string.IsNullOrWhiteSpace(sourceHint))
        {
            string candidate = sourceHint;
            if (Uri.TryCreate(sourceHint, UriKind.Absolute, out Uri? uri))
                candidate = uri.AbsolutePath;

            if (string.Equals(Path.GetExtension(candidate), ".svg", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        string prefix = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 2048)).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
               (prefix.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
                prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase));
    }

    private static Image LoadSvgBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var svg = new Svg.Skia.SKSvg();
        SkiaSharp.SKPicture picture = svg.Load(stream)
            ?? throw new InvalidOperationException("SVG could not be parsed.");

        SkiaSharp.SKRect bounds = picture.CullRect;
        float sourceWidth = bounds.Width > 0 ? bounds.Width : 256f;
        float sourceHeight = bounds.Height > 0 ? bounds.Height : 256f;

        float scale = Math.Min(
            1f,
            Math.Min(
                SvgRasterMaxWidth / sourceWidth,
                SvgRasterMaxHeight / sourceHeight));

        int width = Math.Max(1, (int)Math.Ceiling(sourceWidth * scale));
        int height = Math.Max(1, (int)Math.Ceiling(sourceHeight * scale));

        using var bitmap = new SkiaSharp.SKBitmap(
            new SkiaSharp.SKImageInfo(
                width,
                height,
                SkiaSharp.SKColorType.Bgra8888,
                SkiaSharp.SKAlphaType.Premul));
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        canvas.Scale(width / sourceWidth, height / sourceHeight);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using SkiaSharp.SKData data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var encoded = new MemoryStream(data.ToArray(), writable: false);
        using var loaded = Image.FromStream(encoded);
        return new Bitmap(loaded);
    }

    private void CompleteImageLoad(string cacheKey, Image? image, string? error)
    {
        lock (_imageCacheSync)
        {
            if (!_imageCache.TryGetValue(cacheKey, out var entry))
            {
                image?.Dispose();
                return;
            }

            entry.Image?.Dispose();
            entry.Image = image;
            entry.Error = error;
            entry.IsLoading = false;
        }

        NotifyImageContentChanged();
    }

    private void NotifyImageContentChanged()
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        try
        {
            BeginInvoke((MethodInvoker)(() =>
            {
                if (IsDisposed)
                    return;

                InvalidateLayoutContext();
                RefreshLayoutForCaretContext(force: true);
                RepositionCellEditor();
                Invalidate();
            }));
        }
        catch
        {
            // ignore handle teardown races
        }
    }

    private void ClearImageCache()
    {
        lock (_imageCacheSync)
        {
            foreach (var entry in _imageCache.Values)
                entry.Image?.Dispose();

            _imageCache.Clear();
        }
    }

    private void DrawTable(Graphics g, TableLayout table)
    {
        using var headerBrush = new SolidBrush(_tableHeaderBg);
        using var cellBrush = new SolidBrush(_tableCellBg);
        using var gridPen = new Pen(_tableGrid, 1f);

        for (int r = 0; r < table.Rows; r++)
        {
            for (int c = 0; c < table.Cols; c++)
            {
                Rectangle rect = table.GetCellRect(r, c);

                g.FillRectangle(r == 0 ? headerBrush : cellBrush, rect);
                g.DrawRectangle(gridPen, rect);

                Rectangle textRect = Rectangle.Inflate(rect, -8, -5);

                IReadOnlyList<InlineRun> runs = table.GetCellRuns(r, c);
                string fallbackText = table.GetCellText(r, c);

                Font baseFont = (r == 0) ? _boldFont : Font;

                int textWidth = runs.Count > 0
                    ? MeasureInlineRunsWidthForTable(g, runs, baseFont)
                    : MeasureWidth(g, fallbackText, baseFont);

                int x = textRect.Left;
                string align = table.GetColumnAlignment(c).ToString();

                if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                    x = textRect.Left + Math.Max(0, (textRect.Width - textWidth) / 2);
                else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
                    x = textRect.Right - textWidth;

                x = Math.Max(textRect.Left, x);
                int y = textRect.Top + Math.Max(0, (textRect.Height - MeasureHeight(g, baseFont)) / 2);

                if (runs.Count > 0)
                    DrawInlineRunsInCell(g, runs, baseFont, new Point(x, y), textRect, ForeColor);
                else
                    DrawTextGdiPlus(g, fallbackText, baseFont, new Point(x, y), ForeColor);
            }
        }

        using var outer = new Pen(_tableOuterBorderColor, 1.2f);
        g.DrawRectangle(outer, table.Bounds);
    }

    private int MeasureInlineRunsWidthForTable(Graphics g, IReadOnlyList<InlineRun> runs, Font baseFont)
    {
        if (runs.Count == 0) return 0;

        int width = 0;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (run.IsImage)
                {
                    width += GetInlineImageSize(run.Source).Width;
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font f = GetOrCreateTableRunFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference);

                int w = MeasureWidth(g, run.Text, f);
                if (isCode) w += InlineCodePadX * 2;
                width += w;
            }
        }
        finally
        {
            foreach (var f in cache.Values)
                f.Dispose();
        }

        return width;
    }

    private Font GetOrCreateTableRunFont(Dictionary<int, Font> cache, Font baseFont, InlineStyle style, bool isCode, bool isFootnoteReference)
    {
        if (style == InlineStyle.None && !isFootnoteReference && !isCode)
            return baseFont;

        InlineStyle normalized = style & ~InlineStyle.Code;

        int key = ((int)normalized & 0xFF)
                  | (isCode ? 0x100 : 0)
                  | (isFootnoteReference ? 0x200 : 0)
                  | (((int)baseFont.Style & 0xFF) << 9);

        if (cache.TryGetValue(key, out var f))
            return f;

        Font seed = isCode ? _monoFont : baseFont;
        f = CreateRunDisplayFont(seed, normalized, isFootnoteReference);
        cache[key] = f;
        return f;
    }

    private void DrawInlineRunsInCell(
        Graphics g,
        IReadOnlyList<InlineRun> runs,
        Font baseFont,
        Point start,
        Rectangle clipRectContent,
        Color color)
    {
        int x = start.X;

        using var codeBrush = new SolidBrush(_inlineCodeBg);
        using var codePen = new Pen(_inlineCodeBorder);

        var cache = new Dictionary<int, Font>();
        var state = g.Save();
        g.SetClip(clipRectContent);

        try
        {
            foreach (var run in runs)
            {
                if (run.IsImage)
                {
                    Size imageSize = GetInlineImageSize(run.Source);
                    int imageY = clipRectContent.Top + Math.Max(0, (clipRectContent.Height - imageSize.Height) / 2);
                    DrawInlineImage(g, run, new Rectangle(x, imageY, imageSize.Width, imageSize.Height));
                    x += imageSize.Width;
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateTableRunFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference);

                if (run.IsFootnoteReference)
                {
                    DrawFootnoteReferenceText(g, run.Text, runFont, new Point(x, start.Y));
                    x += MeasureWidth(g, run.Text, runFont);
                    continue;
                }

                if (run.IsLink)
                {
                    DrawLinkText(g, run.Text, runFont, new Point(x, start.Y));
                    x += MeasureWidth(g, run.Text, runFont);
                    continue;
                }

                if (!isCode)
                {
                    DrawTextGdiPlus(g, run.Text, runFont, new Point(x, start.Y), color);
                    x += MeasureWidth(g, run.Text, runFont);
                    continue;
                }

                int textW = MeasureWidth(g, run.Text, runFont);
                int textH = MeasureHeight(g, runFont);

                int chipW = textW + InlineCodePadX * 2;
                int chipH = Math.Min(clipRectContent.Height, textH + InlineCodePadY * 2);
                int chipY = clipRectContent.Top + Math.Max(0, (clipRectContent.Height - chipH) / 2);

                var chip = new Rectangle(x, chipY, chipW, Math.Max(1, chipH));

                g.FillRectangle(codeBrush, chip);
                g.DrawRectangle(codePen, chip.X, chip.Y, Math.Max(1, chip.Width - 1), Math.Max(1, chip.Height - 1));

                int textY = chip.Y + Math.Max(0, (chip.Height - textH) / 2);
                DrawTextGdiPlus(g, run.Text, runFont, new Point(x + InlineCodePadX, textY), color);

                x += chipW;
            }
        }
        finally
        {
            g.Restore(state);
            foreach (var f in cache.Values)
                f.Dispose();
        }
    }

    private int MeasureHeight(Font font)
    {
        using var bmp = new Bitmap(1, 1);
        bmp.SetResolution(Math.Max(1f, DeviceDpi), Math.Max(1f, DeviceDpi));
        using var g = Graphics.FromImage(bmp);
        return MeasureHeight(g, font);
    }

    private int MeasureWidth(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        using var bmp = new Bitmap(1, 1);
        bmp.SetResolution(Math.Max(1f, DeviceDpi), Math.Max(1f, DeviceDpi));
        using var g = Graphics.FromImage(bmp);

        return MeasureWidth(g, text, font);
    }

    private static int MeasureHeight(Graphics g, Font font) => (int)Math.Ceiling(font.GetHeight(g));

    private static int MeasureWidth(Graphics g, string text, Font font)
        => (int)Math.Ceiling(MeasureAdvanceWidth(g, text, font));

    private static float MeasureAdvanceWidth(Graphics g, string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        using StringFormat format = (StringFormat)MeasureStringFormat.Clone();
        format.SetMeasurableCharacterRanges([new CharacterRange(0, text.Length)]);

        Region[] regions = g.MeasureCharacterRanges(text, font, new RectangleF(0, 0, 10000, 1000), format);
        try
        {
            RectangleF bounds = regions[0].GetBounds(g);
            return bounds.Width;
        }
        finally
        {
            foreach (Region region in regions)
                region.Dispose();
        }
    }

    private static float[] MeasurePrefixAdvances(Graphics g, string text, Font font)
    {
        var offsets = new float[text.Length + 1];
        if (text.Length == 0)
            return offsets;

        const int maxRangesPerBatch = 32;
        float layoutHeight = Math.Max(64f, font.GetHeight(g) * 4f);
        var layoutRect = new RectangleF(0, 0, 100000f, layoutHeight);

        using StringFormat format = (StringFormat)MeasureStringFormat.Clone();

        for (int batchStart = 1; batchStart <= text.Length; batchStart += maxRangesPerBatch)
        {
            int count = Math.Min(maxRangesPerBatch, text.Length - batchStart + 1);
            var ranges = new CharacterRange[count];
            for (int i = 0; i < count; i++)
                ranges[i] = new CharacterRange(0, batchStart + i);

            format.SetMeasurableCharacterRanges(ranges);

            Region[] regions = g.MeasureCharacterRanges(text, font, layoutRect, format);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int idx = batchStart + i;
                    RectangleF bounds = regions[i].GetBounds(g);
                    offsets[idx] = Math.Max(offsets[idx - 1], bounds.Width);
                }
            }
            finally
            {
                foreach (Region region in regions)
                    region.Dispose();
            }
        }

        return offsets;
    }

    private static float MeasureInkWidth(Graphics g, string text, Font font)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        using var path = new GraphicsPath();
        float emSize = font.SizeInPoints * g.DpiY / 72f;
        path.AddString(
            text,
            font.FontFamily,
            (int)font.Style,
            emSize,
            PointF.Empty,
            StringFormat.GenericTypographic);

        RectangleF bounds = path.GetBounds();
        return bounds.Width;
    }

    private static void DrawTextGdiPlus(Graphics g, string text, Font font, Point pt, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, pt, DrawStringFormat);
    }

    private static void DrawTextGdiPlus(Graphics g, string text, Font font, float x, float y, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, x, y, DrawStringFormat);
    }

    private void DrawSelection(Graphics g, Rectangle viewport)
    {
        if (!_state.HasSelection) return;

        var (start, end) = _state.GetSelection();
        using var brush = new SolidBrush(_selectionColor);

        // A) Text-/Line-Selection wie bisher
        for (int srcLine = start.Line; srcLine <= end.Line; srcLine++)
        {
            LayoutLine? line = _layout.GetPreparedLine(srcLine);
            if (line is null) continue;
            if (line.Bounds.Bottom < viewport.Top || line.Bounds.Top > viewport.Bottom) continue;

            int srcLen = line.SourceText.Length;
            int selStartSrc = (srcLine == start.Line) ? start.Column : 0;
            int selEndSrc = (srcLine == end.Line) ? end.Column : srcLen;

            selStartSrc = Math.Clamp(selStartSrc, 0, srcLen);
            selEndSrc = Math.Clamp(selEndSrc, 0, srcLen);
            if (selEndSrc <= selStartSrc) continue;

            if (line.IsImagePreview)
            {
                Rectangle imageRect = Rectangle.Inflate(GetImagePreviewRect(line), -1, -1);
                g.FillRectangle(brush, imageRect);
                continue;
            }

            int visStart = line.Projection.SourceToVisual[selStartSrc];
            int visEnd = line.Projection.SourceToVisual[selEndSrc];

            string display = line.Projection.DisplayText;
            visStart = Math.Clamp(visStart, 0, display.Length);
            visEnd = Math.Clamp(visEnd, 0, display.Length);
            if (visEnd <= visStart) continue;

            if (line.Segments.Count == 0)
            {
                if (!TryGetVisualX(g, line, visStart, out _, out float x1) ||
                    !TryGetVisualX(g, line, visEnd, out _, out float x2))
                {
                    continue;
                }

                float width = Math.Max(1f, x2 - x1);
                float height = Math.Max(1f, line.Bounds.Height - 2);

                g.FillRectangle(brush, x1, line.Bounds.Top + 1, width, height);
                continue;
            }

            foreach (LayoutSegment segment in line.Segments)
            {
                int localStart = Math.Max(0, visStart - segment.VisualStart);
                int localEnd = Math.Min(segment.VisualEnd - segment.VisualStart, visEnd - segment.VisualStart);
                if (localEnd <= localStart)
                    continue;

                float x1 = SnapVisualX(GetSegmentVisualX(g, line, segment, localStart));
                float x2 = SnapVisualX(GetSegmentVisualX(g, line, segment, localEnd));
                float width = Math.Max(1f, x2 - x1);
                float height = Math.Max(1f, segment.Bounds.Height - 2);

                g.FillRectangle(brush, x1, segment.Bounds.Top + 1, width, height);
            }
        }

        // B) Grid-Table-Selection (visuelle Tabellenzellen)
        DrawTableGridSelectionOverlay(g, viewport, start, end, brush);
    }

    private void DrawTableGridSelectionOverlay(
    Graphics g,
    Rectangle viewport,
    MarkdownPosition selectionStart,
    MarkdownPosition selectionEnd,
    Brush selectionBrush)
    {
        // Während Cell-Edit nicht über den Editor malen
        if (_cellEditor is not null)
            return;

        foreach (var table in _layout.GetVisibleTables(viewport))
        {
            // Wenn Tabelle gerade im Raw-Source-Modus ist, übernimmt die normale Line-Selection
            if (_rawTableStartLines.Contains(table.StartLine))
                continue;

            TableBlock block = table.Block;

            for (int r = 0; r < table.Rows; r++)
            {
                int sourceLine = MapGridRowToSourceLine(block, r);

                if (!TryGetSelectionSpanForSourceLine(sourceLine, selectionStart, selectionEnd, out int lineSelStart, out int lineSelEnd))
                    continue;

                string source = _doc.GetLine(sourceLine);
                if (string.IsNullOrEmpty(source))
                    continue;

                List<int> pipes = FindUnescapedPipePositions(source);
                int cellCount = Math.Max(0, pipes.Count - 1);
                if (cellCount <= 0)
                    continue;

                int cols = Math.Min(table.Cols, cellCount);

                for (int c = 0; c < cols; c++)
                {
                    if (!TryGetTableCellRawSpan(pipes, source.Length, c, out int cellRawStart, out int cellRawEnd))
                        continue;

                    // Leere Zelle trotzdem selektierbar machen
                    if (cellRawEnd <= cellRawStart)
                        cellRawEnd = Math.Min(source.Length, cellRawStart + 1);

                    if (!RangesOverlap(lineSelStart, lineSelEnd, cellRawStart, cellRawEnd))
                        continue;

                    Rectangle cellRect = Rectangle.Inflate(table.GetCellRect(r, c), -1, -1);
                    g.FillRectangle(selectionBrush, cellRect);
                }
            }
        }
    }
    private bool TryGetSelectionSpanForSourceLine(
    int sourceLine,
    MarkdownPosition selectionStart,
    MarkdownPosition selectionEnd,
    out int startCol,
    out int endCol)
    {
        startCol = 0;
        endCol = 0;

        if (sourceLine < selectionStart.Line || sourceLine > selectionEnd.Line)
            return false;

        int lineLen = _doc.GetLineLength(sourceLine);

        startCol = (sourceLine == selectionStart.Line)
            ? Math.Clamp(selectionStart.Column, 0, lineLen)
            : 0;

        endCol = (sourceLine == selectionEnd.Line)
            ? Math.Clamp(selectionEnd.Column, 0, lineLen)
            : lineLen;

        return endCol > startCol;
    }
    private static bool TryGetTableCellRawSpan(
    List<int> pipePositions,
    int lineLength,
    int columnIndex,
    out int start,
    out int end)
    {
        start = 0;
        end = 0;

        if (pipePositions is null || pipePositions.Count < 2)
            return false;

        if (columnIndex < 0 || columnIndex >= pipePositions.Count - 1)
            return false;

        int leftPipe = pipePositions[columnIndex];
        int rightPipe = pipePositions[columnIndex + 1];

        start = Math.Clamp(leftPipe + 1, 0, lineLength);
        end = Math.Clamp(rightPipe, 0, lineLength);

        return true;
    }


    private bool TryGetTaskCheckboxRect(LayoutLine line, out Rectangle rect)
    {
        return TryGetTaskCheckboxRects(line, out _, out rect);
    }

    private bool TryGetTaskCheckboxRects(LayoutLine line, out Rectangle drawRect, out Rectangle hitRect)
    {
        drawRect = Rectangle.Empty;
        hitRect = Rectangle.Empty;

        if (line.Kind != MarkdownBlockKind.List || !line.IsTaskListItem)
            return false;

        if (line.ListContentSourceStart < 0)
            return false;

        string display = line.Projection.DisplayText;
        if (string.IsNullOrEmpty(display))
            return false;

        int srcContentStart = Math.Clamp(line.ListContentSourceStart, 0, line.SourceText.Length);
        int visContentStart = Math.Clamp(line.Projection.SourceToVisual[srcContentStart], 0, display.Length);

        // BuildListProjection: "... + (☐|☑) + ' ' + content"
        int checkboxGlyphVisStart = visContentStart - 2;
        if (checkboxGlyphVisStart < 0 || checkboxGlyphVisStart >= display.Length)
            return false;

        char glyph = display[checkboxGlyphVisStart];
        if (glyph != '☐' && glyph != '☑')
            return false;

        if (!TryGetVisualX(line, checkboxGlyphVisStart, out LayoutSegment? segment, out float x1) ||
            !TryGetVisualX(line, checkboxGlyphVisStart + 1, out _, out float x2))
        {
            return false;
        }

        Rectangle bounds = segment?.Bounds ?? line.Bounds;

        drawRect = Rectangle.FromLTRB(
            (int)Math.Floor(x1),
            bounds.Top + 1,
            Math.Max((int)Math.Ceiling(x2), (int)Math.Floor(x1) + 1),
            bounds.Bottom - 1);

        const int padX = 4;
        hitRect = Rectangle.FromLTRB(
            (int)Math.Floor(x1) - padX,
            bounds.Top,
            (int)Math.Ceiling(x2) + padX,
            bounds.Bottom);

        return true;
    }

    private void DrawTaskCheckboxOverlay(Graphics g, LayoutLine line)
    {
        if (!TryGetTaskCheckboxRects(line, out Rectangle drawRect, out _))
            return;

        string glyph = line.IsTaskChecked ? "☑" : "☐";

        using var backgroundBrush = new SolidBrush(BackColor);
        g.FillRectangle(backgroundBrush, drawRect);

        TextRenderer.DrawText(
            g,
            glyph,
            GetRenderFont(line),
            drawRect.Location,
            ForeColor,
            PlainTextDrawFlags);
    }

    private static bool RangesOverlap(int aStart, int aEnd, int bStart, int bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }


    private static int GetHeadingMarkerLength(string source)
    {
        if (string.IsNullOrEmpty(source)) return 0;

        int i = 0;
        while (i < source.Length && source[i] == ' ')
            i++;

        int hashes = 0;
        while (i < source.Length && source[i] == '#' && hashes < 6)
        {
            hashes++;
            i++;
        }

        if (hashes == 0) return 0;

        // optionales Leerzeichen nach den # berücksichtigen
        if (i < source.Length && source[i] == ' ')
            i++;

        return i;
    }

    private void DrawCaret(Graphics g)
    {
        if (!TryGetCaretContentRectangle(_state.Caret, out Rectangle caretRect))
            return;

        using var brush = new SolidBrush(ForeColor);
        g.FillRectangle(brush, caretRect);
    }

    private bool TryGetCaretContentRectangle(MarkdownPosition position, out Rectangle caretRect)
    {
        caretRect = Rectangle.Empty;

        LayoutLine? line = _layout.GetPreparedLine(position.Line);
        if (line is null)
            return false;

        int srcCol = Math.Clamp(position.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        string display = line.Projection.DisplayText;
        visCol = Math.Clamp(visCol, 0, display.Length);

        if (!TryGetVisualX(line, visCol, out LayoutSegment? segment, out float caretX))
            return false;

        Rectangle bounds = segment?.Bounds ?? line.Bounds;
        int x = (int)Math.Round(caretX);
        int top = bounds.Top + 2;
        int height = Math.Max(1, bounds.Height - 4);

        caretRect = new Rectangle(x, top, 1, height);
        return true;
    }

    private void InvalidateFastInsertRegion(
        Rectangle oldLineBounds,
        Rectangle newLineBounds,
        Rectangle oldCaretBounds,
        Rectangle newCaretBounds)
    {
        Rectangle dirty = Rectangle.Union(oldLineBounds, newLineBounds);

        if (!oldCaretBounds.IsEmpty)
            dirty = Rectangle.Union(dirty, oldCaretBounds);

        if (!newCaretBounds.IsEmpty)
            dirty = Rectangle.Union(dirty, newCaretBounds);

        dirty = Rectangle.Inflate(dirty, 12, 4);
        InvalidateContentRectangle(dirty);
    }

    private void InvalidateContentRectangle(Rectangle contentRect)
    {
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
            return;

        Rectangle clientRect = ContentToClient(contentRect);
        clientRect = Rectangle.Intersect(clientRect, new Rectangle(Point.Empty, ClientSize));
        if (clientRect.Width <= 0 || clientRect.Height <= 0)
            return;

        Invalidate(clientRect, invalidateChildren: false);
    }

    /*private bool HandleShortcuts(KeyEventArgs e)
    {
        if (!e.Control) return false;

        switch (e.KeyCode)
        {
            case Keys.A:
                SelectAllCommand();
                return true;

            case Keys.C:
                CopyCommand();
                return true;

            case Keys.X:
                CutCommand();
                return true;

            case Keys.V:
                PasteCommand();
                return true;

            case Keys.F:
                RequestFind();   // Host zeigt Find-UI (oder direkt Find(...) aufrufen)
                return true;

            case Keys.Z:
                Undo();
                return true;

            case Keys.Y:
                Redo();
                return true;
        }

        return false;
    }*/

    private bool HandleQuoteStructuralKeys(KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Enter or Keys.Back)) return false;

        int lineIndex = _state.Caret.Line;
        string source = _doc.GetLine(lineIndex);
        if (string.IsNullOrEmpty(source)) return false;

        string? quotePrefix = GetQuotePrefix(source);
        if (quotePrefix is null) return false;

        if (e.KeyCode == Keys.Enter)
        {
            ApplyDocumentEdit(() =>
            {
                _state.NewLine(_doc);
                _state.InsertText(_doc, quotePrefix);
                return true;
            });
            return true;
        }

        if (e.KeyCode == Keys.Back)
        {
            LayoutLine? line = _layout.GetPreparedLine(lineIndex);
            if (line is null) return false;

            int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
            int visCol = line.Projection.SourceToVisual[srcCol];

            if (visCol == 0)
            {
                ApplyDocumentEdit(() =>
                {
                    _doc.DeleteRange(new MarkdownPosition(lineIndex, 0), new MarkdownPosition(lineIndex, quotePrefix.Length));
                    _state.SetCaret(new MarkdownPosition(lineIndex, 0), false, _doc);
                    return true;
                });
                return true;
            }
        }

        return false;
    }

    private static string? GetQuotePrefix(string source)
    {
        int i = 0;
        while (i < source.Length && char.IsWhiteSpace(source[i])) i++;

        if (i >= source.Length || source[i] != '>')
            return null;

        int end = i + 1;
        if (end < source.Length && source[end] == ' ')
            end++;

        return source[..end];
    }

    private bool IsCaretAtVisualStart()
    {
        LayoutLine? line = _layout.GetPreparedLine(_state.Caret.Line);
        if (line is null) return false;

        int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        return visCol == 0;
    }

    private void ResetPreferredCaretContentX()
    {
        _preferredCaretContentX = null;
    }

    private bool MoveCaretVertically(int lineDelta, bool shift)
    {
        int targetLine = _state.Caret.Line + lineDelta;
        if (targetLine < 0 || targetLine >= _doc.LineCount)
            return false;

        RefreshLayoutForCaretContext();

        if (!TryGetCaretContentRectangle(_state.Caret, out Rectangle caretRect))
        {
            return lineDelta < 0
                ? _state.MoveUp(_doc, shift)
                : _state.MoveDown(_doc, shift);
        }

        int preferredX = _preferredCaretContentX ?? (caretRect.Left + Math.Max(0, (caretRect.Width - 1) / 2));
        _preferredCaretContentX = preferredX;

        LayoutLine? targetLayoutLine = _layout.GetPreparedLine(targetLine);
        if (targetLayoutLine is null)
        {
            MarkdownPosition fallback = new(targetLine, Math.Min(_state.Caret.Column, _doc.GetLineLength(targetLine)));
            MarkdownPosition beforeFallbackCaret = _state.Caret;
            MarkdownPosition? beforeFallbackAnchor = _state.Anchor;
            _state.SetCaret(fallback, shift, _doc);
            return _state.Caret != beforeFallbackCaret || _state.Anchor != beforeFallbackAnchor;
        }

        int targetY = targetLayoutLine.Bounds.Top + Math.Max(0, targetLayoutLine.Bounds.Height / 2);
        MarkdownPosition beforeCaret = _state.Caret;
        MarkdownPosition? beforeAnchor = _state.Anchor;
        MarkdownPosition targetPosition = _layout.HitTestText(new Point(preferredX, targetY));
        targetPosition = new MarkdownPosition(
            targetLine,
            Math.Clamp(targetPosition.Column, 0, _doc.GetLineLength(targetLine)));

        _state.SetCaret(targetPosition, shift, _doc);
        return _state.Caret != beforeCaret || _state.Anchor != beforeAnchor;
    }

    private void ApplyDocumentEdit(Func<bool> editOperation)
    {
        EndCellEdit(discard: false, move: CellMove.None);
        ResetPreferredCaretContentX();

        string[] beforeLines = _doc.SnapshotLines();
        MarkdownPosition beforeCaret = _state.Caret;
        MarkdownPosition? beforeAnchor = _state.Anchor;

        bool changed = editOperation();
        if (!changed) return;

        _doc.ReparseDirtyBlocks();
        EnsureTrailingEditableLineAfterTerminalTable();

        UndoRecord undo = BuildUndoRecord(beforeLines, beforeCaret, beforeAnchor);
        PushUndo(undo);
        _redo.Clear();

        CleanupRawTableModes();
        ExitRawModesIfCaretOutside();

        RefreshLayoutAfterDocumentChange();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown));
    }

    private bool TryApplyFastSingleLineInsert(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('\n') >= 0 || _state.HasSelection || ContainsFastPathUnsafeMarkdown(text))
            return false;

        EndCellEdit(discard: false, move: CellMove.None);

        MarkdownPosition beforeCaret = _state.Caret;
        MarkdownPosition? beforeAnchor = _state.Anchor;
        int lineIndex = beforeCaret.Line;
        if (lineIndex < 0 || lineIndex >= _doc.LineCount)
            return false;

        if (!CanUseFastPlainTextLine(lineIndex, beforeCaret.Column, deletingBackward: false))
            return false;

        string oldLine = _doc.GetLine(lineIndex);
        Rectangle oldLineBounds = Rectangle.Empty;
        if (_layout.GetPreparedLine(lineIndex) is { } oldLayoutLine)
            oldLineBounds = oldLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(beforeCaret, out Rectangle oldCaretBounds);
        Point scrollBefore = AutoScrollPosition;

        bool changed = _state.InsertText(_doc, text);
        if (!changed)
            return false;

        string newLine = _doc.GetLine(lineIndex);
        var undo = new UndoRecord(
            lineIndex,
            [oldLine],
            [newLine],
            beforeCaret,
            beforeAnchor,
            _state.Caret,
            _state.Anchor);

        if (_state.Caret.Line != lineIndex ||
            _doc.LineCount <= lineIndex ||
            !_layout.TryFastUpdateSimpleTextLine(lineIndex, newLine))
        {
            FinalizeFastEditFallback(undo, allowMerge: true);
            return true;
        }

        Rectangle newLineBounds = oldLineBounds;
        if (_layout.GetPreparedLine(lineIndex) is { } newLayoutLine)
            newLineBounds = newLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(_state.Caret, out Rectangle newCaretBounds);
        bool layoutShifted = oldLineBounds.Height != newLineBounds.Height;

        FinalizeFastEdit(
            undo,
            allowMerge: true,
            scrollBefore,
            structureChanged: layoutShifted,
            invalidateWholeClient: layoutShifted,
            oldLineBounds,
            newLineBounds,
            oldCaretBounds,
            newCaretBounds);

        return true;
    }

    private bool TryApplyFastSingleLineBackspace()
    {
        if (_state.HasSelection)
            return false;

        MarkdownPosition beforeCaret = _state.Caret;
        if (beforeCaret.Column > 0)
            return TryApplyFastSingleLinePlainEdit(deletingBackward: true, () => _state.Backspace(_doc));

        int currentLine = beforeCaret.Line;
        if (currentLine <= 0 || !IsEntireDocumentFastPlainTextEligible())
            return false;

        string previousLine = _doc.GetLine(currentLine - 1);
        string oldLine = _doc.GetLine(currentLine);
        if (ContainsFastPathUnsafeMarkdown(previousLine) || ContainsFastPathUnsafeMarkdown(oldLine))
            return false;

        EndCellEdit(discard: false, move: CellMove.None);

        MarkdownPosition? beforeAnchor = _state.Anchor;
        Rectangle upperBounds = Rectangle.Empty;
        Rectangle lowerBounds = Rectangle.Empty;

        if (_layout.GetPreparedLine(currentLine - 1) is { } previousLayoutLine)
            upperBounds = previousLayoutLine.Bounds;

        if (_layout.GetPreparedLine(currentLine) is { } currentLayoutLine)
            lowerBounds = currentLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(beforeCaret, out Rectangle oldCaretBounds);
        Point scrollBefore = AutoScrollPosition;

        if (!_state.Backspace(_doc))
            return false;

        string mergedLine = _doc.GetLine(currentLine - 1);
        var undo = new UndoRecord(
            currentLine - 1,
            [previousLine, oldLine],
            [mergedLine],
            beforeCaret,
            beforeAnchor,
            _state.Caret,
            _state.Anchor);

        if (ContainsFastPathUnsafeMarkdown(mergedLine) ||
            !_layout.TryFastMergeSimpleTextLines(currentLine - 1, mergedLine))
        {
            FinalizeFastEditFallback(undo, allowMerge: false);
            return true;
        }

        Rectangle mergedBounds = upperBounds;
        if (_layout.GetPreparedLine(currentLine - 1) is { } mergedLayoutLine)
            mergedBounds = mergedLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(_state.Caret, out Rectangle newCaretBounds);

        FinalizeFastEdit(
            undo,
            allowMerge: false,
            scrollBefore,
            structureChanged: true,
            invalidateWholeClient: true,
            upperBounds,
            lowerBounds,
            mergedBounds,
            oldCaretBounds,
            newCaretBounds);

        return true;
    }

    private bool TryApplyFastSingleLineDelete()
    {
        if (_state.HasSelection)
            return false;

        MarkdownPosition beforeCaret = _state.Caret;
        int lineLength = _doc.GetLineLength(beforeCaret.Line);
        if (beforeCaret.Column < lineLength)
            return TryApplyFastSingleLinePlainEdit(deletingBackward: false, () => _state.Delete(_doc));

        return false;
    }

    private bool TryApplyFastPlainTextEnter()
    {
        if (_state.HasSelection || !IsEntireDocumentFastPlainTextEligible())
            return false;

        EndCellEdit(discard: false, move: CellMove.None);

        MarkdownPosition beforeCaret = _state.Caret;
        MarkdownPosition? beforeAnchor = _state.Anchor;
        int lineIndex = beforeCaret.Line;
        if (lineIndex < 0 || lineIndex >= _doc.LineCount)
            return false;

        string oldLine = _doc.GetLine(lineIndex);
        if (ContainsFastPathUnsafeMarkdown(oldLine))
            return false;

        string left = oldLine[..beforeCaret.Column];
        string right = oldLine[beforeCaret.Column..];

        Rectangle oldLineBounds = Rectangle.Empty;
        if (_layout.GetPreparedLine(lineIndex) is { } oldLayoutLine)
            oldLineBounds = oldLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(beforeCaret, out Rectangle oldCaretBounds);
        Point scrollBefore = AutoScrollPosition;

        if (!_state.NewLine(_doc))
            return false;

        var undo = new UndoRecord(
            lineIndex,
            [oldLine],
            [left, right],
            beforeCaret,
            beforeAnchor,
            _state.Caret,
            _state.Anchor);

        if (ContainsFastPathUnsafeMarkdown(left) ||
            ContainsFastPathUnsafeMarkdown(right) ||
            !_layout.TryFastSplitSimpleTextLine(lineIndex, left, right))
        {
            FinalizeFastEditFallback(undo, allowMerge: false);
            return true;
        }

        Rectangle firstBounds = oldLineBounds;
        Rectangle secondBounds = Rectangle.Empty;
        if (_layout.GetPreparedLine(lineIndex) is { } firstLayoutLine)
            firstBounds = firstLayoutLine.Bounds;

        if (_layout.GetPreparedLine(lineIndex + 1) is { } secondLayoutLine)
            secondBounds = secondLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(_state.Caret, out Rectangle newCaretBounds);

        FinalizeFastEdit(
            undo,
            allowMerge: false,
            scrollBefore,
            structureChanged: true,
            invalidateWholeClient: true,
            oldLineBounds,
            firstBounds,
            secondBounds,
            oldCaretBounds,
            newCaretBounds);

        return true;
    }

    private bool TryApplyFastSingleLinePlainEdit(bool deletingBackward, Func<bool> editOperation)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        MarkdownPosition beforeCaret = _state.Caret;
        MarkdownPosition? beforeAnchor = _state.Anchor;
        int lineIndex = beforeCaret.Line;
        if (lineIndex < 0 || lineIndex >= _doc.LineCount)
            return false;

        if (!CanUseFastPlainTextLine(lineIndex, beforeCaret.Column, deletingBackward))
            return false;

        string oldLine = _doc.GetLine(lineIndex);
        Rectangle oldLineBounds = Rectangle.Empty;
        if (_layout.GetPreparedLine(lineIndex) is { } oldLayoutLine)
            oldLineBounds = oldLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(beforeCaret, out Rectangle oldCaretBounds);
        Point scrollBefore = AutoScrollPosition;

        if (!editOperation())
            return false;

        string newLine = _doc.GetLine(lineIndex);
        var undo = new UndoRecord(
            lineIndex,
            [oldLine],
            [newLine],
            beforeCaret,
            beforeAnchor,
            _state.Caret,
            _state.Anchor);

        if (_state.Caret.Line != lineIndex ||
            _doc.LineCount <= lineIndex ||
            !_layout.TryFastUpdateSimpleTextLine(lineIndex, newLine))
        {
            FinalizeFastEditFallback(undo, allowMerge: true);
            return true;
        }

        Rectangle newLineBounds = oldLineBounds;
        if (_layout.GetPreparedLine(lineIndex) is { } newLayoutLine)
            newLineBounds = newLayoutLine.Bounds;

        _ = TryGetCaretContentRectangle(_state.Caret, out Rectangle newCaretBounds);
        bool layoutShifted = oldLineBounds.Height != newLineBounds.Height;

        FinalizeFastEdit(
            undo,
            allowMerge: true,
            scrollBefore,
            structureChanged: layoutShifted,
            invalidateWholeClient: layoutShifted,
            oldLineBounds,
            newLineBounds,
            oldCaretBounds,
            newCaretBounds);

        return true;
    }

    private void FinalizeFastEdit(
        UndoRecord undo,
        bool allowMerge,
        Point scrollBefore,
        bool structureChanged,
        bool invalidateWholeClient,
        params Rectangle[] dirtyContentRegions)
    {
        if (allowMerge)
            PushOrMergeFastSingleLineUndo(undo);
        else
            PushUndo(undo);

        _redo.Clear();

        CleanupRawTableModes();
        ExitRawModesIfCaretOutside();
        InvalidateLayoutContext();

        UpdateAutoScrollMinSizeAfterFastEdit(structureChanged);

        ResetCaretBlink();
        EnsureCaretVisible();

        if (AutoScrollPosition != scrollBefore)
        {
            Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
        }
        else if (invalidateWholeClient)
        {
            InvalidateClientFromDirtyContentTop(dirtyContentRegions);
        }
        else
        {
            InvalidateFastContentRegions(dirtyContentRegions);
        }

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown));
    }

    private void UpdateAutoScrollMinSizeAfterFastEdit(bool structureChanged)
    {
        Size current = AutoScrollMinSize;
        Size desired = _layout.ContentSize;

        if (structureChanged)
        {
            if (current != desired)
                AutoScrollMinSize = desired;
            return;
        }

        int targetWidth = current.Width;
        int targetHeight = current.Height;

        int desiredWidth = Math.Max(ClientSize.Width, desired.Width);
        int desiredHeight = Math.Max(ClientSize.Height, desired.Height);

        if (desiredWidth > current.Width)
            targetWidth = RoundUpToStep(desiredWidth, 128);

        if (desiredHeight > current.Height)
            targetHeight = RoundUpToStep(desiredHeight, 64);

        if (targetWidth != current.Width || targetHeight != current.Height)
            AutoScrollMinSize = new Size(targetWidth, targetHeight);
    }

    private void InvalidateClientFromDirtyContentTop(params Rectangle[] dirtyContentRegions)
    {
        int top = int.MaxValue;

        foreach (Rectangle region in dirtyContentRegions)
        {
            if (region.IsEmpty)
                continue;

            top = Math.Min(top, region.Top);
        }

        if (top == int.MaxValue)
        {
            Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
            return;
        }

        int clientTop = Math.Max(0, top + AutoScrollPosition.Y - 4);
        if (clientTop >= ClientSize.Height)
            return;

        Invalidate(new Rectangle(0, clientTop, ClientSize.Width, ClientSize.Height - clientTop), invalidateChildren: false);
    }

    private static int RoundUpToStep(int value, int step)
    {
        if (step <= 1)
            return Math.Max(1, value);

        int adjusted = Math.Max(1, value);
        return ((adjusted + step - 1) / step) * step;
    }

    private void FinalizeFastEditFallback(UndoRecord undo, bool allowMerge)
    {
        _doc.ReparseDirtyBlocks();
        EnsureTrailingEditableLineAfterTerminalTable();

        if (allowMerge)
            PushOrMergeFastSingleLineUndo(undo);
        else
            PushUndo(undo);

        _redo.Clear();

        CleanupRawTableModes();
        ExitRawModesIfCaretOutside();
        RefreshLayoutAfterDocumentChange();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown));
    }

    private void PushOrMergeFastSingleLineUndo(UndoRecord next)
    {
        if (_undo.Count > 0 &&
            TryMergeFastSingleLineUndo(_undo[^1], next, out UndoRecord merged))
        {
            _undo[^1] = merged;
            return;
        }

        PushUndo(next);
    }

    private static bool TryMergeFastSingleLineUndo(UndoRecord previous, UndoRecord next, out UndoRecord merged)
    {
        merged = default;

        if (previous.StartLine != next.StartLine ||
            previous.OldLines.Length != 1 ||
            previous.NewLines.Length != 1 ||
            next.OldLines.Length != 1 ||
            next.NewLines.Length != 1)
        {
            return false;
        }

        if (previous.BeforeAnchor.HasValue ||
            previous.AfterAnchor.HasValue ||
            next.BeforeAnchor.HasValue ||
            next.AfterAnchor.HasValue)
        {
            return false;
        }

        if (previous.AfterCaret != next.BeforeCaret)
            return false;

        if (!string.Equals(previous.NewLines[0], next.OldLines[0], StringComparison.Ordinal))
            return false;

        merged = new UndoRecord(
            previous.StartLine,
            previous.OldLines,
            next.NewLines,
            previous.BeforeCaret,
            previous.BeforeAnchor,
            next.AfterCaret,
            next.AfterAnchor);

        return true;
    }

    private bool CanUseFastPlainTextLine(int lineIndex, int caretColumn, bool deletingBackward)
    {
        LayoutLine? line = _layout.GetPreparedLine(lineIndex);
        if (line is null || line.IsImagePreview)
            return false;

        if (line.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank or MarkdownBlockKind.List))
            return false;

        int editColumn = deletingBackward ? caretColumn - 1 : caretColumn;
        if (editColumn < 0 || editColumn > line.SourceText.Length)
            return false;

        if (line.Kind == MarkdownBlockKind.List)
        {
            int contentStart = Math.Max(0, line.ListContentSourceStart);
            return editColumn >= contentStart;
        }

        if (line.Kind == MarkdownBlockKind.Blank && string.IsNullOrWhiteSpace(line.SourceText))
            return false;

        int leadingWhitespace = CountLeadingWhitespace(line.SourceText);
        return editColumn >= leadingWhitespace;
    }

    private static int CountLeadingWhitespace(string text)
    {
        int count = 0;
        while (count < text.Length && char.IsWhiteSpace(text[count]))
            count++;

        return count;
    }

    private bool IsEntireDocumentFastPlainTextEligible()
    {
        if (!_canSideScroll)
            return false;

        foreach (MarkdownBlock block in _doc.Blocks)
        {
            if (block.Kind is not (MarkdownBlockKind.Paragraph or MarkdownBlockKind.Blank))
                return false;
        }

        for (int i = 0; i < _doc.LineCount; i++)
        {
            if (ContainsFastPathUnsafeMarkdown(_doc.GetLine(i)))
                return false;
        }

        return true;
    }

    private void InvalidateFastContentRegions(params Rectangle[] dirtyContentRegions)
    {
        Rectangle dirty = Rectangle.Empty;
        bool hasDirty = false;

        foreach (Rectangle region in dirtyContentRegions)
        {
            if (region.IsEmpty)
                continue;

            dirty = hasDirty ? Rectangle.Union(dirty, region) : region;
            hasDirty = true;
        }

        if (!hasDirty)
        {
            Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);
            return;
        }

        dirty = Rectangle.Inflate(dirty, 12, 4);
        InvalidateContentRectangle(dirty);
    }

    private static bool ContainsFastPathUnsafeMarkdown(string text)
    {
        foreach (char ch in text)
        {
            if (ch is '\\' or '`' or '*' or '_' or '~' or '[' or ']' or '!' or '<' or '>' or '#' or '|' or ':' or '-')
                return true;
        }

        return false;
    }

    private bool TryGetAffectedLineRange(out int startLine, out int endLine)
    {
        if (_doc.LineCount <= 0)
        {
            startLine = endLine = 0;
            return false;
        }

        if (!_state.HasSelection)
        {
            startLine = Math.Clamp(_state.Caret.Line, 0, _doc.LineCount - 1);
            endLine = startLine;
            return true;
        }

        var (selectionStart, selectionEnd) = _state.GetSelection();
        startLine = Math.Clamp(selectionStart.Line, 0, _doc.LineCount - 1);
        endLine = Math.Clamp(selectionEnd.Line, 0, _doc.LineCount - 1);

        if (selectionEnd.Column == 0 && endLine > startLine)
            endLine--;

        return endLine >= startLine;
    }

    private void SetSelectionToLineRange(int startLine, IReadOnlyList<string> lines)
    {
        int endLine = startLine + lines.Count - 1;
        MarkdownPosition selectionStart = new(startLine, 0);
        MarkdownPosition selectionEnd = new(endLine, lines[^1].Length);
        _state.Restore(selectionEnd, selectionStart, _doc);
    }

    private static string ApplyHeadingLevel(string line, int level)
    {
        string headingMarker = new('#', Math.Clamp(level, 1, 6));
        int contentStart = GetHeadingContentStart(line);
        string content = contentStart >= line.Length ? string.Empty : line[contentStart..];
        return string.IsNullOrEmpty(content)
            ? $"{headingMarker} "
            : $"{headingMarker} {content}";
    }

    private static int GetHeadingContentStart(string line)
    {
        if (string.IsNullOrEmpty(line))
            return 0;

        int firstNonWhitespace = GetLeadingWhitespaceLength(line);
        int index = firstNonWhitespace;
        int hashes = 0;

        while (index < line.Length && hashes < 6 && line[index] == '#')
        {
            index++;
            hashes++;
        }

        if (hashes > 0 && (index >= line.Length || char.IsWhiteSpace(line[index])))
        {
            while (index < line.Length && char.IsWhiteSpace(line[index]))
                index++;

            return index;
        }

        return firstNonWhitespace;
    }

    private static int GetHeadingPrefixLength(string line)
    {
        int index = 0;
        while (index < line.Length && line[index] == '#')
            index++;

        if (index < line.Length && line[index] == ' ')
            index++;

        return index;
    }

    private static string AddQuotePrefix(string line)
    {
        int insertAt = GetLeadingWhitespaceLength(line);
        return line.Insert(insertAt, "> ");
    }

    private static string RemoveQuotePrefix(string line)
    {
        return TryGetQuotePrefixRange(line, out int start, out int length)
            ? line.Remove(start, length)
            : line;
    }

    private static bool TryGetQuotePrefixRange(string line, out int start, out int length)
    {
        Match match = QuotePrefixRegex.Match(line);
        if (!match.Success)
        {
            start = 0;
            length = 0;
            return false;
        }

        start = match.Groups["indent"].Length;
        length = match.Length - start;
        return length > 0;
    }

    private static int AdjustColumnForQuoteAddition(int column, string line)
    {
        int insertAt = GetLeadingWhitespaceLength(line);
        return column < insertAt ? column : column + 2;
    }

    private static int AdjustColumnForQuoteRemoval(int column, string line)
    {
        if (!TryGetQuotePrefixRange(line, out int start, out int length))
            return column;

        if (column <= start)
            return column;

        if (column <= start + length)
            return start;

        return column - length;
    }

    private static int GetLeadingWhitespaceLength(string line)
    {
        int index = 0;
        while (index < line.Length && char.IsWhiteSpace(line[index]))
            index++;

        return index;
    }

    private static string CreateFenceMarker(string content)
    {
        int longestRun = 0;
        int currentRun = 0;

        foreach (char ch in content)
        {
            if (ch == '`')
            {
                currentRun++;
                longestRun = Math.Max(longestRun, currentRun);
            }
            else
            {
                currentRun = 0;
            }
        }

        return new string('`', Math.Max(3, longestRun + 1));
    }

    private static string NormalizeMarkdownInsertion(string markdown)
    {
        return (markdown ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim('\n');
    }

    private void Undo()
    {
        EndCellEdit(discard: false, move: CellMove.None);
        if (_undo.Count == 0) return;

        UndoRecord prev = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);

        _redo.Add(prev);
        ApplyUndoRecord(prev, undo: true);
    }

    private void Redo()
    {
        EndCellEdit(discard: false, move: CellMove.None);
        if (_redo.Count == 0) return;

        UndoRecord next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);

        _undo.Add(next);
        ApplyUndoRecord(next, undo: false);
    }

    private void ApplyUndoRecord(UndoRecord record, bool undo)
    {
        int removeCount = undo ? record.NewLines.Length : record.OldLines.Length;
        int endLine = record.StartLine + removeCount - 1;
        IReadOnlyList<string> replacement = undo ? record.OldLines : record.NewLines;

        _doc.ReplaceLines(record.StartLine, endLine, replacement);
        _doc.ReparseAll();
        EnsureTrailingEditableLineAfterTerminalTable();

        _rawTableStartLines.Clear();
        _rawCodeFenceStartLine = null;

        MarkdownPosition caret = undo ? record.BeforeCaret : record.AfterCaret;
        MarkdownPosition? anchor = undo ? record.BeforeAnchor : record.AfterAnchor;
        _state.Restore(caret, anchor, _doc);

        RefreshLayoutAfterDocumentChange();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown));
    }

    private UndoRecord BuildUndoRecord(
        IReadOnlyList<string> beforeLines,
        MarkdownPosition beforeCaret,
        MarkdownPosition? beforeAnchor)
    {
        IReadOnlyList<string> afterLines = _doc.Lines;

        int prefix = 0;
        int beforeCount = beforeLines.Count;
        int afterCount = afterLines.Count;

        while (prefix < beforeCount &&
               prefix < afterCount &&
               string.Equals(beforeLines[prefix], afterLines[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        int suffix = 0;
        while (suffix < beforeCount - prefix &&
               suffix < afterCount - prefix &&
               string.Equals(beforeLines[beforeCount - 1 - suffix], afterLines[afterCount - 1 - suffix], StringComparison.Ordinal))
        {
            suffix++;
        }

        int removedCount = beforeCount - prefix - suffix;
        int insertedCount = afterCount - prefix - suffix;

        string[] oldLines = SliceLines(beforeLines, prefix, removedCount);
        string[] newLines = SliceLines(afterLines, prefix, insertedCount);

        return new UndoRecord(
            prefix,
            oldLines,
            newLines,
            beforeCaret,
            beforeAnchor,
            _state.Caret,
            _state.Anchor);
    }

    private static string[] SliceLines(IReadOnlyList<string> lines, int start, int count)
    {
        if (count <= 0)
            return Array.Empty<string>();

        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = lines[start + i];

        return result;
    }

    private void PushUndo(UndoRecord s)
    {
        _undo.Add(s);
        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);
    }

    private void RebuildLayout()
    {
        SyncCodeFenceRawModeWithCaret();
        ExitRawModesIfCaretOutside();

        RebuildLayoutCore();
    }

    private void RefreshLayoutAfterDocumentChange()
    {
        InvalidateLayoutContext();
        RefreshLayoutForCaretContext(force: true);
    }

    private bool RefreshLayoutForCaretContext(bool force = false)
    {
        bool rebuilt = false;

        while (true)
        {
            SyncCodeFenceRawModeWithCaret();
            ExitRawModesIfCaretOutside();

            if (force || !IsLayoutContextCurrent())
            {
                RebuildLayoutCore();
                rebuilt = true;
                force = false;
            }

            if (!NormalizeCaretOutOfTables())
                return rebuilt;

            force = true;
        }
    }

    private void RebuildLayoutCore()
    {
        IReadOnlySet<int>? rawCodeFenceStarts = GetRawCodeFenceStarts();
        IReadOnlySet<int>? rawSourceLines = GetRawSourceLinesForCaretBlock();
        IReadOnlySet<int>? rawInlineLines = GetRawInlineLinesForCaret();

        _layout.Rebuild(
            _doc,
            ClientSize,
            Font,
            _boldFont,
            _monoFont,
            _canSideScroll,
            DeviceDpi,
            DeviceDpi,
            _state.Caret.Line,
            TryGetCachedImageSize,
            _rawTableStartLines,
            rawCodeFenceStarts,
            rawSourceLines,
            rawInlineLines,
            controlHandle: Handle);

        AutoScrollMinSize = _layout.ContentSize;
        UpdateLayoutContext(rawCodeFenceStarts, rawSourceLines, rawInlineLines);
    }


    private bool NormalizeCaretOutOfTables()
    {
        if (!_layout.IsTableSourceLine(_state.Caret.Line))
            return false;

        if (TryGetContainingTableByLine(_state.Caret.Line, out var t))
        {
            if (!_rawTableStartLines.Contains(t.StartLine))
            {
                _rawTableStartLines.Add(t.StartLine);
                return true;
            }
        }

        int nearest = _layout.GetNearestTextLine(_state.Caret.Line, preferForward: true);
        if (nearest < 0)
        {
            MarkdownPosition target = new(0, 0);
            bool changed = _state.Caret != target || _state.Anchor.HasValue;
            if (changed)
                _state.Restore(target, null, _doc);

            return changed;
        }

        int col = Math.Min(_state.Caret.Column, _doc.GetLineLength(nearest));
        MarkdownPosition next = new(nearest, col);
        bool caretChanged = _state.Caret != next || _state.Anchor.HasValue;
        if (caretChanged)
            _state.Restore(next, null, _doc);

        return caretChanged;
    }

    private bool IsLayoutContextCurrent()
    {
        if (!_layoutContextInitialized)
            return false;

        if (_layoutContextDocumentVersion != _doc.Version)
            return false;

        if (_layoutContextRawCodeFenceStart != _rawCodeFenceStartLine)
            return false;

        int? rawInlineLine = ResolveRawInlineLine();
        if (_layoutContextRawInlineLine != rawInlineLine)
            return false;

        if (TryGetContainingRawSourceBlockRange(_state.Caret.Line, out int rawSourceStart, out int rawSourceEnd))
        {
            if (_layoutContextRawSourceStart != rawSourceStart || _layoutContextRawSourceEnd != rawSourceEnd)
                return false;
        }
        else if (_layoutContextRawSourceStart >= 0 || _layoutContextRawSourceEnd >= 0)
        {
            return false;
        }

        if (_layoutContextRawTableStarts.Length != _rawTableStartLines.Count)
            return false;

        foreach (int start in _layoutContextRawTableStarts)
        {
            if (!_rawTableStartLines.Contains(start))
                return false;
        }

        return true;
    }

    private void UpdateLayoutContext(
        IReadOnlySet<int>? rawCodeFenceStarts,
        IReadOnlySet<int>? rawSourceLines,
        IReadOnlySet<int>? rawInlineLines)
    {
        _layoutContextInitialized = true;
        _layoutContextDocumentVersion = _doc.Version;
        _layoutContextRawCodeFenceStart = rawCodeFenceStarts?.Count > 0 ? rawCodeFenceStarts.First() : null;
        _layoutContextRawInlineLine = rawInlineLines?.Count > 0 ? rawInlineLines.First() : null;

        if (rawSourceLines is not null && rawSourceLines.Count > 0)
        {
            _layoutContextRawSourceStart = rawSourceLines.Min();
            _layoutContextRawSourceEnd = rawSourceLines.Max();
        }
        else
        {
            _layoutContextRawSourceStart = -1;
            _layoutContextRawSourceEnd = -1;
        }

        _layoutContextRawTableStarts = _rawTableStartLines.Count == 0
            ? Array.Empty<int>()
            : _rawTableStartLines.OrderBy(x => x).ToArray();
    }

    private void InvalidateLayoutContext()
    {
        _layoutContextInitialized = false;
        _layoutContextDocumentVersion = -1;
        _layoutContextRawCodeFenceStart = null;
        _layoutContextRawInlineLine = null;
        _layoutContextRawSourceStart = -1;
        _layoutContextRawSourceEnd = -1;
        _layoutContextRawTableStarts = Array.Empty<int>();
    }

    private int? ResolveRawInlineLine()
    {
        int line = _state.Caret.Line;
        if (line < 0 || line >= _doc.LineCount)
            return null;

        if (IsInsideTable(line))
            return null;

        if (GetContainingCodeFenceStartLine(line).HasValue)
            return null;

        return line;
    }

    private void EnterRawTableSourceFromGrid(TableLayout tableLayout, int gridRow, int gridCol)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        TableBlock table = tableLayout.Block;
        _rawTableStartLines.Add(table.StartLine);
        RefreshLayoutForCaretContext(force: true);

        int srcLine = MapGridRowToSourceLine(table, gridRow);
        string src = _doc.GetLine(srcLine);
        int srcCol = MapGridColToSourceColumn(src, gridCol);

        _state.SetCaret(new MarkdownPosition(srcLine, srcCol), false, _doc);

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    private void EnterRawTableSourceFromActiveCell()
    {
        if (_activeTable is null) return;

        int start = _activeTable.StartLine;
        int row = _activeTable.EditRow;
        int col = _activeTable.EditCol;

        EndCellEdit(discard: false, move: CellMove.None);

        var table = _doc.Blocks.OfType<TableBlock>().FirstOrDefault(t => t.StartLine == start);
        if (table is null) return;

        _rawTableStartLines.Add(start);
        RefreshLayoutForCaretContext(force: true);

        int srcLine = MapGridRowToSourceLine(table, row);
        string src = _doc.GetLine(srcLine);
        int srcCol = MapGridColToSourceColumn(src, col);

        _state.SetCaret(new MarkdownPosition(srcLine, srcCol), false, _doc);

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();
    }

    private static int MapGridRowToSourceLine(TableBlock table, int gridRow)
    {
        if (gridRow <= 0) return table.StartLine;
        int line = table.StartLine + gridRow + 1;
        return Math.Clamp(line, table.StartLine, table.EndLine);
    }

    private static int MapGridColToSourceColumn(string sourceLine, int gridCol)
    {
        if (string.IsNullOrEmpty(sourceLine))
            return 0;

        var pipes = FindUnescapedPipePositions(sourceLine);

        if (pipes.Count >= 2)
        {
            int cellCount = pipes.Count - 1;
            int c = Math.Clamp(gridCol, 0, cellCount - 1);

            int leftPipe = pipes[c];
            int rightPipe = pipes[c + 1];

            int pos = leftPipe + 1;
            while (pos < rightPipe && pos < sourceLine.Length && sourceLine[pos] == ' ')
                pos++;

            if (pos >= rightPipe)
                pos = Math.Min(leftPipe + 1, sourceLine.Length);

            return Math.Clamp(pos, 0, sourceLine.Length);
        }

        return 0;
    }

    private void EnsureCaretVisible()
    {
        if (_cellEditor is not null) return;

        LayoutLine? line = _layout.GetPreparedLine(_state.Caret.Line);
        if (line is null) return;

        int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        string display = line.Projection.DisplayText;
        visCol = Math.Clamp(visCol, 0, display.Length);

        if (!TryGetVisualX(line, visCol, out LayoutSegment? segment, out float caretXf))
            return;

        Rectangle caretBounds = segment?.Bounds ?? line.Bounds;
        int caretX = (int)Math.Round(caretXf);
        int caretY = caretBounds.Top;

        int viewLeft = -AutoScrollPosition.X;
        int viewTop = -AutoScrollPosition.Y;
        int viewRight = viewLeft + ClientSize.Width;
        int viewBottom = viewTop + ClientSize.Height;

        int targetX = viewLeft;
        int targetY = viewTop;
        bool change = false;

        if (_canSideScroll)
        {
            if (caretX < viewLeft)
            {
                targetX = Math.Max(0, caretX - 20);
                change = true;
            }
            else if (caretX > viewRight - 10)
            {
                targetX = Math.Max(0, caretX - ClientSize.Width + 40);
                change = true;
            }
        }

        if (caretY < viewTop)
        {
            targetY = Math.Max(0, caretY - 20);
            change = true;
        }
        else if (caretY + caretBounds.Height > viewBottom)
        {
            targetY = Math.Max(0, caretY + caretBounds.Height - ClientSize.Height + 20);
            change = true;
        }

        if (change)
            AutoScrollPosition = new Point(targetX, targetY);
    }

    private void ClearHeadingFontCache()
    {
        foreach (var f in _headingFontCache.Values)
            f.Dispose();
        _headingFontCache.Clear();
    }

    private Font GetHeadingFontCached(int level)
    {
        level = Math.Clamp(level, 1, 6);

        if (_headingFontCache.TryGetValue(level, out var f))
            return f;

        f = MarkdownTypography.CreateHeadingFont(Font, level);
        _headingFontCache[level] = f;
        return f;
    }

    private Font GetRenderFont(LayoutLine line)
        => line.Kind == MarkdownBlockKind.Heading ? GetHeadingFontCached(line.HeadingLevel) : GetFont(line.FontRole);

    private static Font GetOrCreateInlineFont(Dictionary<int, Font> cache, Font baseFont, InlineStyle style, bool isCode, bool isFootnoteReference, Font monoFont)
    {
        if (style == InlineStyle.None && !isFootnoteReference && !isCode)
            return baseFont;

        InlineStyle normalized = style & ~InlineStyle.Code;

        int key = ((int)normalized & 0xFF)
                  | (isCode ? 0x100 : 0)
                  | (isFootnoteReference ? 0x200 : 0)
                  | (((int)baseFont.Style & 0xFF) << 9);

        if (cache.TryGetValue(key, out var f))
            return f;

        Font seed = isCode ? monoFont : baseFont;
        f = CreateRunDisplayFont(seed, normalized, isFootnoteReference);
        cache[key] = f;
        return f;
    }

    private static Font CreateRunDisplayFont(Font seed, InlineStyle normalized, bool isFootnoteReference)
    {
        Font styled = InlineMarkdown.CreateStyledFont(seed, normalized);
        if (!isFootnoteReference)
            return styled;

        try
        {
            return new Font(
                styled.FontFamily,
                Math.Max(1f, styled.Size * FootnoteFontScale),
                styled.Style,
                styled.Unit,
                styled.GdiCharSet,
                styled.GdiVerticalFont);
        }
        finally
        {
            styled.Dispose();
        }
    }

    private void DrawInlineRuns(Graphics g, LayoutLine line)
    {
        if (line.Segments.Count == 0)
        {
            DrawInlineRuns(g, line, new Point(line.TextX, line.Bounds.Top + 1));
            return;
        }

        foreach (LayoutSegment segment in line.Segments)
            DrawInlineRuns(g, line, segment);
    }

    private void DrawInlineRuns(Graphics g, LayoutLine line, Point contentTextStart)
    {
        string display = line.Projection.DisplayText;
        if (string.IsNullOrEmpty(display)) return;

        Font baseFont = GetRenderFont(line);

        if (line.InlineRuns is null || line.InlineRuns.Count == 0)
        {
            DrawTextGdiPlus(g, display, baseFont, contentTextStart, ForeColor);
            return;
        }

        if (TryDrawSimplePlainLine(g, line, contentTextStart, display, baseFont))
            return;

        // Use LayoutEngine's cached offsets so text positions match cursor/selection exactly.
        float[] offsets = _layout.GetVisualOffsets(line);
        int visCol = 0;
        float baseX = contentTextStart.X;
        var cache = new Dictionary<int, Font>();

        using var codeBrush = new SolidBrush(_inlineCodeBg);
        using var codePen = new Pen(_inlineCodeBorder);

        try
        {
            foreach (var run in line.InlineRuns)
            {
                float runX = SnapVisualX(baseX + OffsetAt(offsets, visCol));

                if (run.IsImage)
                {
                    Size imageSize = GetInlineImageSize(run.Source);
                    int imageY = line.Bounds.Top + Math.Max(0, (line.Bounds.Height - imageSize.Height) / 2);
                    DrawInlineImage(g, run, new Rectangle((int)Math.Round(runX), imageY, imageSize.Width, imageSize.Height));
                    visCol += Math.Max(1, run.VisualLength);
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateInlineFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference, _monoFont);
                int visColEnd = visCol + run.Text.Length;

                if (run.IsFootnoteReference)
                {
                    int runTextY = line.Bounds.Top + Math.Max(0, (line.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY - FootnoteRaiseY, _linkColor);
                    visCol = visColEnd;
                    continue;
                }

                if (run.IsLink)
                {
                    int runTextY = line.Bounds.Top + Math.Max(0, (line.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY, _linkColor);

                    float runEndX = SnapVisualX(baseX + OffsetAt(offsets, visColEnd));
                    float linkWidth = Math.Max(1f, runEndX - runX);
                    int height = MeasureHeight(g, runFont);
                    float underlineY = runTextY + Math.Max(1, height - 1);
                    using var linkPen = new Pen(_linkColor, 1f);
                    g.DrawLine(linkPen, runX, underlineY, runX + linkWidth, underlineY);

                    visCol = visColEnd;
                    continue;
                }

                if (!isCode)
                {
                    int runTextY = line.Bounds.Top + Math.Max(0, (line.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY, ForeColor);
                    visCol = visColEnd;
                    continue;
                }

                // Code chip: offsets already include left/right padding
                float chipEndX = SnapVisualX(baseX + OffsetAt(offsets, visColEnd));
                int chipXi = (int)Math.Round(runX);
                int chipWi = Math.Max(1, (int)Math.Round(chipEndX - runX));
                int textH = MeasureHeight(g, runFont);
                int chipH = Math.Max(1, textH + InlineCodePadY * 2);
                int chipY = contentTextStart.Y + Math.Max(0, (line.Bounds.Height - chipH) / 2);

                var chip = new Rectangle(chipXi, chipY, chipWi, chipH);

                g.FillRectangle(codeBrush, chip);
                g.DrawRectangle(codePen, chip.X, chip.Y, Math.Max(1, chip.Width - 1), Math.Max(1, chip.Height - 1));

                int textY = chip.Y + Math.Max(0, (chip.Height - textH) / 2);
                DrawTextGdiPlus(g, run.Text, runFont, runX + InlineCodePadX, textY, ForeColor);

                visCol = visColEnd;
            }
        }
        finally
        {
            foreach (var f in cache.Values)
                f.Dispose();
        }
    }

    private void DrawInlineRuns(Graphics g, LayoutLine line, LayoutSegment segment)
    {
        if (string.IsNullOrEmpty(segment.DisplayText) || segment.InlineRuns.Count == 0 && segment.HiddenLeadingVisualCount >= segment.VisualOffsets.Length - 1)
            return;

        Font baseFont = GetRenderFont(line);
        Point contentTextStart = new(segment.Bounds.X, segment.Bounds.Top + 1);

        if (segment.InlineRuns.Count == 0)
        {
            if (!string.IsNullOrEmpty(segment.DisplayText))
                DrawTextGdiPlus(g, segment.DisplayText, baseFont, contentTextStart, ForeColor);

            return;
        }

        if (TryDrawSimplePlainSegment(g, segment, contentTextStart, baseFont))
            return;

        float[] offsets = segment.VisualOffsets;
        int visCol = segment.HiddenLeadingVisualCount;
        float baseX = contentTextStart.X;
        var cache = new Dictionary<int, Font>();

        using var codeBrush = new SolidBrush(_inlineCodeBg);
        using var codePen = new Pen(_inlineCodeBorder);

        try
        {
            foreach (InlineRun run in segment.InlineRuns)
            {
                float runX = SnapVisualX(baseX + OffsetAt(offsets, visCol));

                if (run.IsImage)
                {
                    Size imageSize = GetInlineImageSize(run.Source);
                    int imageY = segment.Bounds.Top + Math.Max(0, (segment.Bounds.Height - imageSize.Height) / 2);
                    DrawInlineImage(g, run, new Rectangle((int)Math.Round(runX), imageY, imageSize.Width, imageSize.Height));
                    visCol += Math.Max(1, run.VisualLength);
                    continue;
                }

                if (string.IsNullOrEmpty(run.Text))
                    continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateInlineFont(cache, baseFont, run.Style, isCode, run.IsFootnoteReference, _monoFont);
                int visColEnd = visCol + run.Text.Length;

                if (run.IsFootnoteReference)
                {
                    int runTextY = segment.Bounds.Top + Math.Max(0, (segment.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY - FootnoteRaiseY, _linkColor);
                    visCol = visColEnd;
                    continue;
                }

                if (run.IsLink)
                {
                    int runTextY = segment.Bounds.Top + Math.Max(0, (segment.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY, _linkColor);

                    float runEndX = SnapVisualX(baseX + OffsetAt(offsets, visColEnd));
                    float linkWidth = Math.Max(1f, runEndX - runX);
                    int height = MeasureHeight(g, runFont);
                    float underlineY = runTextY + Math.Max(1, height - 1);
                    using var linkPen = new Pen(_linkColor, 1f);
                    g.DrawLine(linkPen, runX, underlineY, runX + linkWidth, underlineY);

                    visCol = visColEnd;
                    continue;
                }

                if (!isCode)
                {
                    int runTextY = segment.Bounds.Top + Math.Max(0, (segment.Bounds.Height - MeasureHeight(g, runFont)) / 2);
                    DrawTextGdiPlus(g, run.Text, runFont, runX, runTextY, ForeColor);
                    visCol = visColEnd;
                    continue;
                }

                float chipEndX = SnapVisualX(baseX + OffsetAt(offsets, visColEnd));
                int chipXi = (int)Math.Round(runX);
                int chipWi = Math.Max(1, (int)Math.Round(chipEndX - runX));
                int textH = MeasureHeight(g, runFont);
                int chipH = Math.Max(1, textH + InlineCodePadY * 2);
                int chipY = contentTextStart.Y + Math.Max(0, (segment.Bounds.Height - chipH) / 2);

                var chip = new Rectangle(chipXi, chipY, chipWi, chipH);

                g.FillRectangle(codeBrush, chip);
                g.DrawRectangle(codePen, chip.X, chip.Y, Math.Max(1, chip.Width - 1), Math.Max(1, chip.Height - 1));

                int textY = chip.Y + Math.Max(0, (chip.Height - textH) / 2);
                DrawTextGdiPlus(g, run.Text, runFont, runX + InlineCodePadX, textY, ForeColor);

                visCol = visColEnd;
            }
        }
        finally
        {
            foreach (Font font in cache.Values)
                font.Dispose();
        }
    }

    private bool TryDrawSimplePlainLine(Graphics g, LayoutLine line, Point contentTextStart, string display, Font baseFont)
    {
        if (line.InlineRuns.Count != 1)
            return false;

        InlineRun run = line.InlineRuns[0];
        if (run.IsImage || run.IsLink || run.IsFootnoteReference || run.Style != InlineStyle.None)
            return false;

        if (!string.Equals(run.Text, display, StringComparison.Ordinal))
            return false;

        TextRenderer.DrawText(
            g,
            display,
            baseFont,
            contentTextStart,
            ForeColor,
            PlainTextDrawFlags);
        return true;
    }

    private bool TryDrawSimplePlainSegment(Graphics g, LayoutSegment segment, Point contentTextStart, Font baseFont)
    {
        if (segment.InlineRuns.Count != 1)
            return false;

        InlineRun run = segment.InlineRuns[0];
        if (run.IsImage || run.IsLink || run.IsFootnoteReference || run.Style != InlineStyle.None)
            return false;

        if (!string.Equals(run.Text, segment.DisplayText, StringComparison.Ordinal))
            return false;

        TextRenderer.DrawText(
            g,
            segment.DisplayText,
            baseFont,
            contentTextStart,
            ForeColor,
            PlainTextDrawFlags);
        return true;
    }

    private static float OffsetAt(float[] offsets, int col)
        => col >= 0 && col < offsets.Length ? offsets[col] : (offsets.Length > 0 ? offsets[^1] : 0f);

    private static float SnapVisualX(float x)
        => (float)Math.Round(x, MidpointRounding.AwayFromZero);

    private static int DistanceToY(Rectangle bounds, int y)
    {
        if (y < bounds.Top)
            return bounds.Top - y;

        if (y > bounds.Bottom)
            return y - bounds.Bottom;

        return 0;
    }

    private LayoutSegment GetNearestSegment(LayoutLine line, int y)
    {
        LayoutSegment best = line.Segments[0];
        int bestDistance = DistanceToY(best.Bounds, y);

        for (int i = 1; i < line.Segments.Count; i++)
        {
            LayoutSegment candidate = line.Segments[i];
            int distance = DistanceToY(candidate.Bounds, y);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private bool TryGetVisualX(LayoutLine line, int visualCol, out LayoutSegment? segment, out float x)
    {
        segment = null;

        if (line.Segments.Count == 0)
        {
            float[] offsets = _layout.GetVisualOffsets(line);
            if (offsets.Length == 0)
            {
                x = SnapVisualX(line.TextX);
                return false;
            }

            visualCol = Math.Clamp(visualCol, 0, offsets.Length - 1);
            if (visualCol == offsets.Length - 1 && TryMeasureSimplePlainLineEndOffset(line, out float endOffset))
            {
                x = SnapVisualX(line.TextX + endOffset);
                return true;
            }

            x = SnapVisualX(line.TextX + offsets[visualCol]);
            return true;
        }

        foreach (LayoutSegment candidate in line.Segments)
        {
            if (visualCol > candidate.VisualEnd)
                continue;

            int localVisualCol = Math.Clamp(visualCol - candidate.VisualStart, 0, candidate.VisualOffsets.Length - 1);
            segment = candidate;
            if (localVisualCol == candidate.VisualOffsets.Length - 1 &&
                TryMeasureSimplePlainSegmentEndOffset(line, candidate, out float segmentEndOffset))
            {
                x = SnapVisualX(candidate.Bounds.X + segmentEndOffset);
                return true;
            }

            x = SnapVisualX(candidate.Bounds.X + OffsetAt(candidate.VisualOffsets, localVisualCol));
            return true;
        }

        LayoutSegment last = line.Segments[^1];
        int localCol = Math.Clamp(visualCol - last.VisualStart, 0, last.VisualOffsets.Length - 1);
        segment = last;
        if (localCol == last.VisualOffsets.Length - 1 &&
            TryMeasureSimplePlainSegmentEndOffset(line, last, out float lastSegmentEndOffset))
        {
            x = SnapVisualX(last.Bounds.X + lastSegmentEndOffset);
            return true;
        }

        x = SnapVisualX(last.Bounds.X + OffsetAt(last.VisualOffsets, localCol));
        return true;
    }

    private bool TryGetVisualX(Graphics g, LayoutLine line, int visualCol, out LayoutSegment? segment, out float x)
    {
        segment = null;

        if (line.Segments.Count == 0)
        {
            float[] offsets = _layout.GetVisualOffsets(line);
            if (offsets.Length == 0)
            {
                x = SnapVisualX(line.TextX);
                return false;
            }

            visualCol = Math.Clamp(visualCol, 0, offsets.Length - 1);
            if (visualCol == offsets.Length - 1 && TryMeasureSimplePlainLineEndOffset(g, line, out float endOffset))
            {
                x = SnapVisualX(line.TextX + endOffset);
                return true;
            }

            x = SnapVisualX(line.TextX + offsets[visualCol]);
            return true;
        }

        foreach (LayoutSegment candidate in line.Segments)
        {
            if (visualCol > candidate.VisualEnd)
                continue;

            int localVisualCol = Math.Clamp(visualCol - candidate.VisualStart, 0, candidate.VisualOffsets.Length - 1);
            segment = candidate;
            if (localVisualCol == candidate.VisualOffsets.Length - 1 &&
                TryMeasureSimplePlainSegmentEndOffset(g, line, candidate, out float segmentEndOffset))
            {
                x = SnapVisualX(candidate.Bounds.X + segmentEndOffset);
                return true;
            }

            x = SnapVisualX(candidate.Bounds.X + OffsetAt(candidate.VisualOffsets, localVisualCol));
            return true;
        }

        LayoutSegment last = line.Segments[^1];
        int localCol = Math.Clamp(visualCol - last.VisualStart, 0, last.VisualOffsets.Length - 1);
        segment = last;
        if (localCol == last.VisualOffsets.Length - 1 &&
            TryMeasureSimplePlainSegmentEndOffset(g, line, last, out float lastSegmentEndOffset))
        {
            x = SnapVisualX(last.Bounds.X + lastSegmentEndOffset);
            return true;
        }

        x = SnapVisualX(last.Bounds.X + OffsetAt(last.VisualOffsets, localCol));
        return true;
    }

    private bool TryMeasureSimplePlainLineEndOffset(LayoutLine line, out float endOffset)
    {
        endOffset = 0f;

        using Graphics g = CreateGraphics();
        g.PageUnit = GraphicsUnit.Pixel;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        return TryMeasureSimplePlainLineEndOffset(g, line, out endOffset);
    }

    private bool TryMeasureSimplePlainLineEndOffset(Graphics g, LayoutLine line, out float endOffset)
    {
        endOffset = 0f;

        string display = line.Projection.DisplayText;
        if (string.IsNullOrEmpty(display) ||
            line.InlineRuns.Count != 1)
        {
            return false;
        }

        InlineRun run = line.InlineRuns[0];
        if (run.IsImage || run.IsLink || run.IsFootnoteReference || run.Style != InlineStyle.None)
            return false;

        if (!string.Equals(run.Text, display, StringComparison.Ordinal))
            return false;

        Size proposed = new(100000, 1000);
        endOffset = TextRenderer.MeasureText(g, display, GetRenderFont(line), proposed, PlainTextMeasureFlags).Width;
        return true;
    }

    private bool TryMeasureSimplePlainSegmentEndOffset(LayoutLine line, LayoutSegment segment, out float endOffset)
    {
        endOffset = 0f;

        using Graphics g = CreateGraphics();
        g.PageUnit = GraphicsUnit.Pixel;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        return TryMeasureSimplePlainSegmentEndOffset(g, line, segment, out endOffset);
    }

    private bool TryMeasureSimplePlainSegmentEndOffset(Graphics g, LayoutLine line, LayoutSegment segment, out float endOffset)
    {
        endOffset = 0f;

        if (string.IsNullOrEmpty(segment.DisplayText) ||
            segment.InlineRuns.Count != 1)
        {
            return false;
        }

        InlineRun run = segment.InlineRuns[0];
        if (run.IsImage || run.IsLink || run.IsFootnoteReference || run.Style != InlineStyle.None)
            return false;

        if (!string.Equals(run.Text, segment.DisplayText, StringComparison.Ordinal))
            return false;

        Size proposed = new(100000, 1000);
        endOffset = TextRenderer.MeasureText(g, segment.DisplayText, GetRenderFont(line), proposed, PlainTextMeasureFlags).Width;
        return true;
    }

    private float GetSegmentVisualX(Graphics g, LayoutLine line, LayoutSegment segment, int localVisualCol)
    {
        localVisualCol = Math.Clamp(localVisualCol, 0, segment.VisualOffsets.Length - 1);
        if (localVisualCol == segment.VisualOffsets.Length - 1 &&
            TryMeasureSimplePlainSegmentEndOffset(g, line, segment, out float endOffset))
        {
            return segment.Bounds.X + endOffset;
        }

        return segment.Bounds.X + OffsetAt(segment.VisualOffsets, localVisualCol);
    }

    private float MeasureVisualPrefix(LayoutLine line, int visualCols)
    {
        float[] offsets = _layout.GetVisualOffsets(line);
        if (offsets.Length == 0)
            return 0;

        visualCols = Math.Clamp(visualCols, 0, offsets.Length - 1);
        return offsets[visualCols];
    }

    private float MeasureVisualPrefix(Graphics g, LayoutLine line, int visualCols)
        => MeasureVisualPrefix(line, visualCols);

    private void ResetCaretBlink()
    {
        _caretVisible = true;
        _caretTimer.Stop();
        if (Focused) _caretTimer.Start();
    }

    private Font GetFont(LineFontRole role) => role switch
    {
        LineFontRole.Bold => _boldFont,
        LineFontRole.Mono => _monoFont,
        _ => Font
    };

    private static Font CreateMonoFont(float size)
    {
        string[] candidates = { "Cascadia Mono", "Consolas", "Courier New" };
        foreach (string name in candidates)
        {
            try
            {
                using var ff = new FontFamily(name);
                return new Font(ff, size);
            }
            catch { }
        }

        return new Font(SystemFonts.DefaultFont.FontFamily, size);
    }

    private Point ClientToContent(Point p)
        => new(p.X - AutoScrollPosition.X, p.Y - AutoScrollPosition.Y);

    private Rectangle ContentToClient(Rectangle r)
        => new(r.X + AutoScrollPosition.X, r.Y + AutoScrollPosition.Y, r.Width, r.Height);

    private static bool IsHorizontalRule(MarkdownBlockKind kind)
        => kind == MarkdownBlockKind.HorizontalRule;


    // -----------------------------
    // Table grid editing
    // -----------------------------

    private void BeginTableCellEdit(TableLayout table, int row, int col)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        TableModel model = TableModel.FromBlock(table.Block);
        model.Normalize();

        row = Math.Clamp(row, 0, model.RowCount - 1);
        col = Math.Clamp(col, 0, model.ColumnCount - 1);

        _activeTable = new ActiveTableSession
        {
            StartLine = table.StartLine,
            EndLine = table.EndLine,
            Model = model,
            EditRow = row,
            EditCol = col
        };

        Rectangle cellContent = table.GetCellRect(row, col);
        BeginCellEditor(cellContent, model.Rows[row][col]);
    }

    private void BeginCellEditor(Rectangle cellRectContent, string text)
    {
        _suppressCellLostFocus = true;

        _cellEditor?.Dispose();
        _cellEditor = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = false,
            Text = text,
            Bounds = Rectangle.Inflate(ContentToClient(cellRectContent), -4, -4),
            Font = Font,
            BackColor = _cellEditorBack,
            ForeColor = _cellEditorFore,
            ContextMenuStrip = ContextMenuStrip
        };

        _cellEditor.KeyDown += CellEditor_KeyDown;
        _cellEditor.LostFocus += CellEditor_LostFocus;
        _cellEditor.MouseDoubleClick += CellEditor_MouseDoubleClick;

        Controls.Add(_cellEditor);
        _cellEditor.Focus();
        _cellEditor.SelectAll();

        _suppressCellLostFocus = false;
    }

    private void CellEditor_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        EnterRawTableSourceFromActiveCell();
    }

    private void CellEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_cellEditor is null) return;

        if (e.KeyCode == Keys.Escape)
        {
            EndCellEdit(discard: true, move: CellMove.None);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            EndCellEdit(discard: false, move: CellMove.Next);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Tab)
        {
            EndCellEdit(discard: false, move: e.Shift ? CellMove.Previous : CellMove.Next);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
    }

    private int GetVisualLineStartSourceColumn(int sourceLine)
    {
        LayoutLine? line = _layout.GetPreparedLine(sourceLine);
        if (line is null) return 0;

        int[] v2s = line.Projection.VisualToSource;
        if (v2s.Length == 0) return 0;

        return Math.Clamp(v2s[0], 0, line.SourceText.Length);
    }

    private int GetVisualLineEndSourceColumn(int sourceLine)
    {
        LayoutLine? line = _layout.GetPreparedLine(sourceLine);
        if (line is null) return _doc.GetLineLength(sourceLine);

        int[] v2s = line.Projection.VisualToSource;
        if (v2s.Length == 0) return _doc.GetLineLength(sourceLine);

        return Math.Clamp(v2s[v2s.Length - 1], 0, line.SourceText.Length);
    }

    private void CellEditor_LostFocus(object? sender, EventArgs e)
    {
        if (_suppressCellLostFocus) return;
        EndCellEdit(discard: false, move: CellMove.None);
    }

    private void EndCellEdit(bool discard, CellMove move)
    {
        if (_activeTable is null || _cellEditor is null)
            return;

        _suppressCellLostFocus = true;

        int row = _activeTable.EditRow;
        int col = _activeTable.EditCol;

        if (!discard)
        {
            _activeTable.Model.Normalize();
            _activeTable.Model.Rows[row][col] = _cellEditor.Text ?? string.Empty;

            int targetRow = row;
            int targetCol = col;
            bool reopen = move is CellMove.Next or CellMove.Previous;

            if (move == CellMove.Next)
            {
                targetCol++;
                if (targetCol >= _activeTable.Model.ColumnCount)
                {
                    targetCol = 0;
                    targetRow++;

                    if (targetRow >= _activeTable.Model.RowCount)
                    {
                        targetRow = row;
                        targetCol = col;
                        reopen = false;
                    }
                }
            }
            else if (move == CellMove.Previous)
            {
                targetCol--;
                if (targetCol < 0)
                {
                    if (targetRow > 0)
                    {
                        targetRow--;
                        targetCol = _activeTable.Model.ColumnCount - 1;
                    }
                    else
                    {
                        targetCol = 0;
                    }
                }
            }

            string[] beforeLines = _doc.SnapshotLines();
            MarkdownPosition beforeCaret = _state.Caret;
            MarkdownPosition? beforeAnchor = _state.Anchor;

            IReadOnlyList<string> tableLines = _activeTable.Model.ToMarkdownLines();
            _doc.ReplaceLines(_activeTable.StartLine, _activeTable.EndLine, tableLines);
            _doc.ReparseAll();
            EnsureTrailingEditableLineAfterTerminalTable();

            UndoRecord undo = BuildUndoRecord(beforeLines, beforeCaret, beforeAnchor);

            CleanupRawTableModes();
            ExitRawModesIfCaretOutside();

            PushUndo(undo);
            _redo.Clear();

            RefreshLayoutAfterDocumentChange();
            Invalidate();

            MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown));

            int reopenRow = targetRow;
            int reopenCol = targetCol;
            int tableStart = _activeTable.StartLine;

            RemoveCellEditor();

            if (_layout.TryGetTableByStartLine(tableStart, out var relaidTable))
            {
                if (_activeTable is not null)
                {
                    _activeTable.EndLine = relaidTable.EndLine;
                    _activeTable.EditRow = Math.Clamp(reopenRow, 0, _activeTable.Model.RowCount - 1);
                    _activeTable.EditCol = Math.Clamp(reopenCol, 0, _activeTable.Model.ColumnCount - 1);

                    if (reopen)
                    {
                        Rectangle cell = relaidTable.GetCellRect(_activeTable.EditRow, _activeTable.EditCol);
                        string txt = _activeTable.Model.Rows[_activeTable.EditRow][_activeTable.EditCol];
                        BeginCellEditor(cell, txt);
                    }
                    else
                    {
                        _activeTable = null;
                    }
                }
            }
            else
            {
                _activeTable = null;
            }
        }
        else
        {
            RemoveCellEditor();
            _activeTable = null;
        }

        _suppressCellLostFocus = false;
        Focus();
    }

    private void RemoveCellEditor()
    {
        if (_cellEditor is null) return;

        Controls.Remove(_cellEditor);
        _cellEditor.KeyDown -= CellEditor_KeyDown;
        _cellEditor.LostFocus -= CellEditor_LostFocus;
        _cellEditor.MouseDoubleClick -= CellEditor_MouseDoubleClick;

        _cellEditor.Dispose();
        _cellEditor = null;
    }

    private void RepositionCellEditor()
    {
        if (_cellEditor is null || _activeTable is null) return;

        if (!_layout.TryGetTableByStartLine(_activeTable.StartLine, out var table))
            return;

        int row = Math.Clamp(_activeTable.EditRow, 0, table.Rows - 1);
        int col = Math.Clamp(_activeTable.EditCol, 0, table.Cols - 1);

        Rectangle rect = table.GetCellRect(row, col);
        _cellEditor.Bounds = Rectangle.Inflate(ContentToClient(rect), -4, -4);
    }

    private sealed class ActiveTableSession
    {
        public required int StartLine { get; set; }
        public required int EndLine { get; set; }
        public required TableModel Model { get; init; }
        public required int EditRow { get; set; }
        public required int EditCol { get; set; }
    }

    private enum CellMove
    {
        None,
        Next,
        Previous
    }

    private readonly record struct UndoRecord(
        int StartLine,
        string[] OldLines,
        string[] NewLines,
        MarkdownPosition BeforeCaret,
        MarkdownPosition? BeforeAnchor,
        MarkdownPosition AfterCaret,
        MarkdownPosition? AfterAnchor);
}
