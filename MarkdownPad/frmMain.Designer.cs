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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
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
            formatToolStripMenuItem = new ToolStripMenuItem();
            insertLinkToolStripMenuItem = new ToolStripMenuItem();
            insertImageToolStripMenuItem = new ToolStripMenuItem();
            formatToolStripSeparator0 = new ToolStripSeparator();
            tableDesignerToolStripMenuItem = new ToolStripMenuItem();
            formatToolStripSeparator1 = new ToolStripSeparator();
            headingToolStripMenuItem = new ToolStripMenuItem();
            heading1ToolStripMenuItem = new ToolStripMenuItem();
            heading2ToolStripMenuItem = new ToolStripMenuItem();
            heading3ToolStripMenuItem = new ToolStripMenuItem();
            heading4ToolStripMenuItem = new ToolStripMenuItem();
            heading5ToolStripMenuItem = new ToolStripMenuItem();
            heading6ToolStripMenuItem = new ToolStripMenuItem();
            formatToolStripSeparator2 = new ToolStripSeparator();
            quoteToolStripMenuItem = new ToolStripMenuItem();
            codeFenceToolStripMenuItem = new ToolStripMenuItem();
            searchToolStripMenuItem = new ToolStripMenuItem();
            findToolStripMenuItem = new ToolStripMenuItem();
            findNextToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
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
            linkToolStripButton = new ToolStripButton();
            imageToolStripButton = new ToolStripButton();
            toolStripSeparator9 = new ToolStripSeparator();
            tableToolStripButton = new ToolStripButton();
            headingToolStripDropDownButton = new ToolStripDropDownButton();
            heading1ToolStripDropDownItem = new ToolStripMenuItem();
            heading2ToolStripDropDownItem = new ToolStripMenuItem();
            heading3ToolStripDropDownItem = new ToolStripMenuItem();
            heading4ToolStripDropDownItem = new ToolStripMenuItem();
            heading5ToolStripDropDownItem = new ToolStripMenuItem();
            heading6ToolStripDropDownItem = new ToolStripMenuItem();
            quoteToolStripButton = new ToolStripButton();
            codeFenceToolStripButton = new ToolStripButton();
            toolStripSeparator8 = new ToolStripSeparator();
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
            saveFileDialog.DefaultExt = "md";
            saveFileDialog.Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*";
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
            printPreviewDialog.Icon = (Icon)resources.GetObject("printPreviewDialog.Icon");
            printPreviewDialog.Name = "printPreviewDialog";
            printPreviewDialog.UseAntiAlias = true;
            printPreviewDialog.Visible = false;
            // 
            // tabContextMenuStrip
            // 
            tabContextMenuStrip.ImageScalingSize = new Size(20, 20);
            tabContextMenuStrip.Items.AddRange(new ToolStripItem[] { closeContextTabToolStripMenuItem, closeOtherContextTabsToolStripMenuItem, closeAllContextTabsToolStripMenuItem });
            tabContextMenuStrip.Name = "tabContextMenuStrip";
            tabContextMenuStrip.Size = new Size(142, 70);
            // 
            // closeContextTabToolStripMenuItem
            // 
            closeContextTabToolStripMenuItem.Name = "closeContextTabToolStripMenuItem";
            closeContextTabToolStripMenuItem.Size = new Size(141, 22);
            closeContextTabToolStripMenuItem.Text = "Close";
            // 
            // closeOtherContextTabsToolStripMenuItem
            // 
            closeOtherContextTabsToolStripMenuItem.Name = "closeOtherContextTabsToolStripMenuItem";
            closeOtherContextTabsToolStripMenuItem.Size = new Size(141, 22);
            closeOtherContextTabsToolStripMenuItem.Text = "Close Others";
            // 
            // closeAllContextTabsToolStripMenuItem
            // 
            closeAllContextTabsToolStripMenuItem.Name = "closeAllContextTabsToolStripMenuItem";
            closeAllContextTabsToolStripMenuItem.Size = new Size(141, 22);
            closeAllContextTabsToolStripMenuItem.Text = "Close All";
            // 
            // padMenu
            // 
            padMenu.ImageScalingSize = new Size(20, 20);
            padMenu.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, formatToolStripMenuItem, searchToolStripMenuItem, helpToolStripMenuItem });
            padMenu.Location = new Point(0, 0);
            padMenu.Name = "padMenu";
            padMenu.Padding = new Padding(5, 2, 0, 2);
            padMenu.Size = new Size(1063, 24);
            padMenu.TabIndex = 0;
            padMenu.Text = "padMenu";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, fileToolStripSeparator1, saveToolStripMenuItem, saveAsToolStripMenuItem, saveAllToolStripMenuItem, fileToolStripSeparator2, closeTabToolStripMenuItem, closeAllTabsToolStripMenuItem, closeOtherTabsToolStripMenuItem, fileToolStripSeparator3, printToolStripMenuItem, printPreviewToolStripMenuItem, fileToolStripSeparator4, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Image = (Image)resources.GetObject("newToolStripMenuItem.Image");
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newToolStripMenuItem.Size = new Size(240, 22);
            newToolStripMenuItem.Text = "&New";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Image = (Image)resources.GetObject("openToolStripMenuItem.Image");
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(240, 22);
            openToolStripMenuItem.Text = "&Open";
            // 
            // fileToolStripSeparator1
            // 
            fileToolStripSeparator1.Name = "fileToolStripSeparator1";
            fileToolStripSeparator1.Size = new Size(237, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = (Image)resources.GetObject("saveToolStripMenuItem.Image");
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveToolStripMenuItem.Size = new Size(240, 22);
            saveToolStripMenuItem.Text = "&Save";
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(240, 22);
            saveAsToolStripMenuItem.Text = "Save &As";
            // 
            // saveAllToolStripMenuItem
            // 
            saveAllToolStripMenuItem.Image = (Image)resources.GetObject("saveAllToolStripMenuItem.Image");
            saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            saveAllToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveAllToolStripMenuItem.Size = new Size(240, 22);
            saveAllToolStripMenuItem.Text = "Save A&ll";
            // 
            // fileToolStripSeparator2
            // 
            fileToolStripSeparator2.Name = "fileToolStripSeparator2";
            fileToolStripSeparator2.Size = new Size(237, 6);
            // 
            // closeTabToolStripMenuItem
            // 
            closeTabToolStripMenuItem.Image = (Image)resources.GetObject("closeTabToolStripMenuItem.Image");
            closeTabToolStripMenuItem.Name = "closeTabToolStripMenuItem";
            closeTabToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.W;
            closeTabToolStripMenuItem.Size = new Size(240, 22);
            closeTabToolStripMenuItem.Text = "Close";
            // 
            // closeAllTabsToolStripMenuItem
            // 
            closeAllTabsToolStripMenuItem.Name = "closeAllTabsToolStripMenuItem";
            closeAllTabsToolStripMenuItem.Size = new Size(240, 22);
            closeAllTabsToolStripMenuItem.Text = "Close All";
            // 
            // closeOtherTabsToolStripMenuItem
            // 
            closeOtherTabsToolStripMenuItem.Name = "closeOtherTabsToolStripMenuItem";
            closeOtherTabsToolStripMenuItem.Size = new Size(240, 22);
            closeOtherTabsToolStripMenuItem.Text = "Close Others";
            // 
            // fileToolStripSeparator3
            // 
            fileToolStripSeparator3.Name = "fileToolStripSeparator3";
            fileToolStripSeparator3.Size = new Size(237, 6);
            // 
            // printToolStripMenuItem
            // 
            printToolStripMenuItem.Image = (Image)resources.GetObject("printToolStripMenuItem.Image");
            printToolStripMenuItem.Name = "printToolStripMenuItem";
            printToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.P;
            printToolStripMenuItem.Size = new Size(240, 22);
            printToolStripMenuItem.Text = "&Print";
            // 
            // printPreviewToolStripMenuItem
            // 
            printPreviewToolStripMenuItem.Image = (Image)resources.GetObject("printPreviewToolStripMenuItem.Image");
            printPreviewToolStripMenuItem.Name = "printPreviewToolStripMenuItem";
            printPreviewToolStripMenuItem.Size = new Size(240, 22);
            printPreviewToolStripMenuItem.Text = "Print Pre&view";
            // 
            // fileToolStripSeparator4
            // 
            fileToolStripSeparator4.Name = "fileToolStripSeparator4";
            fileToolStripSeparator4.Size = new Size(237, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Image = (Image)resources.GetObject("exitToolStripMenuItem.Image");
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(240, 22);
            exitToolStripMenuItem.Text = "E&xit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoToolStripMenuItem, redoToolStripMenuItem, editToolStripSeparator1, cutToolStripMenuItem, copyToolStripMenuItem, pasteToolStripMenuItem, editToolStripSeparator2, selectAllToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Image = (Image)resources.GetObject("undoToolStripMenuItem.Image");
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            undoToolStripMenuItem.Size = new Size(166, 22);
            undoToolStripMenuItem.Text = "Undo";
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Image = (Image)resources.GetObject("redoToolStripMenuItem.Image");
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            redoToolStripMenuItem.Size = new Size(166, 22);
            redoToolStripMenuItem.Text = "Redo";
            // 
            // editToolStripSeparator1
            // 
            editToolStripSeparator1.Name = "editToolStripSeparator1";
            editToolStripSeparator1.Size = new Size(163, 6);
            // 
            // cutToolStripMenuItem
            // 
            cutToolStripMenuItem.Image = (Image)resources.GetObject("cutToolStripMenuItem.Image");
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            cutToolStripMenuItem.Size = new Size(166, 22);
            cutToolStripMenuItem.Text = "Cut";
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Image = (Image)resources.GetObject("copyToolStripMenuItem.Image");
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            copyToolStripMenuItem.Size = new Size(166, 22);
            copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteToolStripMenuItem
            // 
            pasteToolStripMenuItem.Image = (Image)resources.GetObject("pasteToolStripMenuItem.Image");
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            pasteToolStripMenuItem.Size = new Size(166, 22);
            pasteToolStripMenuItem.Text = "Paste";
            // 
            // editToolStripSeparator2
            // 
            editToolStripSeparator2.Name = "editToolStripSeparator2";
            editToolStripSeparator2.Size = new Size(163, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Image = (Image)resources.GetObject("selectAllToolStripMenuItem.Image");
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            selectAllToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            selectAllToolStripMenuItem.Size = new Size(166, 22);
            selectAllToolStripMenuItem.Text = "Select All";
            // 
            // formatToolStripMenuItem
            // 
            formatToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { insertLinkToolStripMenuItem, insertImageToolStripMenuItem, formatToolStripSeparator0, tableDesignerToolStripMenuItem, formatToolStripSeparator1, headingToolStripMenuItem, formatToolStripSeparator2, quoteToolStripMenuItem, codeFenceToolStripMenuItem });
            formatToolStripMenuItem.Name = "formatToolStripMenuItem";
            formatToolStripMenuItem.Size = new Size(57, 20);
            formatToolStripMenuItem.Text = "F&ormat";
            // 
            // insertLinkToolStripMenuItem
            // 
            insertLinkToolStripMenuItem.Name = "insertLinkToolStripMenuItem";
            insertLinkToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.K;
            insertLinkToolStripMenuItem.Size = new Size(270, 22);
            insertLinkToolStripMenuItem.Text = "Insert Link...";
            // 
            // insertImageToolStripMenuItem
            // 
            insertImageToolStripMenuItem.Name = "insertImageToolStripMenuItem";
            insertImageToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.I;
            insertImageToolStripMenuItem.Size = new Size(270, 22);
            insertImageToolStripMenuItem.Text = "Insert Image...";
            // 
            // formatToolStripSeparator0
            // 
            formatToolStripSeparator0.Name = "formatToolStripSeparator0";
            formatToolStripSeparator0.Size = new Size(267, 6);
            // 
            // tableDesignerToolStripMenuItem
            // 
            tableDesignerToolStripMenuItem.Image = (Image)resources.GetObject("tableDesignerToolStripMenuItem.Image");
            tableDesignerToolStripMenuItem.Name = "tableDesignerToolStripMenuItem";
            tableDesignerToolStripMenuItem.Size = new Size(270, 22);
            tableDesignerToolStripMenuItem.Text = "Table Designer...";
            // 
            // formatToolStripSeparator1
            // 
            formatToolStripSeparator1.Name = "formatToolStripSeparator1";
            formatToolStripSeparator1.Size = new Size(267, 6);
            // 
            // headingToolStripMenuItem
            // 
            headingToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { heading1ToolStripMenuItem, heading2ToolStripMenuItem, heading3ToolStripMenuItem, heading4ToolStripMenuItem, heading5ToolStripMenuItem, heading6ToolStripMenuItem });
            headingToolStripMenuItem.Image = (Image)resources.GetObject("headingToolStripMenuItem.Image");
            headingToolStripMenuItem.Name = "headingToolStripMenuItem";
            headingToolStripMenuItem.Size = new Size(270, 22);
            headingToolStripMenuItem.Text = "Heading";
            // 
            // heading1ToolStripMenuItem
            // 
            heading1ToolStripMenuItem.Name = "heading1ToolStripMenuItem";
            heading1ToolStripMenuItem.Size = new Size(89, 22);
            heading1ToolStripMenuItem.Text = "H1";
            // 
            // heading2ToolStripMenuItem
            // 
            heading2ToolStripMenuItem.Name = "heading2ToolStripMenuItem";
            heading2ToolStripMenuItem.Size = new Size(89, 22);
            heading2ToolStripMenuItem.Text = "H2";
            // 
            // heading3ToolStripMenuItem
            // 
            heading3ToolStripMenuItem.Name = "heading3ToolStripMenuItem";
            heading3ToolStripMenuItem.Size = new Size(89, 22);
            heading3ToolStripMenuItem.Text = "H3";
            // 
            // heading4ToolStripMenuItem
            // 
            heading4ToolStripMenuItem.Name = "heading4ToolStripMenuItem";
            heading4ToolStripMenuItem.Size = new Size(89, 22);
            heading4ToolStripMenuItem.Text = "H4";
            // 
            // heading5ToolStripMenuItem
            // 
            heading5ToolStripMenuItem.Name = "heading5ToolStripMenuItem";
            heading5ToolStripMenuItem.Size = new Size(89, 22);
            heading5ToolStripMenuItem.Text = "H5";
            // 
            // heading6ToolStripMenuItem
            // 
            heading6ToolStripMenuItem.Name = "heading6ToolStripMenuItem";
            heading6ToolStripMenuItem.Size = new Size(89, 22);
            heading6ToolStripMenuItem.Text = "H6";
            // 
            // formatToolStripSeparator2
            // 
            formatToolStripSeparator2.Name = "formatToolStripSeparator2";
            formatToolStripSeparator2.Size = new Size(267, 6);
            // 
            // quoteToolStripMenuItem
            // 
            quoteToolStripMenuItem.Image = (Image)resources.GetObject("quoteToolStripMenuItem.Image");
            quoteToolStripMenuItem.Name = "quoteToolStripMenuItem";
            quoteToolStripMenuItem.Size = new Size(270, 22);
            quoteToolStripMenuItem.Text = "Quote";
            // 
            // codeFenceToolStripMenuItem
            // 
            codeFenceToolStripMenuItem.Image = (Image)resources.GetObject("codeFenceToolStripMenuItem.Image");
            codeFenceToolStripMenuItem.Name = "codeFenceToolStripMenuItem";
            codeFenceToolStripMenuItem.Size = new Size(270, 22);
            codeFenceToolStripMenuItem.Text = "Code Fence";
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { findToolStripMenuItem, findNextToolStripMenuItem });
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.Size = new Size(54, 20);
            searchToolStripMenuItem.Text = "&Search";
            // 
            // findToolStripMenuItem
            // 
            findToolStripMenuItem.Image = (Image)resources.GetObject("findToolStripMenuItem.Image");
            findToolStripMenuItem.Name = "findToolStripMenuItem";
            findToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            findToolStripMenuItem.Size = new Size(148, 22);
            findToolStripMenuItem.Text = "Find...";
            // 
            // findNextToolStripMenuItem
            // 
            findNextToolStripMenuItem.Image = (Image)resources.GetObject("findNextToolStripMenuItem.Image");
            findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
            findNextToolStripMenuItem.ShortcutKeys = Keys.F3;
            findNextToolStripMenuItem.Size = new Size(148, 22);
            findNextToolStripMenuItem.Text = "Find Next";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // padToolStrip
            // 
            padToolStrip.AutoSize = false;
            padToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padToolStrip.ImageScalingSize = new Size(20, 20);
            padToolStrip.Items.AddRange(new ToolStripItem[] { newToolStripButton, openToolStripButton, saveToolStripButton, saveAllToolStripButton, toolStripSeparator1, undoToolStripButton, redoToolStripButton, toolStripSeparator2, findToolStripButton, findNextToolStripButton, toolStripSeparator3, linkToolStripButton, imageToolStripButton, toolStripSeparator9, tableToolStripButton, headingToolStripDropDownButton, quoteToolStripButton, codeFenceToolStripButton, toolStripSeparator8, themeToolStripDropDownButton });
            padToolStrip.Location = new Point(0, 24);
            padToolStrip.Name = "padToolStrip";
            padToolStrip.Size = new Size(1063, 72);
            padToolStrip.TabIndex = 1;
            padToolStrip.Text = "padToolStrip";
            // 
            // newToolStripButton
            // 
            newToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            newToolStripButton.Image = (Image)resources.GetObject("newToolStripButton.Image");
            newToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            newToolStripButton.Name = "newToolStripButton";
            newToolStripButton.Size = new Size(44, 69);
            newToolStripButton.Text = "New";
            // 
            // openToolStripButton
            // 
            openToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            openToolStripButton.Image = (Image)resources.GetObject("openToolStripButton.Image");
            openToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            openToolStripButton.Name = "openToolStripButton";
            openToolStripButton.Size = new Size(44, 69);
            openToolStripButton.Text = "Open";
            // 
            // saveToolStripButton
            // 
            saveToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            saveToolStripButton.Image = (Image)resources.GetObject("saveToolStripButton.Image");
            saveToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            saveToolStripButton.Name = "saveToolStripButton";
            saveToolStripButton.Size = new Size(44, 69);
            saveToolStripButton.Text = "Save";
            // 
            // saveAllToolStripButton
            // 
            saveAllToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            saveAllToolStripButton.Image = (Image)resources.GetObject("saveAllToolStripButton.Image");
            saveAllToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            saveAllToolStripButton.Name = "saveAllToolStripButton";
            saveAllToolStripButton.Size = new Size(44, 69);
            saveAllToolStripButton.Text = "Save All";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 72);
            // 
            // undoToolStripButton
            // 
            undoToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            undoToolStripButton.Image = (Image)resources.GetObject("undoToolStripButton.Image");
            undoToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            undoToolStripButton.Name = "undoToolStripButton";
            undoToolStripButton.Size = new Size(44, 69);
            undoToolStripButton.Text = "Undo";
            // 
            // redoToolStripButton
            // 
            redoToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            redoToolStripButton.Image = (Image)resources.GetObject("redoToolStripButton.Image");
            redoToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            redoToolStripButton.Name = "redoToolStripButton";
            redoToolStripButton.Size = new Size(44, 69);
            redoToolStripButton.Text = "Redo";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 72);
            // 
            // findToolStripButton
            // 
            findToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            findToolStripButton.Image = (Image)resources.GetObject("findToolStripButton.Image");
            findToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            findToolStripButton.Name = "findToolStripButton";
            findToolStripButton.Size = new Size(44, 69);
            findToolStripButton.Text = "Find";
            // 
            // findNextToolStripButton
            // 
            findNextToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            findNextToolStripButton.Image = (Image)resources.GetObject("findNextToolStripButton.Image");
            findNextToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            findNextToolStripButton.Name = "findNextToolStripButton";
            findNextToolStripButton.Size = new Size(44, 69);
            findNextToolStripButton.Text = "Next";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 72);
            // 
            // linkToolStripButton
            // 
            linkToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            linkToolStripButton.Image = (Image)resources.GetObject("linkToolStripButton.Image");
            linkToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            linkToolStripButton.Name = "linkToolStripButton";
            linkToolStripButton.Size = new Size(44, 69);
            linkToolStripButton.Text = "Link";
            // 
            // imageToolStripButton
            // 
            imageToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            imageToolStripButton.Image = (Image)resources.GetObject("imageToolStripButton.Image");
            imageToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            imageToolStripButton.Name = "imageToolStripButton";
            imageToolStripButton.Size = new Size(44, 69);
            imageToolStripButton.Text = "Image";
            // 
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new Size(6, 72);
            // 
            // tableToolStripButton
            // 
            tableToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tableToolStripButton.Image = (Image)resources.GetObject("tableToolStripButton.Image");
            tableToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            tableToolStripButton.Name = "tableToolStripButton";
            tableToolStripButton.Size = new Size(44, 69);
            tableToolStripButton.Text = "Table";
            // 
            // headingToolStripDropDownButton
            // 
            headingToolStripDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            headingToolStripDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { heading1ToolStripDropDownItem, heading2ToolStripDropDownItem, heading3ToolStripDropDownItem, heading4ToolStripDropDownItem, heading5ToolStripDropDownItem, heading6ToolStripDropDownItem });
            headingToolStripDropDownButton.Image = (Image)resources.GetObject("headingToolStripDropDownButton.Image");
            headingToolStripDropDownButton.ImageScaling = ToolStripItemImageScaling.None;
            headingToolStripDropDownButton.Name = "headingToolStripDropDownButton";
            headingToolStripDropDownButton.Size = new Size(53, 69);
            headingToolStripDropDownButton.Text = "Heading";
            // 
            // heading1ToolStripDropDownItem
            // 
            heading1ToolStripDropDownItem.Name = "heading1ToolStripDropDownItem";
            heading1ToolStripDropDownItem.Size = new Size(89, 22);
            heading1ToolStripDropDownItem.Text = "H1";
            // 
            // heading2ToolStripDropDownItem
            // 
            heading2ToolStripDropDownItem.Name = "heading2ToolStripDropDownItem";
            heading2ToolStripDropDownItem.Size = new Size(89, 22);
            heading2ToolStripDropDownItem.Text = "H2";
            // 
            // heading3ToolStripDropDownItem
            // 
            heading3ToolStripDropDownItem.Name = "heading3ToolStripDropDownItem";
            heading3ToolStripDropDownItem.Size = new Size(89, 22);
            heading3ToolStripDropDownItem.Text = "H3";
            // 
            // heading4ToolStripDropDownItem
            // 
            heading4ToolStripDropDownItem.Name = "heading4ToolStripDropDownItem";
            heading4ToolStripDropDownItem.Size = new Size(89, 22);
            heading4ToolStripDropDownItem.Text = "H4";
            // 
            // heading5ToolStripDropDownItem
            // 
            heading5ToolStripDropDownItem.Name = "heading5ToolStripDropDownItem";
            heading5ToolStripDropDownItem.Size = new Size(89, 22);
            heading5ToolStripDropDownItem.Text = "H5";
            // 
            // heading6ToolStripDropDownItem
            // 
            heading6ToolStripDropDownItem.Name = "heading6ToolStripDropDownItem";
            heading6ToolStripDropDownItem.Size = new Size(89, 22);
            heading6ToolStripDropDownItem.Text = "H6";
            // 
            // quoteToolStripButton
            // 
            quoteToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            quoteToolStripButton.Image = (Image)resources.GetObject("quoteToolStripButton.Image");
            quoteToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            quoteToolStripButton.Name = "quoteToolStripButton";
            quoteToolStripButton.Size = new Size(44, 69);
            quoteToolStripButton.Text = "Quote";
            // 
            // codeFenceToolStripButton
            // 
            codeFenceToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            codeFenceToolStripButton.Image = (Image)resources.GetObject("codeFenceToolStripButton.Image");
            codeFenceToolStripButton.ImageScaling = ToolStripItemImageScaling.None;
            codeFenceToolStripButton.Name = "codeFenceToolStripButton";
            codeFenceToolStripButton.Size = new Size(44, 69);
            codeFenceToolStripButton.Text = "Fence";
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new Size(6, 72);
            // 
            // themeToolStripDropDownButton
            // 
            themeToolStripDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            themeToolStripDropDownButton.DropDownItems.AddRange(new ToolStripItem[] { themeSystemToolStripDropDownItem, themeLightToolStripDropDownItem, themeDarkToolStripDropDownItem });
            themeToolStripDropDownButton.Image = (Image)resources.GetObject("themeToolStripDropDownButton.Image");
            themeToolStripDropDownButton.ImageScaling = ToolStripItemImageScaling.None;
            themeToolStripDropDownButton.Name = "themeToolStripDropDownButton";
            themeToolStripDropDownButton.Size = new Size(53, 69);
            themeToolStripDropDownButton.Text = "Theme";
            // 
            // themeSystemToolStripDropDownItem
            // 
            themeSystemToolStripDropDownItem.Name = "themeSystemToolStripDropDownItem";
            themeSystemToolStripDropDownItem.Size = new Size(152, 22);
            themeSystemToolStripDropDownItem.Text = "System Theme";
            // 
            // themeLightToolStripDropDownItem
            // 
            themeLightToolStripDropDownItem.Name = "themeLightToolStripDropDownItem";
            themeLightToolStripDropDownItem.Size = new Size(152, 22);
            themeLightToolStripDropDownItem.Text = "Light";
            // 
            // themeDarkToolStripDropDownItem
            // 
            themeDarkToolStripDropDownItem.Name = "themeDarkToolStripDropDownItem";
            themeDarkToolStripDropDownItem.Size = new Size(152, 22);
            themeDarkToolStripDropDownItem.Text = "Dark";
            // 
            // tabControl1
            // 
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.HotTrack = true;
            tabControl1.Location = new Point(0, 96);
            tabControl1.Margin = new Padding(3, 2, 3, 2);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.ShowToolTips = true;
            tabControl1.Size = new Size(1063, 451);
            tabControl1.TabIndex = 2;
            // 
            // padStatusToolStrip
            // 
            padStatusToolStrip.Dock = DockStyle.Bottom;
            padStatusToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padStatusToolStrip.ImageScalingSize = new Size(20, 20);
            padStatusToolStrip.Items.AddRange(new ToolStripItem[] { documentStatusLabel, toolStripSeparator4, pathStatusLabel, toolStripSeparator5, positionStatusLabel, toolStripSeparator6, themeStatusLabel, toolStripSeparator7, messageStatusLabel });
            padStatusToolStrip.Location = new Point(0, 547);
            padStatusToolStrip.Name = "padStatusToolStrip";
            padStatusToolStrip.Size = new Size(1063, 25);
            padStatusToolStrip.TabIndex = 3;
            padStatusToolStrip.Text = "padStatusToolStrip";
            // 
            // documentStatusLabel
            // 
            documentStatusLabel.Name = "documentStatusLabel";
            documentStatusLabel.Size = new Size(74, 22);
            documentStatusLabel.Text = "Document: -";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);
            // 
            // pathStatusLabel
            // 
            pathStatusLabel.Name = "pathStatusLabel";
            pathStatusLabel.Size = new Size(82, 22);
            pathStatusLabel.Text = "Path: Unsaved";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 25);
            // 
            // positionStatusLabel
            // 
            positionStatusLabel.Name = "positionStatusLabel";
            positionStatusLabel.Size = new Size(61, 22);
            positionStatusLabel.Text = "Position: -";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 25);
            // 
            // themeStatusLabel
            // 
            themeStatusLabel.Name = "themeStatusLabel";
            themeStatusLabel.Size = new Size(88, 22);
            themeStatusLabel.Text = "Theme: System";
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(6, 25);
            // 
            // messageStatusLabel
            // 
            messageStatusLabel.Name = "messageStatusLabel";
            messageStatusLabel.Size = new Size(39, 22);
            messageStatusLabel.Text = "Ready";
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1063, 572);
            Controls.Add(tabControl1);
            Controls.Add(padToolStrip);
            Controls.Add(padMenu);
            Controls.Add(padStatusToolStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = padMenu;
            Margin = new Padding(3, 2, 3, 2);
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
        private ToolStripMenuItem formatToolStripMenuItem;
        private ToolStripMenuItem insertLinkToolStripMenuItem;
        private ToolStripMenuItem insertImageToolStripMenuItem;
        private ToolStripSeparator formatToolStripSeparator0;
        private ToolStripMenuItem tableDesignerToolStripMenuItem;
        private ToolStripSeparator formatToolStripSeparator1;
        private ToolStripMenuItem headingToolStripMenuItem;
        private ToolStripMenuItem heading1ToolStripMenuItem;
        private ToolStripMenuItem heading2ToolStripMenuItem;
        private ToolStripMenuItem heading3ToolStripMenuItem;
        private ToolStripMenuItem heading4ToolStripMenuItem;
        private ToolStripMenuItem heading5ToolStripMenuItem;
        private ToolStripMenuItem heading6ToolStripMenuItem;
        private ToolStripSeparator formatToolStripSeparator2;
        private ToolStripMenuItem quoteToolStripMenuItem;
        private ToolStripMenuItem codeFenceToolStripMenuItem;
        private ToolStripMenuItem searchToolStripMenuItem;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem findNextToolStripMenuItem;
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
        private ToolStripButton linkToolStripButton;
        private ToolStripButton imageToolStripButton;
        private ToolStripSeparator toolStripSeparator9;
        private ToolStripButton tableToolStripButton;
        private ToolStripDropDownButton headingToolStripDropDownButton;
        private ToolStripMenuItem heading1ToolStripDropDownItem;
        private ToolStripMenuItem heading2ToolStripDropDownItem;
        private ToolStripMenuItem heading3ToolStripDropDownItem;
        private ToolStripMenuItem heading4ToolStripDropDownItem;
        private ToolStripMenuItem heading5ToolStripDropDownItem;
        private ToolStripMenuItem heading6ToolStripDropDownItem;
        private ToolStripButton quoteToolStripButton;
        private ToolStripButton codeFenceToolStripButton;
        private ToolStripSeparator toolStripSeparator8;
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
        private ToolStripMenuItem helpToolStripMenuItem;
    }
}
