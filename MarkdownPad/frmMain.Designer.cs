namespace MarkdownPad
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            printDialog = new PrintDialog();
            printPreviewDialog = new PrintPreviewDialog();
            printDocument = new System.Drawing.Printing.PrintDocument();
            tabContextMenuStrip = new ContextMenuStrip(components);
            closeContextTabToolStripMenuItem = new ToolStripMenuItem();
            closeOtherContextTabsToolStripMenuItem = new ToolStripMenuItem();
            closeAllContextTabsToolStripMenuItem = new ToolStripMenuItem();
            padMenu = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripSeparator1 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            saveAllToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripSeparator2 = new ToolStripSeparator();
            closeTabToolStripMenuItem = new ToolStripMenuItem();
            closeAllTabsToolStripMenuItem = new ToolStripMenuItem();
            closeOtherTabsToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripSeparator3 = new ToolStripSeparator();
            printToolStripMenuItem = new ToolStripMenuItem();
            printPreviewToolStripMenuItem = new ToolStripMenuItem();
            fileToolStripSeparator4 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            redoToolStripMenuItem = new ToolStripMenuItem();
            editToolStripSeparator1 = new ToolStripSeparator();
            cutToolStripMenuItem = new ToolStripMenuItem();
            copyToolStripMenuItem = new ToolStripMenuItem();
            pasteToolStripMenuItem = new ToolStripMenuItem();
            editToolStripSeparator2 = new ToolStripSeparator();
            selectAllToolStripMenuItem = new ToolStripMenuItem();
            searchToolStripMenuItem = new ToolStripMenuItem();
            findToolStripMenuItem = new ToolStripMenuItem();
            findNextToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            themeSystemToolStripMenuItem = new ToolStripMenuItem();
            themeLightToolStripMenuItem = new ToolStripMenuItem();
            themeDarkToolStripMenuItem = new ToolStripMenuItem();
            padToolStrip = new ToolStrip();
            newToolStripButton = new ToolStripButton();
            openToolStripButton = new ToolStripButton();
            saveToolStripButton = new ToolStripButton();
            saveAllToolStripButton = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            undoToolStripButton = new ToolStripButton();
            redoToolStripButton = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            findToolStripButton = new ToolStripButton();
            findNextToolStripButton = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            themeToolStripDropDownButton = new ToolStripDropDownButton();
            themeSystemToolStripDropDownItem = new ToolStripMenuItem();
            themeLightToolStripDropDownItem = new ToolStripMenuItem();
            themeDarkToolStripDropDownItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            padStatusToolStrip = new ToolStrip();
            documentStatusLabel = new ToolStripLabel();
            toolStripSeparator4 = new ToolStripSeparator();
            pathStatusLabel = new ToolStripLabel();
            toolStripSeparator5 = new ToolStripSeparator();
            positionStatusLabel = new ToolStripLabel();
            toolStripSeparator6 = new ToolStripSeparator();
            themeStatusLabel = new ToolStripLabel();
            toolStripSeparator7 = new ToolStripSeparator();
            messageStatusLabel = new ToolStripLabel();
            tabContextMenuStrip.SuspendLayout();
            padMenu.SuspendLayout();
            padToolStrip.SuspendLayout();
            padStatusToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = "Markdown files (*.md;*.markdown;*.mdown;*.txt)|*.md;*.markdown;*.mdown;*.txt|All files (*.*)|*.*";
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Open Markdown Files";
            // 
            // saveFileDialog
            // 
            saveFileDialog.AddExtension = true;
            saveFileDialog.DefaultExt = "md";
            saveFileDialog.Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Title = "Save Markdown File";
            // 
            // printDialog
            // 
            printDialog.UseEXDialog = true;
            // 
            // printPreviewDialog
            // 
            printPreviewDialog.AutoScrollMargin = new Size(0, 0);
            printPreviewDialog.AutoScrollMinSize = new Size(0, 0);
            printPreviewDialog.ClientSize = new Size(1000, 700);
            printPreviewDialog.Enabled = true;
            printPreviewDialog.Name = "printPreviewDialog";
            printPreviewDialog.UseAntiAlias = true;
            printPreviewDialog.Visible = false;
            // 
            // tabContextMenuStrip
            // 
            tabContextMenuStrip.ImageScalingSize = new Size(20, 20);
            tabContextMenuStrip.Items.AddRange(new ToolStripItem[] { closeContextTabToolStripMenuItem, closeOtherContextTabsToolStripMenuItem, closeAllContextTabsToolStripMenuItem });
            tabContextMenuStrip.Name = "tabContextMenuStrip";
            tabContextMenuStrip.Size = new Size(191, 76);
            // 
            // closeContextTabToolStripMenuItem
            // 
            closeContextTabToolStripMenuItem.Name = "closeContextTabToolStripMenuItem";
            closeContextTabToolStripMenuItem.Size = new Size(190, 24);
            closeContextTabToolStripMenuItem.Text = "Close";
            // 
            // closeOtherContextTabsToolStripMenuItem
            // 
            closeOtherContextTabsToolStripMenuItem.Name = "closeOtherContextTabsToolStripMenuItem";
            closeOtherContextTabsToolStripMenuItem.Size = new Size(190, 24);
            closeOtherContextTabsToolStripMenuItem.Text = "Close Others";
            // 
            // closeAllContextTabsToolStripMenuItem
            // 
            closeAllContextTabsToolStripMenuItem.Name = "closeAllContextTabsToolStripMenuItem";
            closeAllContextTabsToolStripMenuItem.Size = new Size(190, 24);
            closeAllContextTabsToolStripMenuItem.Text = "Close All";
            // 
            // padMenu
            // 
            padMenu.ImageScalingSize = new Size(20, 20);
            padMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, searchToolStripMenuItem, viewToolStripMenuItem });
            padMenu.Location = new Point(0, 0);
            padMenu.Name = "padMenu";
            padMenu.Size = new Size(1215, 28);
            padMenu.TabIndex = 0;
            padMenu.Text = "padMenu";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, fileToolStripSeparator1, saveToolStripMenuItem, saveAsToolStripMenuItem, saveAllToolStripMenuItem, fileToolStripSeparator2, closeTabToolStripMenuItem, closeAllTabsToolStripMenuItem, closeOtherTabsToolStripMenuItem, fileToolStripSeparator3, printToolStripMenuItem, printPreviewToolStripMenuItem, fileToolStripSeparator4, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(59, 24);
            fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newToolStripMenuItem.Size = new Size(253, 26);
            newToolStripMenuItem.Text = "&New";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(253, 26);
            openToolStripMenuItem.Text = "&Open";
            // 
            // fileToolStripSeparator1
            // 
            fileToolStripSeparator1.Name = "fileToolStripSeparator1";
            fileToolStripSeparator1.Size = new Size(250, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveToolStripMenuItem.Size = new Size(253, 26);
            saveToolStripMenuItem.Text = "&Save";
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(253, 26);
            saveAsToolStripMenuItem.Text = "Save &As";
            // 
            // saveAllToolStripMenuItem
            // 
            saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            saveAllToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveAllToolStripMenuItem.Size = new Size(253, 26);
            saveAllToolStripMenuItem.Text = "Save A&ll";
            // 
            // fileToolStripSeparator2
            // 
            fileToolStripSeparator2.Name = "fileToolStripSeparator2";
            fileToolStripSeparator2.Size = new Size(250, 6);
            // 
            // closeTabToolStripMenuItem
            // 
            closeTabToolStripMenuItem.Name = "closeTabToolStripMenuItem";
            closeTabToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.W;
            closeTabToolStripMenuItem.Size = new Size(253, 26);
            closeTabToolStripMenuItem.Text = "Close";
            // 
            // closeAllTabsToolStripMenuItem
            // 
            closeAllTabsToolStripMenuItem.Name = "closeAllTabsToolStripMenuItem";
            closeAllTabsToolStripMenuItem.Size = new Size(253, 26);
            closeAllTabsToolStripMenuItem.Text = "Close All";
            // 
            // closeOtherTabsToolStripMenuItem
            // 
            closeOtherTabsToolStripMenuItem.Name = "closeOtherTabsToolStripMenuItem";
            closeOtherTabsToolStripMenuItem.Size = new Size(253, 26);
            closeOtherTabsToolStripMenuItem.Text = "Close Others";
            // 
            // fileToolStripSeparator3
            // 
            fileToolStripSeparator3.Name = "fileToolStripSeparator3";
            fileToolStripSeparator3.Size = new Size(250, 6);
            // 
            // printToolStripMenuItem
            // 
            printToolStripMenuItem.Name = "printToolStripMenuItem";
            printToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.P;
            printToolStripMenuItem.Size = new Size(253, 26);
            printToolStripMenuItem.Text = "&Print";
            // 
            // printPreviewToolStripMenuItem
            // 
            printPreviewToolStripMenuItem.Name = "printPreviewToolStripMenuItem";
            printPreviewToolStripMenuItem.Size = new Size(253, 26);
            printPreviewToolStripMenuItem.Text = "Print Pre&view";
            // 
            // fileToolStripSeparator4
            // 
            fileToolStripSeparator4.Name = "fileToolStripSeparator4";
            fileToolStripSeparator4.Size = new Size(250, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(253, 26);
            exitToolStripMenuItem.Text = "E&xit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoToolStripMenuItem, redoToolStripMenuItem, editToolStripSeparator1, cutToolStripMenuItem, copyToolStripMenuItem, pasteToolStripMenuItem, editToolStripSeparator2, selectAllToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(95, 24);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            undoToolStripMenuItem.Size = new Size(237, 26);
            undoToolStripMenuItem.Text = "Undo";
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            redoToolStripMenuItem.Size = new Size(237, 26);
            redoToolStripMenuItem.Text = "Redo";
            // 
            // editToolStripSeparator1
            // 
            editToolStripSeparator1.Name = "editToolStripSeparator1";
            editToolStripSeparator1.Size = new Size(234, 6);
            // 
            // cutToolStripMenuItem
            // 
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            cutToolStripMenuItem.Size = new Size(237, 26);
            cutToolStripMenuItem.Text = "Cut";
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            copyToolStripMenuItem.Size = new Size(237, 26);
            copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteToolStripMenuItem
            // 
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            pasteToolStripMenuItem.Size = new Size(237, 26);
            pasteToolStripMenuItem.Text = "Paste";
            // 
            // editToolStripSeparator2
            // 
            editToolStripSeparator2.Name = "editToolStripSeparator2";
            editToolStripSeparator2.Size = new Size(234, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            selectAllToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            selectAllToolStripMenuItem.Size = new Size(237, 26);
            selectAllToolStripMenuItem.Text = "Select All";
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { findToolStripMenuItem, findNextToolStripMenuItem });
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.Size = new Size(70, 24);
            searchToolStripMenuItem.Text = "&Search";
            // 
            // findToolStripMenuItem
            // 
            findToolStripMenuItem.Name = "findToolStripMenuItem";
            findToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            findToolStripMenuItem.Size = new Size(194, 26);
            findToolStripMenuItem.Text = "Find...";
            // 
            // findNextToolStripMenuItem
            // 
            findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
            findNextToolStripMenuItem.ShortcutKeys = Keys.F3;
            findNextToolStripMenuItem.Size = new Size(194, 26);
            findNextToolStripMenuItem.Text = "Find Next";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { themeSystemToolStripMenuItem, themeLightToolStripMenuItem, themeDarkToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(69, 24);
            viewToolStripMenuItem.Text = "&View";
            // 
            // themeSystemToolStripMenuItem
            // 
            themeSystemToolStripMenuItem.Name = "themeSystemToolStripMenuItem";
            themeSystemToolStripMenuItem.Size = new Size(176, 26);
            themeSystemToolStripMenuItem.Text = "System Theme";
            // 
            // themeLightToolStripMenuItem
            // 
            themeLightToolStripMenuItem.Name = "themeLightToolStripMenuItem";
            themeLightToolStripMenuItem.Size = new Size(176, 26);
            themeLightToolStripMenuItem.Text = "Light";
            // 
            // themeDarkToolStripMenuItem
            // 
            themeDarkToolStripMenuItem.Name = "themeDarkToolStripMenuItem";
            themeDarkToolStripMenuItem.Size = new Size(176, 26);
            themeDarkToolStripMenuItem.Text = "Dark";
            // 
            // padToolStrip
            // 
            padToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padToolStrip.ImageScalingSize = new Size(20, 20);
            padToolStrip.Items.AddRange(new ToolStripItem[] { newToolStripButton, openToolStripButton, saveToolStripButton, saveAllToolStripButton, toolStripSeparator1, undoToolStripButton, redoToolStripButton, toolStripSeparator2, findToolStripButton, findNextToolStripButton, toolStripSeparator3, themeToolStripDropDownButton });
            padToolStrip.Location = new Point(0, 28);
            padToolStrip.Name = "padToolStrip";
            padToolStrip.Size = new Size(1215, 31);
            padToolStrip.TabIndex = 1;
            padToolStrip.Text = "padToolStrip";
            // 
            // newToolStripButton
            // 
            newToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            newToolStripButton.Name = "newToolStripButton";
            newToolStripButton.Size = new Size(38, 28);
            newToolStripButton.Text = "New";
            // 
            // openToolStripButton
            // 
            openToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            openToolStripButton.Name = "openToolStripButton";
            openToolStripButton.Size = new Size(61, 28);
            openToolStripButton.Text = "Open";
            // 
            // saveToolStripButton
            // 
            saveToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            saveToolStripButton.Name = "saveToolStripButton";
            saveToolStripButton.Size = new Size(75, 28);
            saveToolStripButton.Text = "Save";
            // 
            // saveAllToolStripButton
            // 
            saveAllToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            saveAllToolStripButton.Name = "saveAllToolStripButton";
            saveAllToolStripButton.Size = new Size(103, 28);
            saveAllToolStripButton.Text = "Save All";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 31);
            // 
            // undoToolStripButton
            // 
            undoToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            undoToolStripButton.Name = "undoToolStripButton";
            undoToolStripButton.Size = new Size(84, 28);
            undoToolStripButton.Text = "Undo";
            // 
            // redoToolStripButton
            // 
            redoToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            redoToolStripButton.Name = "redoToolStripButton";
            redoToolStripButton.Size = new Size(89, 28);
            redoToolStripButton.Text = "Redo";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 31);
            // 
            // findToolStripButton
            // 
            findToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            findToolStripButton.Name = "findToolStripButton";
            findToolStripButton.Size = new Size(58, 28);
            findToolStripButton.Text = "Find";
            // 
            // findNextToolStripButton
            // 
            findNextToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            findNextToolStripButton.Name = "findNextToolStripButton";
            findNextToolStripButton.Size = new Size(52, 28);
            findNextToolStripButton.Text = "Next";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 31);
            // 
            // themeToolStripDropDownButton
            // 
            themeToolStripDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            themeToolStripDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { themeSystemToolStripDropDownItem, themeLightToolStripDropDownItem, themeDarkToolStripDropDownItem });
            themeToolStripDropDownButton.Name = "themeToolStripDropDownButton";
            themeToolStripDropDownButton.Size = new Size(73, 28);
            themeToolStripDropDownButton.Text = "Theme";
            // 
            // themeSystemToolStripDropDownItem
            // 
            themeSystemToolStripDropDownItem.Name = "themeSystemToolStripDropDownItem";
            themeSystemToolStripDropDownItem.Size = new Size(176, 26);
            themeSystemToolStripDropDownItem.Text = "System Theme";
            // 
            // themeLightToolStripDropDownItem
            // 
            themeLightToolStripDropDownItem.Name = "themeLightToolStripDropDownItem";
            themeLightToolStripDropDownItem.Size = new Size(176, 26);
            themeLightToolStripDropDownItem.Text = "Light";
            // 
            // themeDarkToolStripDropDownItem
            // 
            themeDarkToolStripDropDownItem.Name = "themeDarkToolStripDropDownItem";
            themeDarkToolStripDropDownItem.Size = new Size(176, 26);
            themeDarkToolStripDropDownItem.Text = "Dark";
            // 
            // tabControl1
            // 
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.HotTrack = true;
            tabControl1.Location = new Point(0, 59);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.ShowToolTips = true;
            tabControl1.Size = new Size(1215, 672);
            tabControl1.TabIndex = 2;
            // 
            // padStatusToolStrip
            // 
            padStatusToolStrip.Dock = DockStyle.Bottom;
            padStatusToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padStatusToolStrip.ImageScalingSize = new Size(20, 20);
            padStatusToolStrip.Items.AddRange(new ToolStripItem[] { documentStatusLabel, toolStripSeparator4, pathStatusLabel, toolStripSeparator5, positionStatusLabel, toolStripSeparator6, themeStatusLabel, toolStripSeparator7, messageStatusLabel });
            padStatusToolStrip.Location = new Point(0, 731);
            padStatusToolStrip.Name = "padStatusToolStrip";
            padStatusToolStrip.Size = new Size(1215, 31);
            padStatusToolStrip.TabIndex = 3;
            padStatusToolStrip.Text = "padStatusToolStrip";
            // 
            // documentStatusLabel
            // 
            documentStatusLabel.Name = "documentStatusLabel";
            documentStatusLabel.Size = new Size(87, 28);
            documentStatusLabel.Text = "Document: -";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 31);
            // 
            // pathStatusLabel
            // 
            pathStatusLabel.Name = "pathStatusLabel";
            pathStatusLabel.Size = new Size(154, 28);
            pathStatusLabel.Text = "Path: Unsaved";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 31);
            // 
            // positionStatusLabel
            // 
            positionStatusLabel.Name = "positionStatusLabel";
            positionStatusLabel.Size = new Size(72, 28);
            positionStatusLabel.Text = "Position: -";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 31);
            // 
            // themeStatusLabel
            // 
            themeStatusLabel.Name = "themeStatusLabel";
            themeStatusLabel.Size = new Size(95, 28);
            themeStatusLabel.Text = "Theme: System";
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(6, 31);
            // 
            // messageStatusLabel
            // 
            messageStatusLabel.Name = "messageStatusLabel";
            messageStatusLabel.Size = new Size(48, 28);
            messageStatusLabel.Text = "Ready";
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1215, 762);
            Controls.Add(tabControl1);
            Controls.Add(padToolStrip);
            Controls.Add(padMenu);
            Controls.Add(padStatusToolStrip);
            MainMenuStrip = padMenu;
            Name = "frmMain";
            Text = "MarkdownPad";
            tabContextMenuStrip.ResumeLayout(false);
            padMenu.ResumeLayout(false);
            padMenu.PerformLayout();
            padToolStrip.ResumeLayout(false);
            padToolStrip.PerformLayout();
            padStatusToolStrip.ResumeLayout(false);
            padStatusToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private PrintDialog printDialog;
        private PrintPreviewDialog printPreviewDialog;
        private System.Drawing.Printing.PrintDocument printDocument;
        private ContextMenuStrip tabContextMenuStrip;
        private ToolStripMenuItem closeContextTabToolStripMenuItem;
        private ToolStripMenuItem closeOtherContextTabsToolStripMenuItem;
        private ToolStripMenuItem closeAllContextTabsToolStripMenuItem;
        private MenuStrip padMenu;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripSeparator fileToolStripSeparator1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem saveAllToolStripMenuItem;
        private ToolStripSeparator fileToolStripSeparator2;
        private ToolStripMenuItem closeTabToolStripMenuItem;
        private ToolStripMenuItem closeAllTabsToolStripMenuItem;
        private ToolStripMenuItem closeOtherTabsToolStripMenuItem;
        private ToolStripSeparator fileToolStripSeparator3;
        private ToolStripMenuItem printToolStripMenuItem;
        private ToolStripMenuItem printPreviewToolStripMenuItem;
        private ToolStripSeparator fileToolStripSeparator4;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripSeparator editToolStripSeparator1;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripSeparator editToolStripSeparator2;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private ToolStripMenuItem searchToolStripMenuItem;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem findNextToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem themeSystemToolStripMenuItem;
        private ToolStripMenuItem themeLightToolStripMenuItem;
        private ToolStripMenuItem themeDarkToolStripMenuItem;
        private ToolStrip padToolStrip;
        private ToolStripButton newToolStripButton;
        private ToolStripButton openToolStripButton;
        private ToolStripButton saveToolStripButton;
        private ToolStripButton saveAllToolStripButton;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton undoToolStripButton;
        private ToolStripButton redoToolStripButton;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton findToolStripButton;
        private ToolStripButton findNextToolStripButton;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripDropDownButton themeToolStripDropDownButton;
        private ToolStripMenuItem themeSystemToolStripDropDownItem;
        private ToolStripMenuItem themeLightToolStripDropDownItem;
        private ToolStripMenuItem themeDarkToolStripDropDownItem;
        private TabControl tabControl1;
        private ToolStrip padStatusToolStrip;
        private ToolStripLabel documentStatusLabel;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripLabel pathStatusLabel;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripLabel positionStatusLabel;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripLabel themeStatusLabel;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripLabel messageStatusLabel;
    }
}
