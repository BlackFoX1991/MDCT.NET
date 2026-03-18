using System.Drawing.Printing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MarkdownGdi;

namespace MarkdownPad;

public partial class frmMain : Form
{
    private const string AppTitle = "MDCT.NET © by A. Löwen";
    private enum InlineColorTarget
    {
        Foreground,
        Background
    }

    private const int MaxRecentFiles = 12;
    private const int SwRestore = 9;
    private const float DefaultViewScale = 1f;
    private const float MinViewScale = 0.75f;
    private const float MaxViewScale = 2.5f;
    private const float ViewScaleStep = 0.1f;

    private readonly List<string> _recentFiles = [];
    private readonly ToolStripMenuItem _recentToolStripMenuItem = new() { Name = "recentToolStripMenuItem", Text = "Recent..." };
    private readonly TrackBar _viewScaleTrackBar = new();
    private readonly ToolStripLabel _viewScaleToolStripLabel = new() { Name = "viewScaleToolStripLabel", Text = "Scale" };
    private readonly ToolStripLabel _viewScaleValueToolStripLabel = new() { Name = "viewScaleValueToolStripLabel", AutoSize = false, Width = 46 };
    private readonly ContextMenuStrip _editorContextMenuStrip = new() { Name = "editorContextMenuStrip" };
    private readonly ToolStripMenuItem _editorContextUndoMenuItem = new() { Name = "editorContextUndoMenuItem", Text = "Undo" };
    private readonly ToolStripMenuItem _editorContextRedoMenuItem = new() { Name = "editorContextRedoMenuItem", Text = "Redo" };
    private readonly ToolStripMenuItem _editorContextCutMenuItem = new() { Name = "editorContextCutMenuItem", Text = "Cut" };
    private readonly ToolStripMenuItem _editorContextCopyMenuItem = new() { Name = "editorContextCopyMenuItem", Text = "Copy" };
    private readonly ToolStripMenuItem _editorContextPasteMenuItem = new() { Name = "editorContextPasteMenuItem", Text = "Paste" };
    private readonly ToolStripMenuItem _editorContextSelectAllMenuItem = new() { Name = "editorContextSelectAllMenuItem", Text = "Select All" };
    private readonly ToolStripMenuItem _editorContextInsertLinkMenuItem = new() { Name = "editorContextInsertLinkMenuItem", Text = "Insert Link..." };
    private readonly ToolStripMenuItem _editorContextInsertImageMenuItem = new() { Name = "editorContextInsertImageMenuItem", Text = "Insert Image..." };
    private readonly ToolStripMenuItem _editorContextInsertTableMenuItem = new() { Name = "editorContextInsertTableMenuItem", Text = "Insert Table..." };
    private readonly ToolStripMenuItem _editorContextHeadingMenuItem = new() { Name = "editorContextHeadingMenuItem", Text = "Heading" };
    private readonly ToolStripMenuItem _editorContextHeading1MenuItem = new() { Name = "editorContextHeading1MenuItem", Text = "H1" };
    private readonly ToolStripMenuItem _editorContextHeading2MenuItem = new() { Name = "editorContextHeading2MenuItem", Text = "H2" };
    private readonly ToolStripMenuItem _editorContextHeading3MenuItem = new() { Name = "editorContextHeading3MenuItem", Text = "H3" };
    private readonly ToolStripMenuItem _editorContextHeading4MenuItem = new() { Name = "editorContextHeading4MenuItem", Text = "H4" };
    private readonly ToolStripMenuItem _editorContextHeading5MenuItem = new() { Name = "editorContextHeading5MenuItem", Text = "H5" };
    private readonly ToolStripMenuItem _editorContextHeading6MenuItem = new() { Name = "editorContextHeading6MenuItem", Text = "H6" };
    private readonly ToolStripMenuItem _editorContextQuoteMenuItem = new() { Name = "editorContextQuoteMenuItem", Text = "Quote" };
    private readonly ToolStripMenuItem _editorContextCodeFenceMenuItem = new() { Name = "editorContextCodeFenceMenuItem", Text = "Code Fence" };
    private ToolStripControlHost? _viewScaleTrackBarHost;
    private ToolStripSeparator? _viewScaleToolStripSeparator;

    private EditorThemeMode _themeMode = EditorThemeMode.System;
    private string? _lastDirectory;
    private string _statusMessage = "Ready";
    private int _untitledCounter = 1;
    private int _printContentTop;
    private int _printRendererWidth;
    private int _printRendererHeight;
    private int _contextTabIndex = -1;
    private bool _suppressViewScaleTrackBarChange;
    private padTab? _printSourceTab;
    private MarkdownGdiEditor? _printRenderer;
    private Font? _printRendererFont;
    private Color _lastForegroundColor = Color.FromArgb(50, 168, 82);
    private Color _lastBackgroundColor = Color.FromArgb(94, 90, 90);
    private Color _lastFrameBorderColor = Color.FromArgb(47, 93, 255);
    private Color _lastFrameFillColor = Color.FromArgb(238, 243, 255);
    private Color _lastProgressBorderColor = Color.FromArgb(80, 120, 200);
    private Color _lastProgressBarColor = Color.FromArgb(123, 201, 111);
    private int _lastProgressPercent = 50;

    public frmMain()
    {
        InitializeComponent();

        Text = AppTitle;
        KeyPreview = true;

        ConfigureDynamicUi();
        ConfigureMenus();
        ConfigureToolbar();
        ConfigureEditorContextMenu();
        ConfigurePrinting();
        ConfigureTabControl();
        ConfigureDragAndDrop();

        RestoreApplicationState();
        UpdateUiState();
    }

    private padTab? ActiveTab => tabControl1.SelectedTab as padTab;

    private MarkdownGdiEditor? ActiveEditor => ActiveTab?.Editor;

    private IEnumerable<padTab> OpenTabs => tabControl1.TabPages.OfType<padTab>();

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (TryHandleViewScaleShortcut(keyData))
            return true;

        switch (keyData)
        {
            case Keys.Control | Keys.Tab:
                SelectAdjacentTab(+1);
                return true;
            case Keys.Control | Keys.Shift | Keys.Tab:
                SelectAdjacentTab(-1);
                return true;
            case Keys.Control | Keys.W:
            case Keys.Control | Keys.F4:
                CloseActiveTab();
                return true;
            case Keys.F3:
                FindNextInActiveDocument();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void ConfigureMenus()
    {
        newToolStripMenuItem.Click += (_, _) => CreateNewTab();
        openToolStripMenuItem.Click += (_, _) => OpenDocuments();
        _recentToolStripMenuItem.DropDownOpening += (_, _) => PopulateRecentMenu();
        saveToolStripMenuItem.Click += (_, _) => SaveActiveDocument();
        saveAsToolStripMenuItem.Click += (_, _) => SaveActiveDocument(forceSaveAs: true);
        saveAllToolStripMenuItem.Click += (_, _) => SaveAllDocuments();
        closeTabToolStripMenuItem.Click += (_, _) => CloseActiveTab();
        closeAllTabsToolStripMenuItem.Click += (_, _) => CloseAllTabs();
        closeOtherTabsToolStripMenuItem.Click += (_, _) => CloseOtherTabs();
        printToolStripMenuItem.Click += (_, _) => PrintActiveDocument();
        printPreviewToolStripMenuItem.Click += (_, _) => PreviewActiveDocument();
        exitToolStripMenuItem.Click += (_, _) => Close();

        undoToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.UndoCommand());
        redoToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.RedoCommand());
        cutToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CutCommand());
        copyToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CopyCommand());
        pasteToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.PasteCommand());
        selectAllToolStripMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.SelectAllCommand());

        tableDesignerToolStripMenuItem.Click += (_, _) => ShowTableDesigner();
        insertLinkToolStripMenuItem.Click += (_, _) => ShowInsertLinkDialog();
        insertImageToolStripMenuItem.Click += (_, _) => ShowInsertImageDialog();
        insertFrameToolStripMenuItem.Click += (_, _) => ShowInsertFrameDialog();
        insertProgressToolStripMenuItem.Click += (_, _) => ShowInsertProgressDialog();
        heading1ToolStripMenuItem.Click += (_, _) => ApplyHeading(1);
        heading2ToolStripMenuItem.Click += (_, _) => ApplyHeading(2);
        heading3ToolStripMenuItem.Click += (_, _) => ApplyHeading(3);
        heading4ToolStripMenuItem.Click += (_, _) => ApplyHeading(4);
        heading5ToolStripMenuItem.Click += (_, _) => ApplyHeading(5);
        heading6ToolStripMenuItem.Click += (_, _) => ApplyHeading(6);
        quoteToolStripMenuItem.Click += (_, _) => ToggleQuoteBlock();
        codeFenceToolStripMenuItem.Click += (_, _) => WrapSelectionInCodeFence();
        _foregroundColorToolStripMenuItem.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Foreground);
        _backgroundColorToolStripMenuItem.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Background);

        findToolStripMenuItem.Click += (_, _) => ShowFindDialog();
        findNextToolStripMenuItem.Click += (_, _) => FindNextInActiveDocument();

        UpdateThemeMenuChecks();
    }

    private void ConfigureToolbar()
    {
        newToolStripButton.Click += (_, _) => CreateNewTab();
        openToolStripButton.Click += (_, _) => OpenDocuments();
        saveToolStripButton.Click += (_, _) => SaveActiveDocument();
        saveAllToolStripButton.Click += (_, _) => SaveAllDocuments();
        undoToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.UndoCommand());
        redoToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.RedoCommand());
        cutToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CutCommand());
        copyToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CopyCommand());
        pasteToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.PasteCommand());
        selectAllToolStripButton.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.SelectAllCommand());
        findToolStripButton.Click += (_, _) => ShowFindDialog();
        findNextToolStripButton.Click += (_, _) => FindNextInActiveDocument();
        linkToolStripButton.Click += (_, _) => ShowInsertLinkDialog();
        imageToolStripButton.Click += (_, _) => ShowInsertImageDialog();
        frameToolStripButton.Click += (_, _) => ShowInsertFrameDialog();
        progressToolStripButton.Click += (_, _) => ShowInsertProgressDialog();
        tableToolStripButton.Click += (_, _) => ShowTableDesigner();
        heading1ToolStripDropDownItem.Click += (_, _) => ApplyHeading(1);
        heading2ToolStripDropDownItem.Click += (_, _) => ApplyHeading(2);
        heading3ToolStripDropDownItem.Click += (_, _) => ApplyHeading(3);
        heading4ToolStripDropDownItem.Click += (_, _) => ApplyHeading(4);
        heading5ToolStripDropDownItem.Click += (_, _) => ApplyHeading(5);
        heading6ToolStripDropDownItem.Click += (_, _) => ApplyHeading(6);
        quoteToolStripButton.Click += (_, _) => ToggleQuoteBlock();
        codeFenceToolStripButton.Click += (_, _) => WrapSelectionInCodeFence();
        _foregroundColorToolStripButton.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Foreground);
        _backgroundColorToolStripButton.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Background);

        themeSystemToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.System);
        themeLightToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.Light);
        themeDarkToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.Dark);

        ConfigureViewScaleToolbar();
    }

    private void ConfigureEditorContextMenu()
    {
        _editorContextMenuStrip.ImageScalingSize = new Size(20, 20);
        _editorContextUndoMenuItem.Image = undoToolStripButton.Image;
        _editorContextRedoMenuItem.Image = redoToolStripButton.Image;
        _editorContextCutMenuItem.Image = cutToolStripButton.Image;
        _editorContextCopyMenuItem.Image = copyToolStripButton.Image;
        _editorContextPasteMenuItem.Image = pasteToolStripButton.Image;
        _editorContextSelectAllMenuItem.Image = selectAllToolStripButton.Image;
        _editorContextInsertLinkMenuItem.Image = linkToolStripButton.Image;
        _editorContextInsertImageMenuItem.Image = imageToolStripButton.Image;
        _editorContextInsertTableMenuItem.Image = tableToolStripButton.Image;
        _editorContextHeadingMenuItem.Image = headingToolStripDropDownButton.Image;
        _editorContextQuoteMenuItem.Image = quoteToolStripButton.Image;
        _editorContextCodeFenceMenuItem.Image = codeFenceToolStripButton.Image;
        _editorContextInsertFrameMenuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _editorContextInsertProgressMenuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _editorContextForegroundColorMenuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _editorContextBackgroundColorMenuItem.DisplayStyle = ToolStripItemDisplayStyle.Text;

        _editorContextHeadingMenuItem.DropDownItems.AddRange(
        [
            _editorContextHeading1MenuItem,
            _editorContextHeading2MenuItem,
            _editorContextHeading3MenuItem,
            _editorContextHeading4MenuItem,
            _editorContextHeading5MenuItem,
            _editorContextHeading6MenuItem
        ]);

        _editorContextMenuStrip.Items.AddRange(
        [
            _editorContextUndoMenuItem,
            _editorContextRedoMenuItem,
            new ToolStripSeparator(),
            _editorContextCutMenuItem,
            _editorContextCopyMenuItem,
            _editorContextPasteMenuItem,
            _editorContextSelectAllMenuItem,
            new ToolStripSeparator(),
            _editorContextInsertLinkMenuItem,
            _editorContextInsertImageMenuItem,
            _editorContextInsertFrameMenuItem,
            _editorContextInsertProgressMenuItem,
            _editorContextForegroundColorMenuItem,
            _editorContextBackgroundColorMenuItem,
            _editorContextInsertTableMenuItem,
            new ToolStripSeparator(),
            _editorContextHeadingMenuItem,
            _editorContextQuoteMenuItem,
            _editorContextCodeFenceMenuItem
        ]);

        _editorContextUndoMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.UndoCommand());
        _editorContextRedoMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.RedoCommand());
        _editorContextCutMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CutCommand());
        _editorContextCopyMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.CopyCommand());
        _editorContextPasteMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.PasteCommand());
        _editorContextSelectAllMenuItem.Click += (_, _) => ExecuteOnActiveEditor(editor => editor.SelectAllCommand());
        _editorContextInsertLinkMenuItem.Click += (_, _) => ShowInsertLinkDialog();
        _editorContextInsertImageMenuItem.Click += (_, _) => ShowInsertImageDialog();
        _editorContextInsertFrameMenuItem.Click += (_, _) => ShowInsertFrameDialog();
        _editorContextInsertProgressMenuItem.Click += (_, _) => ShowInsertProgressDialog();
        _editorContextInsertTableMenuItem.Click += (_, _) => ShowTableDesigner();
        _editorContextHeading1MenuItem.Click += (_, _) => ApplyHeading(1);
        _editorContextHeading2MenuItem.Click += (_, _) => ApplyHeading(2);
        _editorContextHeading3MenuItem.Click += (_, _) => ApplyHeading(3);
        _editorContextHeading4MenuItem.Click += (_, _) => ApplyHeading(4);
        _editorContextHeading5MenuItem.Click += (_, _) => ApplyHeading(5);
        _editorContextHeading6MenuItem.Click += (_, _) => ApplyHeading(6);
        _editorContextQuoteMenuItem.Click += (_, _) => ToggleQuoteBlock();
        _editorContextCodeFenceMenuItem.Click += (_, _) => WrapSelectionInCodeFence();
        _editorContextForegroundColorMenuItem.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Foreground);
        _editorContextBackgroundColorMenuItem.Click += (_, _) => ApplyInlineColor(InlineColorTarget.Background);
        _editorContextMenuStrip.Opening += EditorContextMenuStrip_Opening;
    }

    private void ConfigurePrinting()
    {
        printDialog.Document = printDocument;
        printPreviewDialog.Document = printDocument;

        printDocument.BeginPrint += (_, _) =>
        {
            _printContentTop = 0;
            DisposePrintRenderer();
        };
        printDocument.EndPrint += (_, _) =>
        {
            _printContentTop = 0;
            _printSourceTab = null;
            DisposePrintRenderer();
        };
        printDocument.PrintPage += PrintDocument_PrintPage;
    }

    private void ConfigureTabControl()
    {
        tabControl1.SelectedIndexChanged += (_, _) => UpdateUiState();
        tabControl1.MouseUp += TabControl_MouseUp;

        closeContextTabToolStripMenuItem.Click += (_, _) => CloseContextTab();
        closeOtherContextTabsToolStripMenuItem.Click += (_, _) => CloseOtherTabs();
        closeAllContextTabsToolStripMenuItem.Click += (_, _) => CloseAllTabs();
    }

    private void ConfigureDragAndDrop()
    {
        AllowDrop = true;
        tabControl1.AllowDrop = true;

        DragEnter += HandleDragEnter;
        DragDrop += HandleDragDrop;
        tabControl1.DragEnter += HandleDragEnter;
        tabControl1.DragDrop += HandleDragDrop;

        FormClosing += frmMain_FormClosing;
    }

    private void RestoreApplicationState()
    {
        MarkdownPadApplicationState state = ApplicationStateStore.Load();

        _themeMode = state.ThemeMode;
        _lastDirectory = string.IsNullOrWhiteSpace(state.LastDirectory) ? null : state.LastDirectory;
        _untitledCounter = Math.Max(1, state.NextUntitledCounter);

        _recentFiles.Clear();
        foreach (string file in NormalizeRecentFiles(state.RecentFiles).Take(MaxRecentFiles))
            _recentFiles.Add(file);

        ApplyWindowPlacement(state.Window);

        if (state.OpenDocuments.Count == 0)
        {
            CreateNewTab();
            UpdateThemeMenuChecks();
            return;
        }

        foreach (MarkdownPadSessionDocument document in state.OpenDocuments)
        {
            string? restoredName = string.IsNullOrWhiteSpace(document.FilePath)
                ? document.DefaultName
                : Path.GetFileName(document.FilePath);

            padTab tab = CreateNewTab(
                select: false,
                defaultName: restoredName,
                viewScale: NormalizeViewScale(document.ViewScale));
            tab.RestoreDocument(document.Markdown ?? string.Empty, document.FilePath, document.Modified);
        }

        if (tabControl1.TabPages.Count == 0)
        {
            CreateNewTab();
        }
        else
        {
            int selectedIndex = Math.Clamp(state.SelectedTabIndex, 0, tabControl1.TabPages.Count - 1);
            tabControl1.SelectedIndex = selectedIndex;

            if (ActiveTab is not null)
                FocusEditor(ActiveTab);
        }

        UpdateThemeMenuChecks();
        SetStatusMessage("Session restored");
    }

    private void ApplyWindowPlacement(WindowPlacementState placement)
    {
        Rectangle bounds = new(placement.Left, placement.Top, placement.Width, placement.Height);
        if (bounds.Width < 600 || bounds.Height < 360 || !IsUsableWindowBounds(bounds))
            return;

        StartPosition = FormStartPosition.Manual;
        Bounds = bounds;
        WindowState = placement.WindowState == FormWindowState.Maximized
            ? FormWindowState.Maximized
            : FormWindowState.Normal;
    }

    private static bool IsUsableWindowBounds(Rectangle bounds)
    {
        return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds));
    }

    private MarkdownPadApplicationState CaptureApplicationState()
    {
        return new MarkdownPadApplicationState
        {
            Window = CaptureWindowPlacement(),
            ThemeMode = _themeMode,
            LastDirectory = _lastDirectory,
            SelectedTabIndex = Math.Max(0, tabControl1.SelectedIndex),
            NextUntitledCounter = Math.Max(1, _untitledCounter),
            RecentFiles = [.. _recentFiles],
            OpenDocuments = [.. OpenTabs.Select(CaptureSessionDocument)]
        };
    }

    private WindowPlacementState CaptureWindowPlacement()
    {
        Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        FormWindowState state = WindowState == FormWindowState.Maximized
            ? FormWindowState.Maximized
            : FormWindowState.Normal;

        return new WindowPlacementState
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = Math.Max(640, bounds.Width),
            Height = Math.Max(400, bounds.Height),
            WindowState = state
        };
    }

    private static MarkdownPadSessionDocument CaptureSessionDocument(padTab tab)
    {
        return new MarkdownPadSessionDocument
        {
            FilePath = tab.FilePath,
            DefaultName = tab.IsUntitled ? tab.DocumentName : null,
            Markdown = tab.Editor.Markdown,
            Modified = tab.Modified,
            ViewScale = tab.ViewScale
        };
    }

    private void PopulateRecentMenu()
    {
        _recentToolStripMenuItem.DropDownItems.Clear();

        if (_recentFiles.Count == 0)
        {
            _recentToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("(Empty)") { Enabled = false });
            return;
        }

        for (int i = 0; i < _recentFiles.Count; i++)
        {
            string path = _recentFiles[i];
            var item = new ToolStripMenuItem
            {
                Text = BuildRecentMenuText(path, i),
                ToolTipText = path,
                Tag = path
            };

            item.Click += RecentFileMenuItem_Click;
            _recentToolStripMenuItem.DropDownItems.Add(item);
        }

        _recentToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

        var clearItem = new ToolStripMenuItem("Clear Recent")
        {
            Enabled = _recentFiles.Count > 0
        };
        clearItem.Click += (_, _) =>
        {
            _recentFiles.Clear();
            SetStatusMessage("Recent files cleared");
            UpdateUiState();
        };

        _recentToolStripMenuItem.DropDownItems.Add(clearItem);
    }

    private static string BuildRecentMenuText(string path, int index)
    {
        string fileName = EscapeMenuText(Path.GetFileName(path));
        string directory = EscapeMenuText(Path.GetDirectoryName(path) ?? string.Empty);
        string prefix = index < 9 ? $"&{index + 1} " : string.Empty;
        return $"{prefix}{fileName}  ({directory})";
    }

    private static string EscapeMenuText(string text) => text.Replace("&", "&&", StringComparison.Ordinal);

    private void RecentFileMenuItem_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: string path })
            return;

        if (!File.Exists(path))
        {
            _recentFiles.RemoveAll(candidate => string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase));
            MessageBox.Show(
                this,
                $"The recent file could not be found:\n{path}",
                "Recent Files",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            UpdateUiState();
            return;
        }

        OpenDocumentsFromPaths([path]);
    }

    private void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        string fullPath = Path.GetFullPath(filePath);
        _recentFiles.RemoveAll(candidate => string.Equals(candidate, fullPath, StringComparison.OrdinalIgnoreCase));
        _recentFiles.Insert(0, fullPath);

        if (_recentFiles.Count > MaxRecentFiles)
            _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);
    }

    private static IEnumerable<string> NormalizeRecentFiles(IEnumerable<string>? files)
    {
        if (files is null)
            yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            if (string.IsNullOrWhiteSpace(file))
                continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(file);
            }
            catch
            {
                continue;
            }

            if (seen.Add(fullPath))
                yield return fullPath;
        }
    }

    private padTab CreateNewTab(bool select = true, string? defaultName = null, float viewScale = DefaultViewScale)
    {
        string tabName = string.IsNullOrWhiteSpace(defaultName)
            ? $"Untitled {_untitledCounter++}"
            : defaultName;

        var tab = new padTab(tabName, _themeMode, NormalizeViewScale(viewScale));
        tab.DocumentStateChanged += Tab_DocumentStateChanged;
        tab.Editor.ContextMenuStrip = _editorContextMenuStrip;

        tab.Editor.MarkdownChanged += Editor_MarkdownChanged;
        tab.Editor.FindRequested += Editor_FindRequested;
        tab.Editor.LinkActivated += Editor_LinkActivated;
        tab.Editor.ViewScaleRequested += Editor_ViewScaleRequested;
        tab.Editor.Enter += Editor_StateAffectingEvent;
        tab.Editor.GotFocus += Editor_StateAffectingEvent;
        tab.Editor.KeyUp += Editor_StateAffectingEvent;
        tab.Editor.MouseUp += Editor_StateAffectingEvent;
        tab.Editor.MouseMove += Editor_MouseMove;

        tabControl1.TabPages.Add(tab);

        if (select)
        {
            tabControl1.SelectedTab = tab;
            FocusEditor(tab);
            SetStatusMessage("New document");
        }

        return tab;
    }

    private void OpenDocuments()
    {
        if (!string.IsNullOrWhiteSpace(_lastDirectory) && Directory.Exists(_lastDirectory))
            openFileDialog.InitialDirectory = _lastDirectory;

        if (openFileDialog.ShowDialog(this) != DialogResult.OK)
            return;

        OpenDocumentsFromPaths(openFileDialog.FileNames);
    }

    private padTab? OpenDocumentsFromPaths(IEnumerable<string> filePaths)
    {
        padTab? selectedTab = null;
        padTab? reusableTab = GetReusableBlankTab();
        bool reusedBlankTab = false;

        foreach (string candidate in filePaths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string fullPath = Path.GetFullPath(candidate);
            _lastDirectory = Path.GetDirectoryName(fullPath);

            padTab? existing = FindTabByPath(fullPath);
            if (existing is not null)
            {
                selectedTab = existing;
                AddRecentFile(fullPath);
                continue;
            }

            try
            {
                string markdown = File.ReadAllText(fullPath);
                padTab targetTab;

                if (!reusedBlankTab && reusableTab is not null)
                {
                    targetTab = reusableTab;
                    reusedBlankTab = true;
                }
                else
                {
                    targetTab = CreateNewTab(select: false);
                }

                targetTab.LoadDocument(markdown, fullPath);
                AddRecentFile(fullPath);
                selectedTab = targetTab;
                SetStatusMessage($"Opened: {targetTab.DocumentName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"The file could not be opened:\n{fullPath}\n\n{ex.Message}",
                    "Open",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        if (selectedTab is not null)
        {
            tabControl1.SelectedTab = selectedTab;
            FocusEditor(selectedTab);
        }

        UpdateUiState();
        return selectedTab;
    }

    internal void OpenExternalDocuments(IEnumerable<string> filePaths)
    {
        if (IsDisposed)
            return;

        IReadOnlyList<string> requestedFiles = [.. filePaths.Where(path => !string.IsNullOrWhiteSpace(path))];
        if (requestedFiles.Count > 0)
            OpenDocumentsFromPaths(requestedFiles);

        RevealAndActivateWindow();
    }

    private bool SaveActiveDocument(bool forceSaveAs = false)
        => ActiveTab is not null && SaveDocument(ActiveTab, forceSaveAs);

    private bool SaveAllDocuments()
    {
        foreach (padTab tab in OpenTabs.Where(NeedsSaving).ToList())
        {
            if (!SaveDocument(tab, forceSaveAs: false))
                return false;
        }

        SetStatusMessage("All changes saved");
        UpdateUiState();
        return true;
    }

    private bool SaveDocument(padTab tab, bool forceSaveAs)
    {
        string? targetPath = tab.FilePath;

        if (forceSaveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            saveFileDialog.FileName = BuildSuggestedFileName(tab);
            saveFileDialog.InitialDirectory = ResolveInitialDirectory(tab);

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                return false;

            targetPath = Path.GetFullPath(saveFileDialog.FileName);
        }

        padTab? otherTab = FindTabByPath(targetPath);
        if (otherTab is not null && !ReferenceEquals(otherTab, tab))
        {
            MessageBox.Show(
                this,
                $"The file is already open in another tab:\n{targetPath}",
                "Save",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            tabControl1.SelectedTab = otherTab;
            FocusEditor(otherTab);
            return false;
        }

        try
        {
            File.WriteAllText(targetPath, tab.Editor.Markdown);
            tab.MarkSaved(targetPath);
            _lastDirectory = Path.GetDirectoryName(targetPath);
            AddRecentFile(targetPath);
            SetStatusMessage($"Saved: {tab.DocumentName}");
            UpdateUiState();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The file could not be saved:\n{targetPath}\n\n{ex.Message}",
                "Save",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private void CloseActiveTab()
    {
        if (ActiveTab is not null)
            CloseTab(ActiveTab, ensureReplacementTab: true);
    }

    private void CloseContextTab()
    {
        padTab? tab = GetContextTab();
        if (tab is not null)
            CloseTab(tab, ensureReplacementTab: true);
    }

    private void CloseAllTabs()
    {
        CloseTabs(OpenTabs.ToList(), ensureReplacementTab: true);
    }

    private void CloseOtherTabs()
    {
        padTab? keepTab = GetContextTab() ?? ActiveTab;
        if (keepTab is null)
            return;

        CloseTabs(OpenTabs.Where(tab => !ReferenceEquals(tab, keepTab)).ToList(), ensureReplacementTab: false);
        tabControl1.SelectedTab = keepTab;
        FocusEditor(keepTab);
    }

    private bool CloseTab(padTab tab, bool ensureReplacementTab)
    {
        if (!ConfirmCloseTab(tab))
            return false;

        tab.Editor.MarkdownChanged -= Editor_MarkdownChanged;
        tab.Editor.FindRequested -= Editor_FindRequested;
        tab.Editor.LinkActivated -= Editor_LinkActivated;
        tab.Editor.Enter -= Editor_StateAffectingEvent;
        tab.Editor.GotFocus -= Editor_StateAffectingEvent;
        tab.Editor.KeyUp -= Editor_StateAffectingEvent;
        tab.Editor.MouseUp -= Editor_StateAffectingEvent;
        tab.Editor.MouseMove -= Editor_MouseMove;
        tab.DocumentStateChanged -= Tab_DocumentStateChanged;

        tabControl1.TabPages.Remove(tab);
        tab.Dispose();

        if (ensureReplacementTab && tabControl1.TabPages.Count == 0)
            CreateNewTab();

        UpdateUiState();
        return true;
    }

    private bool CloseTabs(IReadOnlyList<padTab> tabs, bool ensureReplacementTab)
    {
        foreach (padTab tab in tabs)
        {
            if (!CloseTab(tab, ensureReplacementTab: false))
                return false;
        }

        if (ensureReplacementTab && tabControl1.TabPages.Count == 0)
            CreateNewTab();

        UpdateUiState();
        return true;
    }

    private bool ConfirmCloseTab(padTab tab)
    {
        if (!tab.Modified)
            return true;

        DialogResult result = MessageBox.Show(
            this,
            $"Save changes to \"{tab.DocumentName}\"?",
            AppTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => SaveDocument(tab, forceSaveAs: false),
            DialogResult.No => true,
            _ => false
        };
    }

    private void ShowFindDialog()
    {
        if (ActiveEditor is null)
            return;

        ShowFindDialog(null, new FindOptions());
    }

    private void ShowFindDialog(string? currentQuery, FindOptions currentOptions)
    {
        MarkdownGdiEditor? editor = ActiveEditor;
        if (editor is null)
            return;

        using var dialog = new FindDialog(currentQuery, currentOptions);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        RunFind(editor, dialog.SearchText, dialog.SelectedOptions);
    }

    private void ShowTableDesigner()
    {
        padTab? tab = ActiveTab;
        if (tab is null)
            return;

        MarkdownGdiEditor editor = tab.Editor;

        using var dialog = new TableDesignerDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        editor.InsertTableCommand(dialog.GeneratedMarkdown);
        FocusEditor(tab);
        SetStatusMessage("Table inserted");
        UpdateUiState();
    }

    private void ShowInsertLinkDialog()
    {
        ShowInsertMediaDialog(InsertMediaKind.Link);
    }

    private void ShowInsertImageDialog()
    {
        ShowInsertMediaDialog(InsertMediaKind.Image);
    }

    private void ShowInsertFrameDialog()
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = ActiveEditor;
        if (tab is null || editor is null)
            return;

        using var dialog = new InsertFrameDialog(
            initialText: editor.SelectedText,
            borderColor: _lastFrameBorderColor,
            fillColor: _lastFrameFillColor);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        editor.InsertMarkdownSnippetCommand(dialog.GeneratedMarkdown);
        _lastFrameBorderColor = dialog.BorderColor;
        _lastFrameFillColor = dialog.FillColor;

        FocusEditor(tab);
        SetStatusMessage("Frame inserted");
        UpdateUiState();
    }

    private void ShowInsertProgressDialog()
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = ActiveEditor;
        if (tab is null || editor is null)
            return;

        using var dialog = new InsertProgressDialog(
            initialText: editor.SelectedText,
            initialPercent: _lastProgressPercent,
            borderColor: _lastProgressBorderColor,
            barColor: _lastProgressBarColor);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        editor.InsertMarkdownSnippetCommand(dialog.GeneratedMarkdown);
        _lastProgressPercent = dialog.ProgressPercent;
        _lastProgressBorderColor = dialog.BorderColor;
        _lastProgressBarColor = dialog.BarColor;

        FocusEditor(tab);
        SetStatusMessage("Progress bar inserted");
        UpdateUiState();
    }

    private void ShowInsertMediaDialog(InsertMediaKind kind)
    {
        padTab? tab = ActiveTab;
        if (tab is null)
            return;

        MarkdownGdiEditor editor = tab.Editor;
        string initialTitle = BuildInitialDialogTitle(editor.SelectedText);

        using var dialog = new InsertMediaDialog(
            kind,
            documentBasePath: tab.FilePath is null ? null : Path.GetDirectoryName(tab.FilePath),
            initialTitle: initialTitle,
            documentMarkdown: kind == InsertMediaKind.Link ? editor.Markdown : null);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        editor.InsertMarkdownSnippetCommand(dialog.GeneratedMarkdown);
        FocusEditor(tab);
        SetStatusMessage(kind == InsertMediaKind.Link ? "Link inserted" : "Image inserted");
        UpdateUiState();
    }

    private void ApplyHeading(int level)
    {
        if (ExecuteOnActiveEditor(editor => editor.ApplyHeadingCommand(level)))
            SetStatusMessage($"Heading H{level} applied");
    }

    private void ToggleQuoteBlock()
    {
        if (ExecuteOnActiveEditor(editor => editor.ToggleQuoteBlockCommand()))
            SetStatusMessage("Quote formatting updated");
    }

    private void WrapSelectionInCodeFence()
    {
        if (ExecuteOnActiveEditor(editor => editor.WrapSelectionInCodeFenceCommand()))
            SetStatusMessage("Code fence inserted");
    }

    private void FindNextInActiveDocument()
    {
        MarkdownGdiEditor? editor = ActiveEditor;
        if (editor is null)
            return;

        if (!editor.CanFindNext)
        {
            ShowFindDialog();
            return;
        }

        bool found = editor.FindNext();
        SetStatusMessage(found ? "Next match found" : "No further matches");

        if (!found)
        {
            MessageBox.Show(this, "No further match was found.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        UpdateUiState();
    }

    private void PrintActiveDocument()
    {
        if (!PreparePrintJob())
            return;

        try
        {
            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                printDocument.Print();
                SetStatusMessage("Print job started");
            }
            else
            {
                _printSourceTab = null;
                DisposePrintRenderer();
            }
        }
        catch (Exception ex)
        {
            _printSourceTab = null;
            DisposePrintRenderer();
            MessageBox.Show(this, $"Printing failed.\n\n{ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PreviewActiveDocument()
    {
        if (!PreparePrintJob())
            return;

        try
        {
            printPreviewDialog.ShowDialog(this);
            SetStatusMessage("Print preview opened");
        }
        catch (Exception ex)
        {
            _printSourceTab = null;
            DisposePrintRenderer();
            MessageBox.Show(
                this,
                $"The print preview could not be opened.\n\n{ex.Message}",
                "Print Preview",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private bool PreparePrintJob()
    {
        padTab? tab = ActiveTab;
        if (tab is null)
            return false;

        _printSourceTab = tab;
        _printContentTop = 0;
        DisposePrintRenderer();
        printDocument.DocumentName = tab.DocumentName;
        return true;
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        Graphics graphics = e.Graphics ?? throw new InvalidOperationException("Print graphics are not available.");
        padTab? sourceTab = _printSourceTab;
        if (sourceTab is null)
        {
            e.HasMorePages = false;
            return;
        }

        MarkdownGdiEditor renderer = EnsurePrintRenderer(sourceTab, e.MarginBounds.Size);
        float bitmapScale = GetPrintBitmapScale(graphics, sourceTab.Editor.DeviceDpi);
        int bitmapWidth = Math.Max(1, (int)Math.Ceiling(_printRendererWidth * bitmapScale));
        int bitmapHeight = Math.Max(1, (int)Math.Ceiling(_printRendererHeight * bitmapScale));
        using var pageBitmap = new Bitmap(bitmapWidth, bitmapHeight);
        pageBitmap.SetResolution(Math.Max(1f, sourceTab.Editor.DeviceDpi), Math.Max(1f, sourceTab.Editor.DeviceDpi));

        using (Graphics bitmapGraphics = Graphics.FromImage(pageBitmap))
        {
            bitmapGraphics.Clear(renderer.BackColor);
            renderer.RenderDocumentPage(
                bitmapGraphics,
                new Rectangle(0, 0, _printRendererWidth, _printRendererHeight),
                _printContentTop,
                outputScale: bitmapScale,
                textRenderingHint: System.Drawing.Text.TextRenderingHint.AntiAliasGridFit);
        }

        System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
        try
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(pageBitmap, e.MarginBounds);
        }
        finally
        {
            graphics.Restore(state);
        }

        _printContentTop += Math.Max(1, _printRendererHeight);
        e.HasMorePages = _printContentTop < renderer.DocumentRenderHeight;

        if (!e.HasMorePages)
        {
            _printContentTop = 0;
            _printSourceTab = null;
            DisposePrintRenderer();
        }
    }

    private MarkdownGdiEditor EnsurePrintRenderer(padTab sourceTab, Size pageSize)
    {
        MarkdownGdiEditor sourceEditor = sourceTab.Editor;
        float printScale = Math.Max(0.1f, sourceTab.ViewScale);
        int width = Math.Max(1, (int)Math.Round(sourceEditor.ClientSize.Width / printScale, MidpointRounding.AwayFromZero));
        int height = Math.Max(1, (int)Math.Round(width * (pageSize.Height / (float)Math.Max(1, pageSize.Width))));

        if (_printRenderer is not null &&
            _printRendererWidth == width &&
            _printRendererHeight == height)
        {
            return _printRenderer;
        }

        DisposePrintRenderer();

        var renderer = new MarkdownGdiEditor
        {
            Size = new Size(width, height),
            AllowAutoThemeChange = sourceEditor.AllowAutoThemeChange,
            ThemeMode = sourceEditor.ThemeMode,
            CanSideScroll = sourceEditor.CanSideScroll,
            SuppressEditableRawModes = true,
            BackColor = sourceEditor.BackColor,
            ForeColor = sourceEditor.ForeColor,
            SelectionColor = sourceEditor.SelectionColor,
            QuoteBarColor = sourceEditor.QuoteBarColor,
            CodeBackgroundColor = sourceEditor.CodeBackgroundColor,
            TableHeaderBackgroundColor = sourceEditor.TableHeaderBackgroundColor,
            TableCellBackgroundColor = sourceEditor.TableCellBackgroundColor,
            TableGridColor = sourceEditor.TableGridColor,
            InlineCodeBackgroundColor = sourceEditor.InlineCodeBackgroundColor,
            InlineCodeBorderColor = sourceEditor.InlineCodeBorderColor
        };

        _printRendererFont = (Font)sourceEditor.Font.Clone();
        renderer.Font = _printRendererFont;

        _ = renderer.Handle;
        renderer.LoadDocument(sourceEditor.Markdown, sourceEditor.DocumentBasePath, resetUndoStacks: true);
        renderer.PreparePresentationRender();

        _printRenderer = renderer;
        _printRendererWidth = width;
        _printRendererHeight = height;
        return renderer;
    }

    private void DisposePrintRenderer()
    {
        _printRenderer?.Dispose();
        _printRenderer = null;
        _printRendererWidth = 0;
        _printRendererHeight = 0;

        _printRendererFont?.Dispose();
        _printRendererFont = null;
    }

    private static float GetPrintBitmapScale(Graphics graphics, float sourceDpi)
    {
        float normalizedSourceDpi = Math.Max(1f, sourceDpi);
        float dpiScale = Math.Max(graphics.DpiX, graphics.DpiY) / normalizedSourceDpi;
        return Math.Clamp(Math.Max(2f, dpiScale), 1f, 4f);
    }

    private void ApplyTheme(EditorThemeMode themeMode)
    {
        _themeMode = themeMode;

        foreach (padTab tab in OpenTabs)
            tab.ApplyTheme(themeMode);

        UpdateThemeMenuChecks();
        SetStatusMessage($"Theme: {GetThemeLabel(themeMode)}");
        UpdateUiState();
    }

    private void RunFind(MarkdownGdiEditor editor, string query, FindOptions options)
    {
        bool found = editor.Find(query, options);
        SetStatusMessage(found ? $"Match: {query}" : $"No match: {query}");

        if (!found)
        {
            MessageBox.Show(this, $"\"{query}\" was not found.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        UpdateUiState();
    }

    private void UpdateUiState()
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = tab?.Editor;
        bool hasEditor = editor is not null;
        bool hasSelection = hasEditor && editor!.HasSelection;
        bool hasDirtyTabs = OpenTabs.Any(NeedsSaving);
        bool hasMultipleTabs = tabControl1.TabPages.Count > 1;

        saveToolStripMenuItem.Enabled = hasEditor;
        saveAsToolStripMenuItem.Enabled = hasEditor;
        saveAllToolStripMenuItem.Enabled = hasDirtyTabs;
        _recentToolStripMenuItem.Enabled = true;
        closeTabToolStripMenuItem.Enabled = tab is not null;
        closeAllTabsToolStripMenuItem.Enabled = tabControl1.TabPages.Count > 0;
        closeOtherTabsToolStripMenuItem.Enabled = hasMultipleTabs;
        printToolStripMenuItem.Enabled = hasEditor;
        printPreviewToolStripMenuItem.Enabled = hasEditor;

        undoToolStripMenuItem.Enabled = hasEditor && editor!.CanUndo;
        redoToolStripMenuItem.Enabled = hasEditor && editor!.CanRedo;
        cutToolStripMenuItem.Enabled = hasEditor && editor!.CanCut;
        copyToolStripMenuItem.Enabled = hasEditor && editor!.CanCopy;
        pasteToolStripMenuItem.Enabled = hasEditor && editor!.CanPaste;
        selectAllToolStripMenuItem.Enabled = hasEditor && editor!.CanSelectAll;
        formatToolStripMenuItem.Enabled = hasEditor;
        insertLinkToolStripMenuItem.Enabled = hasEditor;
        insertImageToolStripMenuItem.Enabled = hasEditor;
        insertFrameToolStripMenuItem.Enabled = hasEditor;
        insertProgressToolStripMenuItem.Enabled = hasEditor;
        tableDesignerToolStripMenuItem.Enabled = hasEditor;
        headingToolStripMenuItem.Enabled = hasEditor;
        quoteToolStripMenuItem.Enabled = hasEditor;
        codeFenceToolStripMenuItem.Enabled = hasEditor;
        _foregroundColorToolStripMenuItem.Enabled = hasSelection;
        _backgroundColorToolStripMenuItem.Enabled = hasSelection;

        findToolStripMenuItem.Enabled = hasEditor;
        findNextToolStripMenuItem.Enabled = hasEditor && editor!.CanFindNext;

        newToolStripButton.Enabled = true;
        openToolStripButton.Enabled = true;
        saveToolStripButton.Enabled = hasEditor;
        saveAllToolStripButton.Enabled = hasDirtyTabs;
        undoToolStripButton.Enabled = hasEditor && editor!.CanUndo;
        redoToolStripButton.Enabled = hasEditor && editor!.CanRedo;
        cutToolStripButton.Enabled = hasEditor && editor!.CanCut;
        copyToolStripButton.Enabled = hasEditor && editor!.CanCopy;
        pasteToolStripButton.Enabled = hasEditor && editor!.CanPaste;
        selectAllToolStripButton.Enabled = hasEditor && editor!.CanSelectAll;
        findToolStripButton.Enabled = hasEditor;
        findNextToolStripButton.Enabled = hasEditor && editor!.CanFindNext;
        linkToolStripButton.Enabled = hasEditor;
        imageToolStripButton.Enabled = hasEditor;
        frameToolStripButton.Enabled = hasEditor;
        progressToolStripButton.Enabled = hasEditor;
        tableToolStripButton.Enabled = hasEditor;
        headingToolStripDropDownButton.Enabled = hasEditor;
        quoteToolStripButton.Enabled = hasEditor;
        codeFenceToolStripButton.Enabled = hasEditor;
        _foregroundColorToolStripButton.Enabled = hasSelection;
        _backgroundColorToolStripButton.Enabled = hasSelection;

        closeContextTabToolStripMenuItem.Enabled = tab is not null;
        closeOtherContextTabsToolStripMenuItem.Enabled = hasMultipleTabs;
        closeAllContextTabsToolStripMenuItem.Enabled = tabControl1.TabPages.Count > 0;

        UpdateEditorContextMenuState(editor);
        UpdateViewScaleToolbarState(tab);
        Text = tab is null ? AppTitle : $"{tab.DisplayName} - {AppTitle}";
        UpdateStatusBar();
    }

    private void EditorContextMenuStrip_Opening(object? sender, CancelEventArgs e)
    {
        MarkdownGdiEditor? editor = ActiveEditor;
        UpdateEditorContextMenuState(editor);
        e.Cancel = editor is null;
    }

    private void UpdateEditorContextMenuState(MarkdownGdiEditor? editor)
    {
        bool hasEditor = editor is not null;
        bool hasSelection = hasEditor && editor!.HasSelection;

        _editorContextUndoMenuItem.Enabled = hasEditor && editor!.CanUndo;
        _editorContextRedoMenuItem.Enabled = hasEditor && editor!.CanRedo;
        _editorContextCutMenuItem.Enabled = hasEditor && editor!.CanCut;
        _editorContextCopyMenuItem.Enabled = hasEditor && editor!.CanCopy;
        _editorContextPasteMenuItem.Enabled = hasEditor && editor!.CanPaste;
        _editorContextSelectAllMenuItem.Enabled = hasEditor && editor!.CanSelectAll;
        _editorContextInsertLinkMenuItem.Enabled = hasEditor;
        _editorContextInsertImageMenuItem.Enabled = hasEditor;
        _editorContextInsertFrameMenuItem.Enabled = hasEditor;
        _editorContextInsertProgressMenuItem.Enabled = hasEditor;
        _editorContextInsertTableMenuItem.Enabled = hasEditor;
        _editorContextHeadingMenuItem.Enabled = hasEditor;
        _editorContextQuoteMenuItem.Enabled = hasEditor;
        _editorContextCodeFenceMenuItem.Enabled = hasEditor;
        _editorContextForegroundColorMenuItem.Enabled = hasSelection;
        _editorContextBackgroundColorMenuItem.Enabled = hasSelection;
    }

    private void UpdateStatusBar()
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = tab?.Editor;

        documentStatusLabel.Text = tab is null ? "Document: -" : $"Document: {tab.DisplayName}";
        pathStatusLabel.Text = $"Path: {(tab?.FilePath ?? "Unsaved")}";
        themeStatusLabel.Text = $"Theme: {GetThemeLabel(_themeMode)} | View: {(tab is null ? "-" : FormatViewScale(tab.ViewScale))}";

        if (editor is null)
        {
            positionStatusLabel.Text = "Position: -";
        }
        else
        {
            MarkdownPosition caret = editor.CaretPosition;
            string selection = editor.HasSelection ? " | Selection active" : string.Empty;
            positionStatusLabel.Text = $"Position: Ln {caret.Line + 1}, Col {caret.Column + 1}{selection}";
        }

        messageStatusLabel.Text = _statusMessage;
    }

    private void UpdateThemeMenuChecks()
    {
        themeSystemToolStripDropDownItem.Checked = _themeMode == EditorThemeMode.System;
        themeLightToolStripDropDownItem.Checked = _themeMode == EditorThemeMode.Light;
        themeDarkToolStripDropDownItem.Checked = _themeMode == EditorThemeMode.Dark;
    }

    private void SetStatusMessage(string message)
    {
        _statusMessage = message;
        UpdateStatusBar();
    }

    private string ResolveInitialDirectory(padTab tab)
    {
        if (!string.IsNullOrWhiteSpace(tab.FilePath))
            return Path.GetDirectoryName(tab.FilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrWhiteSpace(_lastDirectory) && Directory.Exists(_lastDirectory))
            return _lastDirectory;

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string BuildSuggestedFileName(padTab tab)
    {
        string baseName = tab.IsUntitled ? tab.DocumentName : Path.GetFileName(tab.FilePath!) ?? tab.DocumentName;
        return Path.ChangeExtension(baseName, ".md");
    }

    private bool TryHandleViewScaleShortcut(Keys keyData)
    {
        Keys keyCode = keyData & Keys.KeyCode;
        Keys modifiers = keyData & Keys.Modifiers;

        if (modifiers == Keys.Control)
        {
            return keyCode switch
            {
                Keys.Add or Keys.Oemplus => AdjustViewScale(ActiveTab, +ViewScaleStep),
                Keys.Subtract or Keys.OemMinus => AdjustViewScale(ActiveTab, -ViewScaleStep),
                Keys.D0 or Keys.NumPad0 => SetViewScale(ActiveTab, DefaultViewScale),
                _ => false
            };
        }

        if (modifiers == (Keys.Control | Keys.Shift) && keyCode == Keys.Oemplus)
            return AdjustViewScale(ActiveTab, +ViewScaleStep);

        return false;
    }

    private bool AdjustViewScale(padTab? tab, float delta)
        => tab is not null && SetViewScale(tab, tab.ViewScale + delta);

    private bool SetViewScale(padTab? tab, float viewScale, bool restoreEditorFocus = true)
    {
        if (tab is null)
            return false;

        float normalized = NormalizeViewScale(viewScale);
        bool changed = Math.Abs(tab.ViewScale - normalized) >= 0.001f;

        tab.ApplyViewScale(normalized);

        UpdateUiState();

        if (changed)
            SetStatusMessage($"View scale: {FormatViewScale(tab.ViewScale)}");

        if (restoreEditorFocus && tab == ActiveTab)
            FocusEditor(tab);

        return true;
    }

    private static float NormalizeViewScale(float viewScale)
    {
        if (float.IsNaN(viewScale) || float.IsInfinity(viewScale))
            return DefaultViewScale;

        float rounded = (float)Math.Round(viewScale, 2, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, MinViewScale, MaxViewScale);
    }

    private static string FormatViewScale(float viewScale)
        => $"{(int)Math.Round(viewScale * 100f, MidpointRounding.AwayFromZero)}%";

    private void ConfigureViewScaleToolbar()
    {
        _viewScaleTrackBar.AutoSize = false;
        _viewScaleTrackBar.Minimum = ScaleToTrackBarValue(MinViewScale);
        _viewScaleTrackBar.Maximum = ScaleToTrackBarValue(MaxViewScale);
        _viewScaleTrackBar.TickFrequency = 25;
        _viewScaleTrackBar.TickStyle = TickStyle.None;
        _viewScaleTrackBar.SmallChange = 5;
        _viewScaleTrackBar.LargeChange = 25;
        _viewScaleTrackBar.Size = new Size(140, 24);
        _viewScaleTrackBar.Margin = Padding.Empty;
        _viewScaleTrackBar.Value = ScaleToTrackBarValue(DefaultViewScale);
        _viewScaleTrackBar.ValueChanged += ViewScaleTrackBar_ValueChanged;

        _viewScaleToolStripLabel.Margin = new Padding(8, 0, 4, 0);
        _viewScaleValueToolStripLabel.Margin = new Padding(4, 0, 0, 0);
        _viewScaleValueToolStripLabel.TextAlign = ContentAlignment.MiddleLeft;
        _viewScaleValueToolStripLabel.Text = FormatViewScale(DefaultViewScale);

        _viewScaleTrackBarHost = new ToolStripControlHost(_viewScaleTrackBar)
        {
            Name = "viewScaleTrackBarHost",
            AutoSize = false,
            Size = new Size(150, 28),
            Margin = new Padding(0)
        };

        _viewScaleToolStripSeparator = new ToolStripSeparator { Name = "viewScaleToolStripSeparator" };

        padToolStrip.Items.Add(_viewScaleToolStripSeparator);
        padToolStrip.Items.Add(_viewScaleToolStripLabel);
        padToolStrip.Items.Add(_viewScaleTrackBarHost);
        padToolStrip.Items.Add(_viewScaleValueToolStripLabel);
    }

    private void ViewScaleTrackBar_ValueChanged(object? sender, EventArgs e)
    {
        if (_suppressViewScaleTrackBarChange || ActiveTab is null)
            return;

        SetViewScale(ActiveTab, _viewScaleTrackBar.Value / 100f, restoreEditorFocus: false);
    }

    private void UpdateViewScaleToolbarState(padTab? tab)
    {
        bool enabled = tab is not null;
        float viewScale = tab?.ViewScale ?? DefaultViewScale;

        _suppressViewScaleTrackBarChange = true;
        try
        {
            _viewScaleTrackBar.Enabled = enabled;
            _viewScaleTrackBar.Value = ScaleToTrackBarValue(viewScale);
        }
        finally
        {
            _suppressViewScaleTrackBarChange = false;
        }

        _viewScaleToolStripLabel.Enabled = enabled;
        _viewScaleValueToolStripLabel.Enabled = enabled;
        _viewScaleValueToolStripLabel.Text = enabled ? FormatViewScale(viewScale) : "-";

        if (_viewScaleTrackBarHost is not null)
            _viewScaleTrackBarHost.Enabled = enabled;
    }

    private static int ScaleToTrackBarValue(float viewScale)
        => Math.Clamp(
            (int)Math.Round(NormalizeViewScale(viewScale) * 100f, MidpointRounding.AwayFromZero),
            (int)Math.Round(MinViewScale * 100f, MidpointRounding.AwayFromZero),
            (int)Math.Round(MaxViewScale * 100f, MidpointRounding.AwayFromZero));

    private padTab? FindTabByPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        string fullPath = Path.GetFullPath(filePath);
        return OpenTabs.FirstOrDefault(tab => string.Equals(tab.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
    }

    private padTab? FindTabByEditor(MarkdownGdiEditor editor)
        => OpenTabs.FirstOrDefault(tab => ReferenceEquals(tab.Editor, editor));

    private padTab? GetReusableBlankTab()
    {
        if (tabControl1.TabPages.Count != 1)
            return null;

        padTab? tab = ActiveTab;
        if (tab is null || tab.Modified || !tab.IsUntitled)
            return null;

        return string.IsNullOrEmpty(tab.Editor.Markdown) ? tab : null;
    }

    private static bool NeedsSaving(padTab tab) => tab.Modified;

    private static string GetThemeLabel(EditorThemeMode themeMode)
    {
        return themeMode switch
        {
            EditorThemeMode.Light => "Light",
            EditorThemeMode.Dark => "Dark",
            _ => "System"
        };
    }

    private static string BuildInitialDialogTitle(string? selectedText)
    {
        string normalized = (selectedText ?? string.Empty)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Trim();

        return normalized.Length > 120 ? normalized[..120].TrimEnd() : normalized;
    }

    private void SelectAdjacentTab(int offset)
    {
        if (tabControl1.TabCount <= 1)
            return;

        int nextIndex = (tabControl1.SelectedIndex + offset + tabControl1.TabCount) % tabControl1.TabCount;
        tabControl1.SelectedIndex = nextIndex;
        if (ActiveTab is not null)
            FocusEditor(ActiveTab);
    }

    private static void FocusEditor(padTab tab)
    {
        if (!tab.IsDisposed && tab.Editor.CanFocus)
            tab.Editor.Focus();
    }

    private bool ExecuteOnActiveEditor(Action<MarkdownGdiEditor> action)
    {
        MarkdownGdiEditor? editor = ActiveEditor;
        if (editor is null)
            return false;

        action(editor);
        UpdateUiState();
        return true;
    }

    private void Tab_DocumentStateChanged(object? sender, EventArgs e)
    {
        UpdateUiState();
    }

    private void Editor_MarkdownChanged(object? sender, MarkdownChangedEventArgs e)
    {
        _statusMessage = "Document changed";
        UpdateStatusBar();
    }

    private void Editor_ViewScaleRequested(object? sender, ViewScaleRequestedEventArgs e)
    {
        if (sender is not MarkdownGdiEditor editor)
            return;

        padTab? tab = FindTabByEditor(editor);
        if (tab is not null && tab != ActiveTab)
            tabControl1.SelectedTab = tab;

        if (e.Delta > 0)
            AdjustViewScale(tab, +ViewScaleStep);
        else if (e.Delta < 0)
            AdjustViewScale(tab, -ViewScaleStep);
    }

    private void Editor_FindRequested(object? sender, FindRequestedEventArgs e)
    {
        if (sender is not MarkdownGdiEditor editor)
            return;

        padTab? tab = FindTabByEditor(editor);
        if (tab is not null)
            tabControl1.SelectedTab = tab;

        ShowFindDialog(e.CurrentQuery, e.CurrentOptions);
    }

    private void Editor_LinkActivated(object? sender, LinkActivatedEventArgs e)
    {
        if (sender is not MarkdownGdiEditor editor)
            return;

        padTab? tab = FindTabByEditor(editor);
        if (tab is not null)
            tabControl1.SelectedTab = tab;

        try
        {
            if (e.IsMarkdownDocument)
            {
                HandleMarkdownLink(tab, e);
                return;
            }

            if (e.IsWebLink)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.ResolvedTarget,
                    UseShellExecute = true
                });

                SetStatusMessage($"Opened link: {e.ResolvedTarget}");
                return;
            }

            if (!File.Exists(e.ResolvedTarget))
            {
                MessageBox.Show(
                    this,
                    $"The linked file could not be found:\n{e.ResolvedTarget}",
                    "Open Link",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = e.ResolvedTarget,
                UseShellExecute = true
            });

            SetStatusMessage($"Opened link: {Path.GetFileName(e.ResolvedTarget)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The link could not be opened:\n{e.Target}\n\n{ex.Message}",
                "Open Link",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ConfigureDynamicUi()
    {
        int recentIndex = fileToolStripMenuItem.DropDownItems.IndexOf(fileToolStripSeparator1);
        if (recentIndex >= 0)
            fileToolStripMenuItem.DropDownItems.Insert(recentIndex, _recentToolStripMenuItem);
    }

    private void ApplyInlineColor(InlineColorTarget target)
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = ActiveEditor;
        if (tab is null || editor is null)
            return;

        if (!editor.HasSelection || string.IsNullOrEmpty(editor.SelectedText))
        {
            SetStatusMessage("Select text before applying a color");
            FocusEditor(tab);
            return;
        }

        Color initialColor = target == InlineColorTarget.Foreground
            ? _lastForegroundColor
            : _lastBackgroundColor;

        if (!TryPickColor(initialColor, out Color chosenColor))
        {
            FocusEditor(tab);
            return;
        }

        string token = target == InlineColorTarget.Foreground ? "FG" : "BG";
        string wrapped = WrapSelectedTextWithColor(editor.SelectedText, token, chosenColor);

        editor.InsertMarkdownSnippetCommand(wrapped);

        if (target == InlineColorTarget.Foreground)
        {
            _lastForegroundColor = chosenColor;
            _foregroundColorToolStripButton.ForeColor = chosenColor;
        }
        else
        {
            _lastBackgroundColor = chosenColor;
            _backgroundColorToolStripButton.BackColor = chosenColor;
        }

        FocusEditor(tab);
        SetStatusMessage(target == InlineColorTarget.Foreground
            ? "Foreground color applied"
            : "Background color applied");
        UpdateUiState();
    }

    private bool TryPickColor(Color initialColor, out Color color)
    {
        using var dialog = new ColorDialog
        {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
            Color = initialColor
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            color = initialColor;
            return false;
        }

        color = dialog.Color;
        return true;
    }

    private static string ColorToMarkdownHex(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string WrapSelectedTextWithColor(string selectedText, string token, Color color)
    {
        string normalized = (selectedText ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        if (normalized.IndexOf('\n') < 0)
            return BuildColorWrapper(token, color, normalized);

        string[] lines = normalized.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
                lines[i] = BuildColorWrapper(token, color, lines[i]);
        }

        return string.Join('\n', lines);
    }

    private static string BuildColorWrapper(string token, Color color, string text)
        => $"![{token}:{ColorToMarkdownHex(color)}]({text})";

    private void HandleMarkdownLink(padTab? sourceTab, LinkActivatedEventArgs e)
    {
        padTab? targetTab = ResolveMarkdownLinkTargetTab(sourceTab, e);
        if (targetTab is null)
            return;

        tabControl1.SelectedTab = targetTab;
        FocusEditor(targetTab);

        if (e.HasFragment)
        {
            if (!targetTab.Editor.NavigateToMarkdownAnchor(e.Fragment))
            {
                MessageBox.Show(
                    this,
                    $"The target heading could not be found:\n{e.Fragment}",
                    "Open Link",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string statusTarget = !string.IsNullOrWhiteSpace(targetTab.FilePath)
                ? Path.GetFileName(targetTab.FilePath)
                : targetTab.DocumentName;

            SetStatusMessage($"Jumped to {e.Fragment} in {statusTarget}");
            return;
        }

        string documentLabel = !string.IsNullOrWhiteSpace(targetTab.FilePath)
            ? Path.GetFileName(targetTab.FilePath)
            : targetTab.DocumentName;

        SetStatusMessage($"Opened link: {documentLabel}");
    }

    private padTab? ResolveMarkdownLinkTargetTab(padTab? sourceTab, LinkActivatedEventArgs e)
    {
        if (e.IsCurrentDocument || string.IsNullOrWhiteSpace(e.ResolvedTarget))
            return sourceTab;

        if (sourceTab is not null &&
            !string.IsNullOrWhiteSpace(sourceTab.FilePath) &&
            string.Equals(sourceTab.FilePath, e.ResolvedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return sourceTab;
        }

        if (!File.Exists(e.ResolvedTarget))
        {
            MessageBox.Show(
                this,
                $"The linked Markdown document could not be found:\n{e.ResolvedTarget}",
                "Open Link",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return null;
        }

        return OpenDocumentsFromPaths([e.ResolvedTarget]);
    }

    private void Editor_StateAffectingEvent(object? sender, EventArgs e)
    {
        if (sender == ActiveEditor)
            UpdateUiState();
    }

    private void Editor_MouseMove(object? sender, MouseEventArgs e)
    {
        if (sender == ActiveEditor && e.Button != MouseButtons.None)
            UpdateUiState();
    }

    private void TabControl_MouseUp(object? sender, MouseEventArgs e)
    {
        _contextTabIndex = GetTabIndexAt(e.Location);

        if (_contextTabIndex < 0)
            return;

        if (e.Button == MouseButtons.Middle)
        {
            if (tabControl1.TabPages[_contextTabIndex] is padTab tab)
            {
                tabControl1.SelectedIndex = _contextTabIndex;
                CloseTab(tab, ensureReplacementTab: true);
            }

            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            tabControl1.SelectedIndex = _contextTabIndex;
            UpdateUiState();
            tabContextMenuStrip.Show(tabControl1, e.Location);
        }
    }

    private int GetTabIndexAt(Point location)
    {
        for (int i = 0; i < tabControl1.TabCount; i++)
        {
            if (tabControl1.GetTabRect(i).Contains(location))
                return i;
        }

        return -1;
    }

    private padTab? GetContextTab()
    {
        return _contextTabIndex >= 0 && _contextTabIndex < tabControl1.TabPages.Count
            ? tabControl1.TabPages[_contextTabIndex] as padTab
            : null;
    }

    private void HandleDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void HandleDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            OpenDocumentsFromPaths(files);
    }

    private void frmMain_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            ApplicationStateStore.Save(CaptureApplicationState());
        }
        catch
        {
            // Keep application shutdown non-blocking even if the session file cannot be written.
        }
    }

    private void RevealAndActivateWindow()
    {
        if (!Visible)
            Show();

        if (WindowState == FormWindowState.Minimized && IsHandleCreated)
            ShowWindow(Handle, SwRestore);

        Activate();
        BringToFront();
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
        aboutDiag abt = new();
            abt.ShowDialog(this);
    }
}
