using System.Drawing.Printing;
using MarkdownGdi;

namespace MarkdownPad;

public partial class frmMain : Form
{
    private const string AppTitle = "MarkdownPad";

    private readonly List<string> _printLines = [];
    private readonly Queue<string> _pendingPrintSegments = [];

    private EditorThemeMode _themeMode = EditorThemeMode.System;
    private string? _lastDirectory;
    private string _statusMessage = "Ready";
    private int _untitledCounter = 1;
    private int _printLineIndex;
    private int _contextTabIndex = -1;

    public frmMain()
    {
        InitializeComponent();

        Text = AppTitle;
        KeyPreview = true;

        ConfigureMenus();
        ConfigureToolbar();
        ConfigurePrinting();
        ConfigureTabControl();
        ConfigureDragAndDrop();

        CreateNewTab();
        UpdateUiState();
    }

    private padTab? ActiveTab => tabControl1.SelectedTab as padTab;

    private MarkdownGdiEditor? ActiveEditor => ActiveTab?.Editor;

    private IEnumerable<padTab> OpenTabs => tabControl1.TabPages.OfType<padTab>();

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
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
        heading1ToolStripMenuItem.Click += (_, _) => ApplyHeading(1);
        heading2ToolStripMenuItem.Click += (_, _) => ApplyHeading(2);
        heading3ToolStripMenuItem.Click += (_, _) => ApplyHeading(3);
        heading4ToolStripMenuItem.Click += (_, _) => ApplyHeading(4);
        heading5ToolStripMenuItem.Click += (_, _) => ApplyHeading(5);
        heading6ToolStripMenuItem.Click += (_, _) => ApplyHeading(6);
        quoteToolStripMenuItem.Click += (_, _) => ToggleQuoteBlock();
        codeFenceToolStripMenuItem.Click += (_, _) => WrapSelectionInCodeFence();

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
        findToolStripButton.Click += (_, _) => ShowFindDialog();
        findNextToolStripButton.Click += (_, _) => FindNextInActiveDocument();
        tableToolStripButton.Click += (_, _) => ShowTableDesigner();
        heading1ToolStripDropDownItem.Click += (_, _) => ApplyHeading(1);
        heading2ToolStripDropDownItem.Click += (_, _) => ApplyHeading(2);
        heading3ToolStripDropDownItem.Click += (_, _) => ApplyHeading(3);
        heading4ToolStripDropDownItem.Click += (_, _) => ApplyHeading(4);
        heading5ToolStripDropDownItem.Click += (_, _) => ApplyHeading(5);
        heading6ToolStripDropDownItem.Click += (_, _) => ApplyHeading(6);
        quoteToolStripButton.Click += (_, _) => ToggleQuoteBlock();
        codeFenceToolStripButton.Click += (_, _) => WrapSelectionInCodeFence();

        themeSystemToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.System);
        themeLightToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.Light);
        themeDarkToolStripDropDownItem.Click += (_, _) => ApplyTheme(EditorThemeMode.Dark);
    }

    private void ConfigurePrinting()
    {
        printDialog.Document = printDocument;
        printPreviewDialog.Document = printDocument;

        printDocument.BeginPrint += (_, _) =>
        {
            _printLineIndex = 0;
            _pendingPrintSegments.Clear();
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

    private padTab CreateNewTab(bool select = true)
    {
        var tab = new padTab($"Untitled {_untitledCounter++}", _themeMode);
        tab.DocumentStateChanged += Tab_DocumentStateChanged;

        tab.Editor.MarkdownChanged += Editor_MarkdownChanged;
        tab.Editor.FindRequested += Editor_FindRequested;
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

    private void OpenDocumentsFromPaths(IEnumerable<string> filePaths)
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
        if (!PreparePrintBuffer())
            return;

        try
        {
            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                printDocument.Print();
                SetStatusMessage("Print job started");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Printing failed.\n\n{ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PreviewActiveDocument()
    {
        if (!PreparePrintBuffer())
            return;

        try
        {
            printPreviewDialog.ShowDialog(this);
            SetStatusMessage("Print preview opened");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"The print preview could not be opened.\n\n{ex.Message}",
                "Print Preview",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private bool PreparePrintBuffer()
    {
        padTab? tab = ActiveTab;
        if (tab is null)
            return false;

        _printLines.Clear();
        _pendingPrintSegments.Clear();
        _printLines.AddRange(tab.Editor.Markdown.Replace("\r\n", "\n").Split('\n'));
        _printLineIndex = 0;
        printDocument.DocumentName = tab.DocumentName;
        return true;
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        Graphics graphics = e.Graphics ?? throw new InvalidOperationException("Print graphics are not available.");
        using var printFont = new Font("Consolas", 10f);
        float lineHeight = printFont.GetHeight(graphics);
        float y = e.MarginBounds.Top;

        while (_printLineIndex < _printLines.Count || _pendingPrintSegments.Count > 0)
        {
            if (_pendingPrintSegments.Count == 0)
            {
                string line = _printLines[_printLineIndex];
                foreach (string wrappedLine in WrapPrintLine(graphics, line, printFont, e.MarginBounds.Width))
                    _pendingPrintSegments.Enqueue(wrappedLine);

                _printLineIndex++;
            }

            string nextSegment = _pendingPrintSegments.Peek();
            if (y + lineHeight > e.MarginBounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }

            graphics.DrawString(nextSegment, printFont, Brushes.Black, e.MarginBounds.Left, y, StringFormat.GenericTypographic);
            y += lineHeight;
            _pendingPrintSegments.Dequeue();
        }

        e.HasMorePages = false;
        _printLineIndex = 0;
        _pendingPrintSegments.Clear();
    }

    private static IEnumerable<string> WrapPrintLine(Graphics graphics, string text, Font font, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield return string.Empty;
            yield break;
        }

        int start = 0;

        while (start < text.Length)
        {
            int bestLength = FindBestPrintChunkLength(graphics, text, start, font, maxWidth);
            if (bestLength <= 0)
                bestLength = 1;

            int breakLength = bestLength;
            if (start + breakLength < text.Length)
            {
                int lastBreak = text.LastIndexOfAny([' ', '\t', '-', '/'], start + breakLength - 1, breakLength);
                if (lastBreak >= start + Math.Max(1, breakLength / 2))
                    breakLength = lastBreak - start + 1;
            }

            yield return text.Substring(start, breakLength).TrimEnd('\r');
            start += breakLength;
        }
    }

    private static int FindBestPrintChunkLength(Graphics graphics, string text, int start, Font font, int maxWidth)
    {
        int low = 1;
        int high = text.Length - start;
        int best = 0;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            string candidate = text.Substring(start, mid);
            float width = graphics.MeasureString(candidate, font, PointF.Empty, StringFormat.GenericTypographic).Width;

            if (width <= maxWidth)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
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
        bool hasDirtyTabs = OpenTabs.Any(NeedsSaving);
        bool hasMultipleTabs = tabControl1.TabPages.Count > 1;

        saveToolStripMenuItem.Enabled = hasEditor;
        saveAsToolStripMenuItem.Enabled = hasEditor;
        saveAllToolStripMenuItem.Enabled = hasDirtyTabs;
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
        tableDesignerToolStripMenuItem.Enabled = hasEditor;
        headingToolStripMenuItem.Enabled = hasEditor;
        quoteToolStripMenuItem.Enabled = hasEditor;
        codeFenceToolStripMenuItem.Enabled = hasEditor;

        findToolStripMenuItem.Enabled = hasEditor;
        findNextToolStripMenuItem.Enabled = hasEditor && editor!.CanFindNext;

        newToolStripButton.Enabled = true;
        openToolStripButton.Enabled = true;
        saveToolStripButton.Enabled = hasEditor;
        saveAllToolStripButton.Enabled = hasDirtyTabs;
        undoToolStripButton.Enabled = hasEditor && editor!.CanUndo;
        redoToolStripButton.Enabled = hasEditor && editor!.CanRedo;
        findToolStripButton.Enabled = hasEditor;
        findNextToolStripButton.Enabled = hasEditor && editor!.CanFindNext;
        tableToolStripButton.Enabled = hasEditor;
        headingToolStripDropDownButton.Enabled = hasEditor;
        quoteToolStripButton.Enabled = hasEditor;
        codeFenceToolStripButton.Enabled = hasEditor;

        closeContextTabToolStripMenuItem.Enabled = tab is not null;
        closeOtherContextTabsToolStripMenuItem.Enabled = hasMultipleTabs;
        closeAllContextTabsToolStripMenuItem.Enabled = tabControl1.TabPages.Count > 0;

        Text = tab is null ? AppTitle : $"{tab.DisplayName} - {AppTitle}";
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        padTab? tab = ActiveTab;
        MarkdownGdiEditor? editor = tab?.Editor;

        documentStatusLabel.Text = tab is null ? "Document: -" : $"Document: {tab.DisplayName}";
        pathStatusLabel.Text = $"Path: {(tab?.FilePath ?? "Unsaved")}";
        themeStatusLabel.Text = $"Theme: {GetThemeLabel(_themeMode)}";

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
        SetStatusMessage("Document changed");
        UpdateUiState();
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
        if (!CloseTabs(OpenTabs.ToList(), ensureReplacementTab: false))
            e.Cancel = true;
    }
}
