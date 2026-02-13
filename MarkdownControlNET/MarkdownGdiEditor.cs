using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;



namespace MarkdownGdi;

public sealed class MarkdownChangedEventArgs : EventArgs
{
    public string Markdown { get; }
    public MarkdownChangedEventArgs(string markdown) => Markdown = markdown;
}


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



public enum EditorThemeMode
{
    System,
    Light,
    Dark
}


public sealed class MarkdownGdiEditor : ScrollableControl, ISupportInitialize
{
    // ... deine bestehenden Felder ...

    private bool _isInitializing;
    private bool _pendingInitialRefresh = true;

    // ... restliche Felder unverändert ...

    private readonly DocumentModel _doc = new();
    private readonly LayoutEngine _layout = new();
    private readonly EditorState _state = new();

    private static readonly Regex TaskMarkerRegex =
    new(@"\[(?<mark>[ xX])\]", RegexOptions.Compiled);


    // Tables can be displayed non-destructively as raw source
    private readonly HashSet<int> _rawTableStartLines = new();
    private readonly Dictionary<int, Font> _headingFontCache = new();

    private readonly List<EditorSnapshot> _undo = new();
    private readonly List<EditorSnapshot> _redo = new();

    private readonly System.Windows.Forms.Timer _caretTimer;
    private bool _caretVisible = true;
    private bool _mouseSelecting;

    private Font _boldFont;
    private Font _monoFont;

    private TextBox? _cellEditor;
    private ActiveTableSession? _activeTable;
    private bool _suppressCellLostFocus;

    private const int MaxUndo = 250;

    private int? _rawCodeFenceStartLine;

    private static readonly TextFormatFlags MeasureFlags =
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

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
        // Manuelles Setzen deaktiviert Auto-Sync bewusst
        AllowAutoThemeChange = false;
        ApplyTheme(darkTheme, raiseEvent: true);
    }

    private bool IsInDesigner
        => LicenseManager.UsageMode == LicenseUsageMode.Designtime || (Site?.DesignMode ?? false);

    private void ApplyThemeFromSystem(bool raiseEvent)
    {
        bool dark = ReadWindowsAppsUseDarkTheme();
        ApplyTheme(dark, raiseEvent);
    }

    private static bool ReadWindowsAppsUseDarkTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);

            object? v = key?.GetValue("AppsUseLightTheme");
            return v switch
            {
                int i => i == 0,   // 0 = dark
                long l => l == 0L,
                _ => false         // fallback: light
            };
        }
        catch
        {
            return false;
        }
    }

    private void ApplyTheme(bool darkTheme, bool raiseEvent)
    {
        bool old = _isDarkTheme;
        _isDarkTheme = darkTheme;

        if (darkTheme)
            ApplyDarkPalette();
        else
            ApplyLightPalette();

        if (_cellEditor is not null)
        {
            _cellEditor.BackColor = _tableCellBg;
            _cellEditor.ForeColor = ForeColor;
        }

        Invalidate(new Rectangle(Point.Empty, ClientSize), invalidateChildren: false);

        if (raiseEvent && old != _isDarkTheme)
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(old, _isDarkTheme));
    }

    private void ApplyLightPalette()
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

        _horizontalRuleColor = Color.Silver;
        _tableOuterBorderColor = Color.Silver;

        _tableDraftBg = Color.FromArgb(255, 252, 242);
        _tableDraftBorder = Color.FromArgb(230, 190, 120);
        _tableDraftHintColor = Color.DarkGoldenrod;
    }

    private void ApplyDarkPalette()
    {
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.FromArgb(230, 230, 230);

        _selectionColor = Color.FromArgb(120, 88, 145, 255);
        _quoteBarColor = Color.FromArgb(110, 118, 130);

        _codeBg = Color.FromArgb(42, 45, 52);
        _tableHeaderBg = Color.FromArgb(48, 52, 60);
        _tableCellBg = Color.FromArgb(36, 39, 46);
        _tableGrid = Color.FromArgb(74, 79, 89);

        _inlineCodeBg = Color.FromArgb(56, 60, 68);
        _inlineCodeBorder = Color.FromArgb(88, 94, 106);

        _horizontalRuleColor = Color.FromArgb(108, 116, 130);
        _tableOuterBorderColor = Color.FromArgb(118, 126, 140);

        _tableDraftBg = Color.FromArgb(58, 50, 38);
        _tableDraftBorder = Color.FromArgb(176, 132, 68);
        _tableDraftHintColor = Color.FromArgb(226, 191, 128);
    }

    private void UpdateSystemThemeSubscription()
    {
        bool shouldHook = !IsDisposed && !IsInDesigner && AllowAutoThemeChange;

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
        if (!AllowAutoThemeChange || IsDisposed) return;

        if (e.Category is not (UserPreferenceCategory.General
                               or UserPreferenceCategory.Color
                               or UserPreferenceCategory.VisualStyle))
            return;

        if (!IsHandleCreated) return;

        void Apply() => ApplyThemeFromSystem(raiseEvent: true);

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

    private EditorThemeMode _themeMode = EditorThemeMode.System;
    private bool _isDarkThemeEffective;

    private Color _hrColor = Color.Silver;
    private Color _tableOuterBorderColor = Color.Silver;
    private Color _tableDraftBg = Color.FromArgb(255, 252, 242);
    private Color _tableDraftBorder = Color.FromArgb(230, 190, 120);
    private Color _tableDraftHint = Color.DarkGoldenrod;
    private Color _cellEditorBack = Color.White;
    private Color _cellEditorFore = Color.Black;
    private Color _horizontalRuleColor = Color.Silver;
    private Color _tableDraftHintColor = Color.DarkGoldenrod;


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
            ApplyTheme();
        }
    }

    public void SetLightTheme() => ThemeMode = EditorThemeMode.Light;
    public void SetDarkTheme() => ThemeMode = EditorThemeMode.Dark;
    public void SetSystemTheme() => ThemeMode = EditorThemeMode.System;


    private void ApplyTheme(bool rebuildLayout = true)
    {
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
            return;
        }

        if (!TryApplyPendingInitialRefresh())
        {
            RebuildLayout();
            RepositionCellEditor();
            Invalidate();
        }
    }

    // --- Theme state ---
    private bool _allowAutoThemeChange = true;
    private bool _isDarkTheme;
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

            if (_allowAutoThemeChange)
                ApplyThemeFromSystem(raiseEvent: true);

            UpdateSystemThemeSubscription();
        }
    }

    [Browsable(false)]
    public bool IsDarkTheme => _isDarkTheme;


    private bool ResolveEffectiveDarkMode()
    {
        return _themeMode switch
        {
            EditorThemeMode.Dark => true,
            EditorThemeMode.Light => false,
            _ => IsDarkColor(Parent?.BackColor ?? SystemColors.Window)
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
    }

    private void EnsureSystemThemeSync()
    {
        if (_themeMode != EditorThemeMode.System)
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

    private readonly record struct AdmonitionPalette(
        Color Bar,
        Color Background,
        Color Border,
        Color TitleColor,
        string Icon);

    private AdmonitionPalette GetAdmonitionPalette(AdmonitionKind kind)
    {
        if (!_isDarkTheme)
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
        set
        {
            EndCellEdit(discard: false, move: CellMove.None);

            _doc.LoadMarkdown(value ?? string.Empty);
            EnsureTrailingEditableLineAfterTerminalTable();

            _rawTableStartLines.Clear();
            _rawCodeFenceStartLine = null;

            _state.Restore(new MarkdownPosition(0, 0), null, _doc);
            _undo.Clear();
            _redo.Clear();

            RebuildLayout();
            NormalizeCaretOutOfTables();
            Invalidate();
        }
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

    // verhindert Designer-Serialisierung von Text zusätzlich
    private bool ShouldSerializeText() => false;

    // Designer-Serialization-Helfer
    private bool ShouldSerializeMarkdown() => !string.IsNullOrEmpty(Markdown);
    private void ResetMarkdown() => Markdown = string.Empty;

    void ISupportInitialize.BeginInit()
    {
        _isInitializing = true;
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
        => IsHandleCreated && ClientSize.Width > 0 && ClientSize.Height > 0;

    private bool TryApplyPendingInitialRefresh()
    {
        if (!_pendingInitialRefresh) return false;
        if (_isInitializing) return false;
        if (!CanApplyInitialLayout()) return false;

        _pendingInitialRefresh = false;

        RebuildLayout();
        NormalizeCaretOutOfTables();
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

        // Initiale Theme-Anwendung (ohne Event)
        if (!IsInDesigner)
            ApplyThemeFromSystem(raiseEvent: false);
        else
            ApplyTheme(darkTheme: false, raiseEvent: false);

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
            _cellEditor?.Dispose();
        }

        base.Dispose(disposing);
    }


    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (!IsInDesigner)
        {
            if (AllowAutoThemeChange)
                ApplyThemeFromSystem(raiseEvent: false);

            UpdateSystemThemeSubscription();
        }

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
            RebuildLayout();
            RepositionCellEditor();
            Invalidate();
        }
    }


    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (!TryApplyPendingInitialRefresh())
        {
            RebuildLayout();
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

        Focus();
        Point content = ClientToContent(e.Location);

        if (_layout.TryHitTestTableCell(content, out var th))
        {
            _mouseSelecting = false;
            BeginTableCellEdit(th.Table, th.Row, th.Col);
            return;
        }

        if (_cellEditor is not null)
            EndCellEdit(discard: false, move: CellMove.None);

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

        // Always rebuild so Raw/Inline source mode follows caret
        RebuildLayout();

        NormalizeCaretOutOfTables();

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
            Cursor = IsPointOverTaskCheckbox(content) ? Cursors.Hand : Cursors.IBeam;
        }

        if (!_mouseSelecting) return;
        if (_cellEditor is not null) return;

        var pos = _layout.HitTestText(ClientToContent(e.Location));
        _state.SetCaret(pos, shift: true, _doc);

        // Always rebuild so Raw/Inline source mode follows caret
        RebuildLayout();

        NormalizeCaretOutOfTables();

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

    private bool TryBuildTaskCheckboxHit(Point contentPoint, out LayoutLine line, out Rectangle hitRect)
    {
        line = null!;
        hitRect = Rectangle.Empty;

        var pos = _layout.HitTestText(contentPoint);
        LayoutLine? candidate = _layout.GetLine(pos.Line);
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

        if (e.KeyChar == '>' && IsCaretAtVisualStart())
        {
            ApplyDocumentEdit(() => _state.InsertText(_doc, "> "));
            e.Handled = true;
            return;
        }

        ApplyDocumentEdit(() => _state.InsertText(_doc, e.KeyChar.ToString()));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_cellEditor is not null) return;

        if (HandleShortcuts(e))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

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

        switch (e.KeyCode)
        {
            case Keys.Left:
                movedCaret = _state.MoveLeft(_doc, shift);
                break;
            case Keys.Right:
                movedCaret = _state.MoveRight(_doc, shift);
                break;
            case Keys.Up:
                movedCaret = _state.MoveUp(_doc, shift);
                break;
            case Keys.Down:
                movedCaret = _state.MoveDown(_doc, shift);
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
                ApplyDocumentEdit(() => _state.Backspace(_doc));
                break;
            case Keys.Delete:
                ApplyDocumentEdit(() => _state.Delete(_doc));
                break;
            case Keys.Enter:
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
            // Immer neu aufbauen, damit Raw/Inline-Source-Modus dem Caret folgt
            RebuildLayout();

            NormalizeCaretOutOfTables();
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
        var set = GetContainingRawSourceBlockLineSet(_state.Caret.Line);
        return set is null || set.Count == 0 ? null : set;
    }


    // NEW
    private HashSet<int>? GetContainingRawSourceBlockLineSet(int caretLine)
    {
        if (caretLine < 0 || caretLine >= _doc.LineCount)
            return null;

        foreach (var b in _doc.Blocks)
        {
            if (caretLine < b.StartLine || caretLine > b.EndLine)
                continue;

            // Tabellen bleiben bei bestehender Tabellenlogik
            if (b is TableBlock)
                return null;

            // CodeFence über bestehende Fence-Logik
            if (b is CodeFenceBlock)
                return null;

            // Für diese Blöcke Source zeigen, solange Caret im Block ist
            if (b is HeadingBlock or QuoteBlock or ListBlock || b.Kind == MarkdownBlockKind.HorizontalRule)
            {
                var lines = new HashSet<int>();
                for (int i = Math.Max(0, b.StartLine); i <= Math.Min(_doc.LineCount - 1, b.EndLine); i++)
                    lines.Add(i);
                return lines;
            }

            return null;
        }

        return null;
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
        RebuildLayout();

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

        NormalizeCaretOutOfTables();
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

        RebuildLayout();

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
        e.Graphics.Clear(BackColor);

        e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        Rectangle viewport = new(
            -AutoScrollPosition.X,
            -AutoScrollPosition.Y,
            ClientSize.Width,
            ClientSize.Height);

        e.Graphics.SetClip(viewport);

        // Draw content first
        foreach (var line in _layout.GetVisibleLines(viewport))
            DrawLine(e.Graphics, line);

        foreach (var table in _layout.GetVisibleTables(viewport))
            DrawTable(e.Graphics, table);

        // Then selection overlay
        DrawSelection(e.Graphics, viewport);

        if (Focused && _caretVisible && _cellEditor is null)
            DrawCaret(e.Graphics);
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        if (_themeMode == EditorThemeMode.System)
            ApplyTheme();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        if (_themeMode == EditorThemeMode.System)
            ApplyTheme();
    }

    protected override void OnParentForeColorChanged(EventArgs e)
    {
        base.OnParentForeColorChanged(e);
        if (_themeMode == EditorThemeMode.System)
            ApplyTheme();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_themeMode == EditorThemeMode.System)
            ApplyTheme();
    }


    private void DrawLine(Graphics g, LayoutLine line)
    {
        if (IsHorizontalRule(line.Kind))
        {
            int x1 = line.TextX;
            int x2 = Math.Max(line.TextX + 64, AutoScrollMinSize.Width - 16);
            int y = line.Bounds.Top + (line.Bounds.Height / 2);

            using var p = new Pen(_horizontalRuleColor, 1f);
            g.DrawLine(p, x1, y, x2, y);
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
                DrawTextGdiPlus(g, hint, hintFont, new Point(line.TextX + w + 8, line.Bounds.Top + 1), _tableDraftHintColor);
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
            DrawInlineRuns(g, line, new Point(line.TextX, line.Bounds.Top + 1));
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
                    ? MeasureInlineRunsWidthForTable(runs, baseFont)
                    : MeasureWidth(fallbackText, baseFont);

                int x = textRect.Left;
                string align = table.GetColumnAlignment(c).ToString();

                if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                    x = textRect.Left + Math.Max(0, (textRect.Width - textWidth) / 2);
                else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
                    x = textRect.Right - textWidth;

                x = Math.Max(textRect.Left, x);
                int y = textRect.Top + Math.Max(0, (textRect.Height - MeasureHeight(baseFont)) / 2);

                if (runs.Count > 0)
                    DrawInlineRunsInCell(g, runs, baseFont, new Point(x, y), textRect, ForeColor);
                else
                    DrawTextGdiPlus(g, fallbackText, baseFont, new Point(x, y), ForeColor);
            }
        }

        using var outer = new Pen(_tableOuterBorderColor, 1.2f);
        g.DrawRectangle(outer, table.Bounds);
    }

    private int MeasureInlineRunsWidthForTable(IReadOnlyList<InlineRun> runs, Font baseFont)
    {
        if (runs.Count == 0) return 0;

        int width = 0;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in runs)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font f = GetOrCreateTableRunFont(cache, baseFont, run.Style, isCode);

                int w = MeasureWidth(run.Text, f);
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

    private Font GetOrCreateTableRunFont(Dictionary<int, Font> cache, Font baseFont, InlineStyle style, bool isCode)
    {
        InlineStyle normalized = style & ~InlineStyle.Code;

        int key = ((int)normalized & 0xFF)
                  | (isCode ? 0x100 : 0)
                  | (((int)baseFont.Style & 0xFF) << 9);

        if (cache.TryGetValue(key, out var f))
            return f;

        Font seed = isCode ? _monoFont : baseFont;
        f = InlineMarkdown.CreateStyledFont(seed, normalized);
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
                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateTableRunFont(cache, baseFont, run.Style, isCode);

                if (!isCode)
                {
                    DrawTextGdiPlus(g, run.Text, runFont, new Point(x, start.Y), color);
                    x += MeasureWidth(run.Text, runFont);
                    continue;
                }

                int textW = MeasureWidth(run.Text, runFont);
                int textH = MeasureHeight(runFont);

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

    private static int MeasureHeight(Font font) => (int)Math.Ceiling(font.GetHeight());

    private static int MeasureWidth(string text, Font font)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);

        var size = g.MeasureString(text, font, int.MaxValue, MeasureStringFormat);
        return (int)Math.Ceiling(size.Width);
    }

    private static void DrawTextGdiPlus(Graphics g, string text, Font font, Point pt, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, pt, DrawStringFormat);
    }

    private void DrawSelection(Graphics g, Rectangle viewport)
    {
        if (!_state.HasSelection) return;

        var (start, end) = _state.GetSelection();
        using var brush = new SolidBrush(_selectionColor);

        // A) Text-/Line-Selection wie bisher
        for (int srcLine = start.Line; srcLine <= end.Line; srcLine++)
        {
            LayoutLine? line = _layout.GetLine(srcLine);
            if (line is null) continue;
            if (line.Bounds.Bottom < viewport.Top || line.Bounds.Top > viewport.Bottom) continue;

            int srcLen = line.SourceText.Length;
            int selStartSrc = (srcLine == start.Line) ? start.Column : 0;
            int selEndSrc = (srcLine == end.Line) ? end.Column : srcLen;

            selStartSrc = Math.Clamp(selStartSrc, 0, srcLen);
            selEndSrc = Math.Clamp(selEndSrc, 0, srcLen);
            if (selEndSrc <= selStartSrc) continue;

            int visStart = line.Projection.SourceToVisual[selStartSrc];
            int visEnd = line.Projection.SourceToVisual[selEndSrc];

            string display = line.Projection.DisplayText;
            visStart = Math.Clamp(visStart, 0, display.Length);
            visEnd = Math.Clamp(visEnd, 0, display.Length);
            if (visEnd <= visStart) continue;

            int x1 = line.TextX + MeasureVisualPrefix(line, visStart);
            int x2 = line.TextX + MeasureVisualPrefix(line, visEnd);

            var rect = new Rectangle(
                x1,
                line.Bounds.Top + 1,
                Math.Max(1, x2 - x1),
                Math.Max(1, line.Bounds.Height - 2));

            g.FillRectangle(brush, rect);
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


    private bool TryGetHiddenPrefixSelectionRect(
    LayoutLine line,
    int selStartSrc,
    int selEndSrc,
    out Rectangle rect)
    {
        rect = Rectangle.Empty;

        int srcLen = line.SourceText.Length;
        if (srcLen <= 0) return false;

        int hiddenPrefixLen = GetHiddenPrefixSourceLength(line);
        if (hiddenPrefixLen <= 0) return false;

        int overlapStart = Math.Max(0, Math.Max(selStartSrc, 0));
        int overlapEnd = Math.Min(hiddenPrefixLen, selEndSrc);
        if (overlapEnd <= overlapStart) return false;

        Font f = GetRenderFont(line);

        // komplette versteckte Prefix-Breite (dynamisch, aus echtem Source-Text)
        string fullHidden = line.SourceText[..hiddenPrefixLen];
        int fullHiddenW = Math.Max(1, MeasureWidth(fullHidden, f));

        int beforeW = overlapStart > 0
            ? MeasureWidth(line.SourceText[..overlapStart], f)
            : 0;

        int untilW = overlapEnd > 0
            ? MeasureWidth(line.SourceText[..overlapEnd], f)
            : 0;

        int xBase = line.TextX - fullHiddenW;
        int x1 = xBase + beforeW;
        int x2 = xBase + untilW;

        if (x2 <= x1) x2 = x1 + 1;

        rect = new Rectangle(
            x1,
            line.Bounds.Top + 1,
            x2 - x1,
            Math.Max(1, line.Bounds.Height - 2));

        return true;
    }

    private static int GetHiddenPrefixSourceLength(LayoutLine line)
    {
        if (line.SourceText.Length == 0)
            return 0;

        int[] v2s = line.Projection.VisualToSource;
        if (v2s is null || v2s.Length == 0)
            return 0;

        // Source-Index des ersten sichtbaren Visual-Zeichens.
        // Alles davor gilt als "visuell versteckter Prefix".
        int firstVisibleSrc = v2s[0];
        return Math.Clamp(firstVisibleSrc, 0, line.SourceText.Length);
    }

    private void DrawSelectionForVisualElements(Graphics g, LayoutLine line, int selStartSrc, int selEndSrc, Brush brush)
    {
        // Horizontal rule: gesamte visuelle Linie markieren, sobald Quellbereich selektiert ist
        if (line.Kind == MarkdownBlockKind.HorizontalRule)
        {
            if (!RangesOverlap(selStartSrc, selEndSrc, 0, line.SourceText.Length))
                return;

            int x1 = line.TextX;
            int x2 = Math.Max(line.TextX + 64, AutoScrollMinSize.Width - 16);
            int y = line.Bounds.Top + (line.Bounds.Height / 2);

            var hrRect = Rectangle.FromLTRB(x1, y - 3, x2, y + 3);
            g.FillRectangle(brush, hrRect);
            return;
        }

        // Task-Checkbox (☐ / ☑)
        if (line.Kind == MarkdownBlockKind.List &&
            line.IsTaskListItem &&
            line.TaskMarkerSourceStart >= 0 &&
            line.TaskMarkerSourceLength > 0)
        {
            int mStart = line.TaskMarkerSourceStart;
            int mEnd = mStart + line.TaskMarkerSourceLength;

            if (RangesOverlap(selStartSrc, selEndSrc, mStart, mEnd) &&
                TryGetTaskCheckboxRect(line, out var checkboxRect))
            {
                g.FillRectangle(brush, checkboxRect);
            }
        }

        // Quote-Bar links markieren, wenn Quote-Präfix selektiert wurde
        if (line.Kind == MarkdownBlockKind.Quote)
        {
            string? quotePrefix = GetQuotePrefix(line.SourceText);
            if (quotePrefix is not null && RangesOverlap(selStartSrc, selEndSrc, 0, quotePrefix.Length))
            {
                var barRect = new Rectangle(
                    10,
                    line.Bounds.Top + 1,
                    5,
                    Math.Max(1, line.Bounds.Height - 2));

                g.FillRectangle(brush, barRect);
            }
        }

        // Heading-Markerbereich (#, ##, ...)
        if (line.Kind == MarkdownBlockKind.Heading)
        {
            int markerLen = GetHeadingMarkerLength(line.SourceText);
            if (markerLen > 0 && RangesOverlap(selStartSrc, selEndSrc, 0, markerLen))
            {
                // Marker werden im Rendern ausgeblendet -> visuellen Bereich links vor Heading-Text highlighten
                var markerRect = new Rectangle(
                    Math.Max(0, line.TextX - 12),
                    line.Bounds.Top + 1,
                    10,
                    Math.Max(1, line.Bounds.Height - 2));

                g.FillRectangle(brush, markerRect);
            }
        }
    }

    private bool TryGetTaskCheckboxRect(LayoutLine line, out Rectangle rect)
    {
        rect = Rectangle.Empty;

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

        int x1 = line.TextX + MeasureVisualPrefix(line, checkboxGlyphVisStart);
        int x2 = line.TextX + MeasureVisualPrefix(line, checkboxGlyphVisStart + 1);

        const int padX = 4;
        rect = Rectangle.FromLTRB(
            x1 - padX,
            line.Bounds.Top,
            x2 + padX,
            line.Bounds.Bottom);

        return true;
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
        LayoutLine? line = _layout.GetLine(_state.Caret.Line);
        if (line is null) return;

        int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        string display = line.Projection.DisplayText;
        visCol = Math.Clamp(visCol, 0, display.Length);

        int x = line.TextX + MeasureVisualPrefix(line, visCol);

        using var p = new Pen(ForeColor, 1f);
        g.DrawLine(p, x, line.Bounds.Top + 2, x, line.Bounds.Bottom - 2);
    }

    private bool HandleShortcuts(KeyEventArgs e)
    {
        if (!e.Control) return false;

        switch (e.KeyCode)
        {
            case Keys.A:
                _state.SelectAll(_doc);
                NormalizeCaretOutOfTables();
                ResetCaretBlink();
                EnsureCaretVisible();
                Invalidate();
                return true;

            case Keys.C:
                CopySelection();
                return true;

            case Keys.X:
                if (_state.HasSelection)
                {
                    CopySelection();
                    ApplyDocumentEdit(() => _state.DeleteSelection(_doc));
                }
                return true;

            case Keys.V:
                if (Clipboard.ContainsText())
                {
                    string t = Clipboard.GetText(TextDataFormat.Text);
                    if (!string.IsNullOrEmpty(t))
                        ApplyDocumentEdit(() => _state.InsertText(_doc, t));
                }
                return true;

            case Keys.Z:
                Undo();
                return true;

            case Keys.Y:
                Redo();
                return true;
        }

        return false;
    }

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
            LayoutLine? line = _layout.GetLine(lineIndex);
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
        LayoutLine? line = _layout.GetLine(_state.Caret.Line);
        if (line is null) return false;

        int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        return visCol == 0;
    }

    private void CopySelection()
    {
        string sel = _state.GetSelectedText(_doc);
        if (!string.IsNullOrEmpty(sel))
            Clipboard.SetText(sel);
    }

    private void ApplyDocumentEdit(Func<bool> editOperation)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        EditorSnapshot before = CaptureSnapshot();
        bool changed = editOperation();
        if (!changed) return;

        PushUndo(before);
        _redo.Clear();

        _doc.ReparseDirtyBlocks();
        EnsureTrailingEditableLineAfterTerminalTable();

        CleanupRawTableModes();
        ExitRawModesIfCaretOutside();

        RebuildLayout();
        NormalizeCaretOutOfTables();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown()));
    }

    private void Undo()
    {
        EndCellEdit(discard: false, move: CellMove.None);
        if (_undo.Count == 0) return;

        EditorSnapshot current = CaptureSnapshot();
        EditorSnapshot prev = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);

        _redo.Add(current);
        RestoreSnapshot(prev);
    }

    private void Redo()
    {
        EndCellEdit(discard: false, move: CellMove.None);
        if (_redo.Count == 0) return;

        EditorSnapshot current = CaptureSnapshot();
        EditorSnapshot next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);

        _undo.Add(current);
        RestoreSnapshot(next);
    }

    private void RestoreSnapshot(EditorSnapshot s)
    {
        _doc.LoadMarkdown(s.Markdown);
        EnsureTrailingEditableLineAfterTerminalTable();

        _rawTableStartLines.Clear();
        _rawCodeFenceStartLine = null;

        _state.Restore(s.Caret, s.Anchor, _doc);

        RebuildLayout();
        NormalizeCaretOutOfTables();

        ResetCaretBlink();
        EnsureCaretVisible();
        Invalidate();

        MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown()));
    }

    private EditorSnapshot CaptureSnapshot()
        => new(_doc.ToMarkdown(), _state.Caret, _state.Anchor);

    private void PushUndo(EditorSnapshot s)
    {
        _undo.Add(s);
        if (_undo.Count > MaxUndo)
            _undo.RemoveAt(0);
    }

    private void RebuildLayout()
    {
        SyncCodeFenceRawModeWithCaret();
        ExitRawModesIfCaretOutside();

        _layout.Rebuild(
            _doc,
            ClientSize,
            Font,
            _boldFont,
            _monoFont,
            _rawTableStartLines,
            GetRawCodeFenceStarts(),
            GetRawSourceLinesForCaretBlock(),
            GetRawInlineLinesForCaret());

        AutoScrollMinSize = _layout.ContentSize;
    }


    private void NormalizeCaretOutOfTables()
    {
        if (!_layout.IsTableSourceLine(_state.Caret.Line))
            return;

        if (TryGetContainingTableByLine(_state.Caret.Line, out var t))
        {
            if (!_rawTableStartLines.Contains(t.StartLine))
            {
                _rawTableStartLines.Add(t.StartLine);
                RebuildLayout();
                return;
            }
        }

        int nearest = _layout.GetNearestTextLine(_state.Caret.Line, preferForward: true);
        if (nearest < 0)
        {
            _state.Restore(new MarkdownPosition(0, 0), null, _doc);
            return;
        }

        int col = Math.Min(_state.Caret.Column, _doc.GetLineLength(nearest));
        _state.Restore(new MarkdownPosition(nearest, col), null, _doc);
    }

    private void EnterRawTableSourceFromGrid(TableLayout tableLayout, int gridRow, int gridCol)
    {
        EndCellEdit(discard: false, move: CellMove.None);

        TableBlock table = tableLayout.Block;
        _rawTableStartLines.Add(table.StartLine);
        RebuildLayout();

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
        RebuildLayout();

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

        LayoutLine? line = _layout.GetLine(_state.Caret.Line);
        if (line is null) return;

        int srcCol = Math.Clamp(_state.Caret.Column, 0, line.SourceText.Length);
        int visCol = line.Projection.SourceToVisual[srcCol];
        string display = line.Projection.DisplayText;
        visCol = Math.Clamp(visCol, 0, display.Length);

        int caretX = line.TextX + MeasureVisualPrefix(line, visCol);
        int caretY = line.Bounds.Top;

        int viewLeft = -AutoScrollPosition.X;
        int viewTop = -AutoScrollPosition.Y;
        int viewRight = viewLeft + ClientSize.Width;
        int viewBottom = viewTop + ClientSize.Height;

        int targetX = viewLeft;
        int targetY = viewTop;
        bool change = false;

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

        if (caretY < viewTop)
        {
            targetY = Math.Max(0, caretY - 20);
            change = true;
        }
        else if (caretY + line.Bounds.Height > viewBottom)
        {
            targetY = Math.Max(0, caretY + line.Bounds.Height - ClientSize.Height + 20);
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

    private static Font GetOrCreateInlineFont(Dictionary<int, Font> cache, Font baseFont, InlineStyle style, bool isCode, Font monoFont)
    {
        InlineStyle normalized = style & ~InlineStyle.Code;

        int key = ((int)normalized & 0xFF)
                  | (isCode ? 0x100 : 0)
                  | (((int)baseFont.Style & 0xFF) << 9);

        if (cache.TryGetValue(key, out var f))
            return f;

        Font seed = isCode ? monoFont : baseFont;
        f = InlineMarkdown.CreateStyledFont(seed, normalized);
        cache[key] = f;
        return f;
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

        int x = contentTextStart.X;
        var cache = new Dictionary<int, Font>();

        using var codeBrush = new SolidBrush(_inlineCodeBg);
        using var codePen = new Pen(_inlineCodeBorder);

        try
        {
            foreach (var run in line.InlineRuns)
            {
                if (string.IsNullOrEmpty(run.Text)) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateInlineFont(cache, baseFont, run.Style, isCode, _monoFont);

                if (!isCode)
                {
                    DrawTextGdiPlus(g, run.Text, runFont, new Point(x, contentTextStart.Y), ForeColor);
                    x += MeasureWidth(run.Text, runFont);
                    continue;
                }

                int textW = MeasureWidth(run.Text, runFont);
                int textH = MeasureHeight(runFont);

                int chipW = textW + InlineCodePadX * 2;
                int chipH = Math.Max(1, textH + InlineCodePadY * 2);
                int chipY = contentTextStart.Y + Math.Max(0, (line.Bounds.Height - chipH) / 2);

                var chip = new Rectangle(x, chipY, chipW, chipH);

                g.FillRectangle(codeBrush, chip);
                g.DrawRectangle(codePen, chip.X, chip.Y, Math.Max(1, chip.Width - 1), Math.Max(1, chip.Height - 1));

                int textY = chip.Y + Math.Max(0, (chip.Height - textH) / 2);
                DrawTextGdiPlus(g, run.Text, runFont, new Point(x + InlineCodePadX, textY), ForeColor);

                x += chipW;
            }
        }
        finally
        {
            foreach (var f in cache.Values)
                f.Dispose();
        }
    }

    private int MeasureVisualPrefix(LayoutLine line, int visualCols)
    {
        string display = line.Projection.DisplayText;
        visualCols = Math.Clamp(visualCols, 0, display.Length);
        if (visualCols <= 0) return 0;

        Font baseFont = GetRenderFont(line);

        if (line.InlineRuns is null || line.InlineRuns.Count == 0)
        {
            return MeasureWidth(display[..visualCols], baseFont);
        }

        int remaining = visualCols;
        int width = 0;
        var cache = new Dictionary<int, Font>();

        try
        {
            foreach (var run in line.InlineRuns)
            {
                if (remaining <= 0) break;
                if (string.IsNullOrEmpty(run.Text)) continue;

                int runLen = run.Text.Length;
                int take = Math.Min(remaining, runLen);
                if (take <= 0) continue;

                bool isCode = (run.Style & InlineStyle.Code) != 0;
                Font runFont = GetOrCreateInlineFont(cache, baseFont, run.Style, isCode, _monoFont);

                if (isCode)
                    width += InlineCodePadX;

                string part = take == runLen ? run.Text : run.Text[..take];
                width += MeasureWidth(part, runFont);

                if (isCode && take == runLen)
                    width += InlineCodePadX;

                remaining -= take;
            }
        }
        finally
        {
            foreach (var f in cache.Values)
                f.Dispose();
        }

        return width;
    }

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
            BackColor = _tableCellBg,
            ForeColor = ForeColor
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
        LayoutLine? line = _layout.GetLine(sourceLine);
        if (line is null) return 0;

        int[] v2s = line.Projection.VisualToSource;
        if (v2s.Length == 0) return 0;

        return Math.Clamp(v2s[0], 0, line.SourceText.Length);
    }

    private int GetVisualLineEndSourceColumn(int sourceLine)
    {
        LayoutLine? line = _layout.GetLine(sourceLine);
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

            EditorSnapshot before = CaptureSnapshot();

            IReadOnlyList<string> tableLines = _activeTable.Model.ToMarkdownLines();
            _doc.ReplaceLines(_activeTable.StartLine, _activeTable.EndLine, tableLines);
            _doc.ReparseAll();
            EnsureTrailingEditableLineAfterTerminalTable();

            CleanupRawTableModes();
            ExitRawModesIfCaretOutside();

            PushUndo(before);
            _redo.Clear();

            RebuildLayout();
            NormalizeCaretOutOfTables();
            Invalidate();

            MarkdownChanged?.Invoke(this, new MarkdownChangedEventArgs(_doc.ToMarkdown()));

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

    private readonly record struct EditorSnapshot(string Markdown, MarkdownPosition Caret, MarkdownPosition? Anchor);
}
