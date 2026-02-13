using MarkdownGdi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace MarkdownEditor
{
    public partial class mkEditor : Form
    {
        private static readonly string SessionFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarkdownEditor");

        private static readonly string SessionFilePath =
            Path.Combine(SessionFolder, "session.json");

        private int _untitledCounter = 1;
        private string _lastFindText = string.Empty;

        private Font _editorFont = new Font("Consolas", 11f, FontStyle.Regular, GraphicsUnit.Point);

        private string _printBuffer = string.Empty;
        private int _printCharIndex = 0;
        private Font _printFont;

        public mkEditor()
        {
            InitializeComponent();

            _printFont = _editorFont;
            pageSetupDialog1.Document = printDocument1;
            printDialog1.Document = printDocument1;

            LoadSessionOrCreateDefault();
            UpdateUiState();
        }

        // -------------------------
        // Session restore / save
        // -------------------------
        private void LoadSessionOrCreateDefault()
        {
            if (!File.Exists(SessionFilePath))
            {
                CreateNewTabAndSelect();
                return;
            }

            try
            {
                string json = File.ReadAllText(SessionFilePath, Encoding.UTF8);
                EditorSession? session = JsonSerializer.Deserialize<EditorSession>(json);

                if (session == null || session.Tabs.Count == 0)
                {
                    CreateNewTabAndSelect();
                    return;
                }

                ApplySessionSettings(session);

                tabEditors.TabPages.Clear();

                int maxUntitledNumber = 0;

                foreach (EditorTabSession tabSession in session.Tabs)
                {
                    int untitledNumber = tabSession.UntitledNumber > 0
                        ? tabSession.UntitledNumber
                        : NextUntitledNumber();

                    maxUntitledNumber = Math.Max(maxUntitledNumber, untitledNumber);

                    float zoom = tabSession.ZoomFactor <= 0f ? 1f : tabSession.ZoomFactor;
                    string encoding = string.IsNullOrWhiteSpace(tabSession.EncodingName)
                        ? "UTF-8"
                        : tabSession.EncodingName;

                    CreateTab(
                        initialText: tabSession.Content ?? string.Empty,
                        filePath: tabSession.FilePath,
                        isModified: tabSession.IsModified,
                        untitledNumber: untitledNumber,
                        zoomFactor: zoom,
                        caretIndex: Math.Max(0, tabSession.CaretIndex),
                        encodingName: encoding);
                }

                _untitledCounter = Math.Max(_untitledCounter, maxUntitledNumber + 1);

                if (tabEditors.TabPages.Count == 0)
                {
                    CreateNewTabAndSelect();
                }
                else
                {
                    int selected = session.SelectedIndex;
                    tabEditors.SelectedIndex = (selected >= 0 && selected < tabEditors.TabPages.Count) ? selected : 0;
                }

                
                ApplyStatusBarVisibility();
                UpdateUiState();
            }
            catch
            {
                tabEditors.TabPages.Clear();
                CreateNewTabAndSelect();
            }
        }

        private void SaveSession()
        {
            Directory.CreateDirectory(SessionFolder);

            var session = new EditorSession
            {
                SelectedIndex = tabEditors.SelectedIndex,
                WordWrap = mnuWordWrap.Checked,
                ShowStatusBar = mnuStatusBar.Checked,
                FontFamily = _editorFont.FontFamily.Name,
                FontSize = _editorFont.Size,
                FontStyleValue = (int)_editorFont.Style
            };

            foreach (TabPage tab in tabEditors.TabPages)
            {
                if (tab.Tag is not EditorTabContext ctx)
                    continue;

                session.Tabs.Add(new EditorTabSession
                {
                    FilePath = ctx.FilePath,
                    UntitledNumber = ctx.UntitledNumber,
                    Content = ctx.Editor.Text,
                    IsModified = ctx.IsModified,
                    EncodingName = ctx.EncodingName
                });
            }

            string json = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SessionFilePath, json, Encoding.UTF8);
        }

        private void ApplySessionSettings(EditorSession session)
        {
            mnuWordWrap.Checked = session.WordWrap;
            mnuStatusBar.Checked = session.ShowStatusBar;

            string family = string.IsNullOrWhiteSpace(session.FontFamily) ? "Consolas" : session.FontFamily;
            float size = session.FontSize > 1f ? session.FontSize : 11f;

            FontStyle style = FontStyle.Regular;
            if (Enum.IsDefined(typeof(FontStyle), session.FontStyleValue))
            {
                style = (FontStyle)session.FontStyleValue;
            }

            try
            {
                _editorFont = new Font(family, size, style, GraphicsUnit.Point);
            }
            catch
            {
                _editorFont = new Font("Consolas", 11f, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                SaveSession();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Session could not be saved.\n\n{ex.Message}",
                    "MarkdownEditor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // -------------------------
        // Tab / editor infrastructure
        // -------------------------
        private int NextUntitledNumber() => _untitledCounter++;

        private TabPage CreateTab(
            string initialText,
            string? filePath,
            bool isModified,
            int untitledNumber,
            float zoomFactor,
            int caretIndex,
            string encodingName)
        {
            var editor = new MarkdownGdiEditor
            {
                Dock = DockStyle.Fill,
            };

            

            var tab = new TabPage();
            tab.Controls.Add(editor);

            var ctx = new EditorTabContext
            {
                Editor = editor,
                FilePath = filePath,
                UntitledNumber = untitledNumber,
                IsModified = false,
                IsLoading = true,
                EncodingName = string.IsNullOrWhiteSpace(encodingName) ? "UTF-8" : encodingName
            };

            tab.Tag = ctx;
            tabEditors.TabPages.Add(tab);

            editor.TextChanged += Editor_TextChanged;

            editor.Text = initialText ?? string.Empty;


            ctx.IsLoading = false;
            ctx.IsModified = isModified;

            UpdateTabTitle(tab);
            return tab;
        }

        private void CreateNewTabAndSelect()
        {
            TabPage tab = CreateTab(
                initialText: string.Empty,
                filePath: null,
                isModified: false,
                untitledNumber: NextUntitledNumber(),
                zoomFactor: 1f,
                caretIndex: 0,
                encodingName: "UTF-8");

            tabEditors.SelectedTab = tab;
            (tab.Tag as EditorTabContext)?.Editor.Focus();
            UpdateUiState();
        }

        private EditorTabContext? ActiveContext => tabEditors.SelectedTab?.Tag as EditorTabContext;
        private MarkdownGdiEditor? ActiveEditor => ActiveContext?.Editor;

        private TabPage? FindTabForEditor(RichTextBox editor)
        {
            foreach (TabPage tab in tabEditors.TabPages)
            {
                if (tab.Tag is EditorTabContext ctx && ReferenceEquals(ctx.Editor, editor))
                    return tab;
            }

            return null;
        }

        private EditorTabContext? FindContextForEditor(RichTextBox editor)
        {
            foreach (TabPage tab in tabEditors.TabPages)
            {
                if (tab.Tag is EditorTabContext ctx && ReferenceEquals(ctx.Editor, editor))
                    return ctx;
            }

            return null;
        }

        private TabPage? FindTabByFilePath(string filePath)
        {
            string full = Path.GetFullPath(filePath);

            foreach (TabPage tab in tabEditors.TabPages)
            {
                if (tab.Tag is not EditorTabContext ctx || string.IsNullOrWhiteSpace(ctx.FilePath))
                    continue;

                if (string.Equals(Path.GetFullPath(ctx.FilePath), full, StringComparison.OrdinalIgnoreCase))
                    return tab;
            }

            return null;
        }

        private static float ClampZoom(float value)
        {
            if (value < 0.2f) return 0.2f;
            if (value > 5.0f) return 5.0f;
            return value;
        }

        private static string GetDisplayName(EditorTabContext ctx)
        {
            return string.IsNullOrWhiteSpace(ctx.FilePath)
                ? $"Untitled {ctx.UntitledNumber}"
                : Path.GetFileName(ctx.FilePath);
        }

        private void UpdateTabTitle(TabPage tab)
        {
            if (tab.Tag is not EditorTabContext ctx)
                return;

            string baseName = GetDisplayName(ctx);
            tab.Text = ctx.IsModified ? $"{baseName} *" : baseName;
            tab.ToolTipText = string.IsNullOrWhiteSpace(ctx.FilePath) ? "(unsaved)" : ctx.FilePath;
        }

        private void tabEditors_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateUiState();
            ActiveEditor?.Focus();
        }

        private void tabEditors_MouseUp(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabEditors.TabPages.Count; i++)
            {
                Rectangle r = tabEditors.GetTabRect(i);
                if (!r.Contains(e.Location))
                    continue;

                tabEditors.SelectedIndex = i;

                if (e.Button == MouseButtons.Middle)
                {
                    _ = CloseTab(tabEditors.TabPages[i], askForSave: true, createFallbackTabIfNeeded: true);
                }

                break;
            }
        }

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (sender is not RichTextBox editor)
                return;

            TabPage? tab = FindTabForEditor(editor);
            if (tab == null || tab.Tag is not EditorTabContext ctx)
                return;

            if (!ctx.IsLoading)
            {
                ctx.IsModified = true;
                UpdateTabTitle(tab);
            }

            UpdateUiState();
        }

        private void Editor_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateUiState();
        }

        // -------------------------
        // UI state + status bar
        // -------------------------
        private void UpdateUiState()
        {
            MarkdownGdiEditor? editor = ActiveEditor;
            EditorTabContext? ctx = ActiveContext;

            bool hasEditor = editor != null;
            
            bool hasAnyTab = tabEditors.TabPages.Count > 0;

            mnuSave.Enabled = hasAnyTab;
            mnuSaveAs.Enabled = hasAnyTab;
            mnuCloseTab.Enabled = hasAnyTab;
            mnuPageSetup.Enabled = hasEditor;
            mnuPrint.Enabled = hasEditor;

            mnuUndo.Enabled = hasEditor;
            mnuRedo.Enabled = hasEditor;

            mnuPaste.Enabled = hasEditor;

            mnuFind.Enabled = hasEditor;
            mnuFindNext.Enabled = hasEditor && !string.IsNullOrWhiteSpace(_lastFindText);
            mnuReplace.Enabled = hasEditor;
            mnuGoTo.Enabled = hasEditor && !mnuWordWrap.Checked;
            mnuSelectAll.Enabled = hasEditor;
            mnuTimeDate.Enabled = hasEditor;

            mnuFont.Enabled = hasEditor;
            mnuZoomIn.Enabled = hasEditor;
            mnuZoomOut.Enabled = hasEditor;
            mnuZoomReset.Enabled = hasEditor;

            ApplyStatusBarVisibility();

            if (!hasEditor || editor == null)
            {
                lblPosition.Text = "Ln -, Col -";
                lblEncoding.Text = "-";
                lblZoom.Text = "100%";
                return;
            }

        }

        private void ApplyStatusBarVisibility()
        {
            // Like classic Notepad: status bar is hidden when Word Wrap is on.
            statusMain.Visible = mnuStatusBar.Checked && !mnuWordWrap.Checked;
        }

        // -------------------------
        // File menu
        // -------------------------
        private void mnuNewTab_Click(object? sender, EventArgs e)
        {
            CreateNewTabAndSelect();
        }

        private void mnuOpen_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Open",
                Filter = "Text/Markdown files|*.txt;*.md;*.log;*.json;*.xml;*.csv|All files|*.*",
                Multiselect = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return;

            bool reusedActiveTab = false;

            foreach (string file in ofd.FileNames)
            {
                try
                {
                    TabPage? existing = FindTabByFilePath(file);
                    if (existing != null)
                    {
                        tabEditors.SelectedTab = existing;
                        continue;
                    }

                    (string text, string enc) = ReadFileWithEncoding(file);

                    if (!reusedActiveTab && CanReuseActiveTabForOpen())
                    {
                        if (tabEditors.SelectedTab?.Tag is EditorTabContext ctx)
                        {
                            ctx.IsLoading = true;
                            ctx.Editor.Markdown = text;
                            ctx.FilePath = file;
                            ctx.EncodingName = enc;
                            ctx.IsModified = false;
                            ctx.IsLoading = false;
                            UpdateTabTitle(tabEditors.SelectedTab);
                            reusedActiveTab = true;
                            tabEditors.SelectedTab = tabEditors.SelectedTab;
                        }
                    }
                    else
                    {
                        TabPage newTab = CreateTab(
                            initialText: text,
                            filePath: file,
                            isModified: false,
                            untitledNumber: NextUntitledNumber(),
                            zoomFactor: 1f,
                            caretIndex: 0,
                            encodingName: enc);

                        tabEditors.SelectedTab = newTab;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        $"Failed to open file:\n{file}\n\n{ex.Message}",
                        "Open",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            UpdateUiState();
        }

        private bool CanReuseActiveTabForOpen()
        {
            if (tabEditors.SelectedTab?.Tag is not EditorTabContext ctx)
                return false;

            return string.IsNullOrWhiteSpace(ctx.FilePath)
                   && !ctx.IsModified
                   && ctx.Editor.Markdown.Length == 0;
        }

        private static (string Text, string EncodingName) ReadFileWithEncoding(string path)
        {
            using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string text = sr.ReadToEnd();

            string enc = sr.CurrentEncoding.WebName.ToUpperInvariant();
            if (enc == "UTF-8") enc = "UTF-8";

            return (text, enc);
        }

        private void mnuSave_Click(object? sender, EventArgs e)
        {
            if (tabEditors.SelectedTab == null)
                return;

            _ = SaveTab(tabEditors.SelectedTab, forceSaveAs: false);
        }

        private void mnuSaveAs_Click(object? sender, EventArgs e)
        {
            if (tabEditors.SelectedTab == null)
                return;

            _ = SaveTab(tabEditors.SelectedTab, forceSaveAs: true);
        }

        private bool SaveTab(TabPage tab, bool forceSaveAs)
        {
            if (tab.Tag is not EditorTabContext ctx)
                return false;

            string? targetPath = ctx.FilePath;

            if (forceSaveAs || string.IsNullOrWhiteSpace(targetPath))
            {
                using var sfd = new SaveFileDialog
                {
                    Title = "Save As",
                    Filter = "Text file (*.txt)|*.txt|Markdown (*.md)|*.md|All files (*.*)|*.*",
                    DefaultExt = "txt",
                    AddExtension = true,
                    FileName = string.IsNullOrWhiteSpace(ctx.FilePath)
                        ? $"{GetDisplayName(ctx)}.txt"
                        : Path.GetFileName(ctx.FilePath)
                };

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return false;

                targetPath = sfd.FileName;
            }

            try
            {
                File.WriteAllText(
                    targetPath!,
                    ctx.Editor.Text,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                ctx.FilePath = targetPath;
                ctx.EncodingName = "UTF-8";
                ctx.IsModified = false;
                UpdateTabTitle(tab);
                UpdateUiState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Failed to save file:\n{targetPath}\n\n{ex.Message}",
                    "Save",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void mnuPageSetup_Click(object? sender, EventArgs e)
        {
            _ = pageSetupDialog1.ShowDialog(this);
        }

        private void mnuPrint_Click(object? sender, EventArgs e)
        {
            MarkdownGdiEditor? editor = ActiveEditor;
            if (editor == null)
                return;

            _printBuffer = editor.Text;
            _printFont = editor.Font;

            if (printDialog1.ShowDialog(this) == DialogResult.OK)
            {
                printDocument1.Print();
            }
        }

        private void printDocument1_BeginPrint(object? sender, PrintEventArgs e)
        {
            _printCharIndex = 0;
        }

        private void printDocument1_PrintPage(object? sender, PrintPageEventArgs e)
        {
            if (string.IsNullOrEmpty(_printBuffer))
            {
                e.HasMorePages = false;
                return;
            }

            string remaining = _printBuffer.Substring(_printCharIndex);

            using var fmt = new StringFormat(StringFormatFlags.LineLimit);

            e.Graphics.MeasureString(
                remaining,
                _printFont,
                e.MarginBounds.Size,
                fmt,
                out int charsFitted,
                out _);

            if (charsFitted <= 0)
            {
                e.HasMorePages = false;
                return;
            }

            string chunk = remaining.Substring(0, charsFitted);

            e.Graphics.DrawString(
                chunk,
                _printFont,
                Brushes.Black,
                e.MarginBounds,
                fmt);

            _printCharIndex += charsFitted;
            e.HasMorePages = _printCharIndex < _printBuffer.Length;
        }

        private void mnuCloseTab_Click(object? sender, EventArgs e)
        {
            if (tabEditors.SelectedTab == null)
                return;

            _ = CloseTab(tabEditors.SelectedTab, askForSave: true, createFallbackTabIfNeeded: true);
        }

        private void mnuExit_Click(object? sender, EventArgs e)
        {
            // No save prompts here. Session is persisted on close.
            Close();
        }

        // -------------------------
        // Tab context menu
        // -------------------------
        private void ctxCloseTab_Click(object? sender, EventArgs e)
        {
            if (tabEditors.SelectedTab == null)
                return;

            _ = CloseTab(tabEditors.SelectedTab, askForSave: true, createFallbackTabIfNeeded: true);
        }

        private void ctxCloseOtherTabs_Click(object? sender, EventArgs e)
        {
            if (tabEditors.SelectedTab == null)
                return;

            TabPage selected = tabEditors.SelectedTab;
            List<TabPage> toClose = tabEditors.TabPages.Cast<TabPage>().Where(t => !ReferenceEquals(t, selected)).ToList();

            foreach (TabPage tab in toClose)
            {
                if (!CloseTab(tab, askForSave: true, createFallbackTabIfNeeded: false))
                    break;
            }

            if (tabEditors.TabPages.Count == 0)
            {
                CreateNewTabAndSelect();
            }
            else
            {
                tabEditors.SelectedTab = selected.IsDisposed ? tabEditors.TabPages[0] : selected;
            }

            UpdateUiState();
        }

        private void ctxCloseAllTabs_Click(object? sender, EventArgs e)
        {
            List<TabPage> toClose = tabEditors.TabPages.Cast<TabPage>().ToList();

            foreach (TabPage tab in toClose)
            {
                if (!CloseTab(tab, askForSave: true, createFallbackTabIfNeeded: false))
                    break;
            }

            if (tabEditors.TabPages.Count == 0)
            {
                CreateNewTabAndSelect();
            }

            UpdateUiState();
        }

        private bool CloseTab(TabPage tab, bool askForSave, bool createFallbackTabIfNeeded)
        {
            if (tab.Tag is not EditorTabContext ctx)
                return false;

            if (askForSave && ctx.IsModified)
            {
                string name = GetDisplayName(ctx);

                DialogResult dr = MessageBox.Show(
                    this,
                    $"Do you want to save changes to '{name}'?",
                    "MarkdownEditor",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Cancel)
                    return false;

                if (dr == DialogResult.Yes)
                {
                    bool saved = SaveTab(tab, forceSaveAs: string.IsNullOrWhiteSpace(ctx.FilePath));
                    if (!saved)
                        return false;
                }
            }

            ctx.Editor.TextChanged -= Editor_TextChanged;
            

            tabEditors.TabPages.Remove(tab);
            tab.Dispose();

            if (createFallbackTabIfNeeded && tabEditors.TabPages.Count == 0)
            {
                CreateNewTabAndSelect();
            }

            UpdateUiState();
            return true;
        }

        // -------------------------
        // Edit menu
        // -------------------------



        private void mnuCut_Click(object? sender, EventArgs e)
        {
            ActiveEditor?.CutCommand();
        }

        private void mnuCopy_Click(object? sender, EventArgs e)
        {
            ActiveEditor?.CopyCommand();
        }

        private void mnuPaste_Click(object? sender, EventArgs e)
        {
            ActiveEditor?.PasteCommand();
        }

       

        
        

        
        
        private void mnuSelectAll_Click(object? sender, EventArgs e)
        {
            ActiveEditor?.SelectAllCommand();
        }

        // -------------------------
        // Format / View
        // -------------------------
        private void mnuWordWrap_Click(object? sender, EventArgs e)
        {
            
            ApplyStatusBarVisibility();
            UpdateUiState();
        }

        private void mnuFont_Click(object? sender, EventArgs e)
        {
            using var fd = new FontDialog
            {
                Font = _editorFont,
                ShowColor = false
            };

            if (fd.ShowDialog(this) != DialogResult.OK)
                return;

            _editorFont = fd.Font;

            foreach (TabPage tab in tabEditors.TabPages)
            {
                if (tab.Tag is EditorTabContext ctx)
                {
                    ctx.Editor.Font = _editorFont;
                }
            }

            UpdateUiState();
        }


        private void mnuStatusBar_Click(object? sender, EventArgs e)
        {
            ApplyStatusBarVisibility();
            UpdateUiState();
        }

        // -------------------------
        // Help
        // -------------------------
        private void mnuAbout_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                this,
                "MarkdownEditor\nTab-based text editor with Notepad-like features.",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // -------------------------
        // Small input dialog helper
        // -------------------------
        private static bool ShowTextPrompt(
            IWin32Window owner,
            string title,
            string labelText,
            string initialValue,
            out string value)
        {
            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(430, 130)
            };

            var lbl = new Label
            {
                Left = 12,
                Top = 12,
                Width = 406,
                AutoSize = false,
                Text = labelText
            };

            var txt = new TextBox
            {
                Left = 12,
                Top = 34,
                Width = 406,
                Text = initialValue ?? string.Empty
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 262,
                Top = 74,
                Width = 75
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 343,
                Top = 74,
                Width = 75
            };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            if (form.ShowDialog(owner) == DialogResult.OK)
            {
                value = txt.Text;
                return true;
            }

            value = initialValue ?? string.Empty;
            return false;
        }

        // -------------------------
        // Session DTO / tab context
        // -------------------------
        private sealed class EditorTabContext
        {
            public required MarkdownGdiEditor Editor { get; init; }
            public string? FilePath { get; set; }
            public int UntitledNumber { get; set; }
            public bool IsModified { get; set; }
            public bool IsLoading { get; set; }
            public string EncodingName { get; set; } = "UTF-8";
        }

        private sealed class EditorSession
        {
            public List<EditorTabSession> Tabs { get; set; } = new();
            public int SelectedIndex { get; set; }
            public bool WordWrap { get; set; } = true;
            public bool ShowStatusBar { get; set; } = true;
            public string FontFamily { get; set; } = "Consolas";
            public float FontSize { get; set; } = 11f;
            public int FontStyleValue { get; set; } = (int)FontStyle.Regular;
        }

        private sealed class EditorTabSession
        {
            public string? FilePath { get; set; }
            public int UntitledNumber { get; set; }
            public string Content { get; set; } = string.Empty;
            public bool IsModified { get; set; }
            public int CaretIndex { get; set; }
            public float ZoomFactor { get; set; } = 1f;
            public string EncodingName { get; set; } = "UTF-8";
        }
    }
}
