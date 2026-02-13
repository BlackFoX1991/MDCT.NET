using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MarkdownEditor
{
    partial class mkEditor
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
            menuMain = new MenuStrip();
            mnuFile = new ToolStripMenuItem();
            mnuNewTab = new ToolStripMenuItem();
            mnuOpen = new ToolStripMenuItem();
            mnuSave = new ToolStripMenuItem();
            mnuSaveAs = new ToolStripMenuItem();
            sepFile1 = new ToolStripSeparator();
            mnuPageSetup = new ToolStripMenuItem();
            mnuPrint = new ToolStripMenuItem();
            sepFile2 = new ToolStripSeparator();
            mnuCloseTab = new ToolStripMenuItem();
            sepFile3 = new ToolStripSeparator();
            mnuExit = new ToolStripMenuItem();
            mnuEdit = new ToolStripMenuItem();
            mnuUndo = new ToolStripMenuItem();
            mnuRedo = new ToolStripMenuItem();
            sepEdit1 = new ToolStripSeparator();
            mnuCut = new ToolStripMenuItem();
            mnuCopy = new ToolStripMenuItem();
            mnuPaste = new ToolStripMenuItem();
            mnuDelete = new ToolStripMenuItem();
            sepEdit2 = new ToolStripSeparator();
            mnuFind = new ToolStripMenuItem();
            mnuFindNext = new ToolStripMenuItem();
            mnuReplace = new ToolStripMenuItem();
            mnuGoTo = new ToolStripMenuItem();
            sepEdit3 = new ToolStripSeparator();
            mnuSelectAll = new ToolStripMenuItem();
            mnuTimeDate = new ToolStripMenuItem();
            mnuFormat = new ToolStripMenuItem();
            mnuWordWrap = new ToolStripMenuItem();
            mnuFont = new ToolStripMenuItem();
            mnuView = new ToolStripMenuItem();
            mnuZoomIn = new ToolStripMenuItem();
            mnuZoomOut = new ToolStripMenuItem();
            mnuZoomReset = new ToolStripMenuItem();
            sepView1 = new ToolStripSeparator();
            mnuStatusBar = new ToolStripMenuItem();
            mnuHelp = new ToolStripMenuItem();
            mnuAbout = new ToolStripMenuItem();
            tabEditors = new TabControl();
            ctxTabs = new ContextMenuStrip(components);
            ctxCloseTab = new ToolStripMenuItem();
            ctxCloseOtherTabs = new ToolStripMenuItem();
            ctxCloseAllTabs = new ToolStripMenuItem();
            statusMain = new StatusStrip();
            lblPosition = new ToolStripStatusLabel();
            lblSpring = new ToolStripStatusLabel();
            lblEncoding = new ToolStripStatusLabel();
            lblZoom = new ToolStripStatusLabel();
            printDocument1 = new System.Drawing.Printing.PrintDocument();
            pageSetupDialog1 = new PageSetupDialog();
            printDialog1 = new PrintDialog();
            menuMain.SuspendLayout();
            ctxTabs.SuspendLayout();
            statusMain.SuspendLayout();
            SuspendLayout();
            // 
            // menuMain
            // 
            menuMain.Items.AddRange(new ToolStripItem[] { mnuFile, mnuEdit, mnuFormat, mnuView, mnuHelp });
            menuMain.Location = new Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Size = new Size(1200, 24);
            menuMain.TabIndex = 0;
            // 
            // mnuFile
            // 
            mnuFile.DropDownItems.AddRange(new ToolStripItem[] { mnuNewTab, mnuOpen, mnuSave, mnuSaveAs, sepFile1, mnuPageSetup, mnuPrint, sepFile2, mnuCloseTab, sepFile3, mnuExit });
            mnuFile.Name = "mnuFile";
            mnuFile.Size = new Size(37, 20);
            mnuFile.Text = "&File";
            // 
            // mnuNewTab
            // 
            mnuNewTab.Name = "mnuNewTab";
            mnuNewTab.ShortcutKeys = Keys.Control | Keys.N;
            mnuNewTab.Size = new Size(248, 22);
            mnuNewTab.Text = "&New Tab";
            mnuNewTab.Click += mnuNewTab_Click;
            // 
            // mnuOpen
            // 
            mnuOpen.Name = "mnuOpen";
            mnuOpen.ShortcutKeys = Keys.Control | Keys.O;
            mnuOpen.Size = new Size(248, 22);
            mnuOpen.Text = "&Open...";
            mnuOpen.Click += mnuOpen_Click;
            // 
            // mnuSave
            // 
            mnuSave.Name = "mnuSave";
            mnuSave.ShortcutKeys = Keys.Control | Keys.S;
            mnuSave.Size = new Size(248, 22);
            mnuSave.Text = "&Save";
            mnuSave.Click += mnuSave_Click;
            // 
            // mnuSaveAs
            // 
            mnuSaveAs.Name = "mnuSaveAs";
            mnuSaveAs.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            mnuSaveAs.Size = new Size(248, 22);
            mnuSaveAs.Text = "Save &As...";
            mnuSaveAs.Click += mnuSaveAs_Click;
            // 
            // sepFile1
            // 
            sepFile1.Name = "sepFile1";
            sepFile1.Size = new Size(245, 6);
            // 
            // mnuPageSetup
            // 
            mnuPageSetup.Name = "mnuPageSetup";
            mnuPageSetup.Size = new Size(248, 22);
            mnuPageSetup.Text = "Page Set&up...";
            mnuPageSetup.Click += mnuPageSetup_Click;
            // 
            // mnuPrint
            // 
            mnuPrint.Name = "mnuPrint";
            mnuPrint.ShortcutKeys = Keys.Control | Keys.P;
            mnuPrint.Size = new Size(248, 22);
            mnuPrint.Text = "&Print...";
            mnuPrint.Click += mnuPrint_Click;
            // 
            // sepFile2
            // 
            sepFile2.Name = "sepFile2";
            sepFile2.Size = new Size(245, 6);
            // 
            // mnuCloseTab
            // 
            mnuCloseTab.Name = "mnuCloseTab";
            mnuCloseTab.ShortcutKeys = Keys.Control | Keys.W;
            mnuCloseTab.Size = new Size(248, 22);
            mnuCloseTab.Text = "&Close Tab";
            mnuCloseTab.Click += mnuCloseTab_Click;
            // 
            // sepFile3
            // 
            sepFile3.Name = "sepFile3";
            sepFile3.Size = new Size(245, 6);
            // 
            // mnuExit
            // 
            mnuExit.Name = "mnuExit";
            mnuExit.Size = new Size(248, 22);
            mnuExit.Text = "E&xit";
            mnuExit.Click += mnuExit_Click;
            // 
            // mnuEdit
            // 
            mnuEdit.DropDownItems.AddRange(new ToolStripItem[] { mnuUndo, mnuRedo, sepEdit1, mnuCut, mnuCopy, mnuPaste, mnuDelete, sepEdit2, mnuFind, mnuFindNext, mnuReplace, mnuGoTo, sepEdit3, mnuSelectAll, mnuTimeDate });
            mnuEdit.Name = "mnuEdit";
            mnuEdit.Size = new Size(39, 20);
            mnuEdit.Text = "&Edit";
            // 
            // mnuUndo
            // 
            mnuUndo.Name = "mnuUndo";
            mnuUndo.ShortcutKeys = Keys.Control | Keys.Z;
            mnuUndo.Size = new Size(169, 22);
            mnuUndo.Text = "&Undo";
            
            // 
            // mnuRedo
            // 
            mnuRedo.Name = "mnuRedo";
            mnuRedo.ShortcutKeys = Keys.Control | Keys.Y;
            mnuRedo.Size = new Size(169, 22);
            mnuRedo.Text = "&Redo";
            
            // 
            // sepEdit1
            // 
            sepEdit1.Name = "sepEdit1";
            sepEdit1.Size = new Size(166, 6);
            // 
            // mnuCut
            // 
            mnuCut.Name = "mnuCut";
            mnuCut.ShortcutKeys = Keys.Control | Keys.X;
            mnuCut.Size = new Size(169, 22);
            mnuCut.Text = "Cu&t";
            mnuCut.Click += mnuCut_Click;
            // 
            // mnuCopy
            // 
            mnuCopy.Name = "mnuCopy";
            mnuCopy.ShortcutKeys = Keys.Control | Keys.C;
            mnuCopy.Size = new Size(169, 22);
            mnuCopy.Text = "&Copy";
            mnuCopy.Click += mnuCopy_Click;
            // 
            // mnuPaste
            // 
            mnuPaste.Name = "mnuPaste";
            mnuPaste.ShortcutKeys = Keys.Control | Keys.V;
            mnuPaste.Size = new Size(169, 22);
            mnuPaste.Text = "&Paste";
            mnuPaste.Click += mnuPaste_Click;
            // 
            // mnuDelete
            // 
            mnuDelete.Name = "mnuDelete";
            mnuDelete.ShortcutKeys = Keys.Delete;
            mnuDelete.Size = new Size(169, 22);
            mnuDelete.Text = "De&lete";
            
            // 
            // sepEdit2
            // 
            sepEdit2.Name = "sepEdit2";
            sepEdit2.Size = new Size(166, 6);
            // 
            // mnuFind
            // 
            mnuFind.Name = "mnuFind";
            mnuFind.ShortcutKeys = Keys.Control | Keys.F;
            mnuFind.Size = new Size(169, 22);
            mnuFind.Text = "&Find...";
            
            // 
            // mnuFindNext
            // 
            mnuFindNext.Name = "mnuFindNext";
            mnuFindNext.ShortcutKeys = Keys.F3;
            mnuFindNext.Size = new Size(169, 22);
            mnuFindNext.Text = "Find &Next";
            
            // 
            // mnuReplace
            // 
            mnuReplace.Name = "mnuReplace";
            mnuReplace.ShortcutKeys = Keys.Control | Keys.H;
            mnuReplace.Size = new Size(169, 22);
            mnuReplace.Text = "&Replace...";
            
            // 
            // mnuGoTo
            // 
            mnuGoTo.Name = "mnuGoTo";
            mnuGoTo.ShortcutKeys = Keys.Control | Keys.G;
            mnuGoTo.Size = new Size(169, 22);
            mnuGoTo.Text = "&Go To...";
            
            // 
            // sepEdit3
            // 
            sepEdit3.Name = "sepEdit3";
            sepEdit3.Size = new Size(166, 6);
            // 
            // mnuSelectAll
            // 
            mnuSelectAll.Name = "mnuSelectAll";
            mnuSelectAll.ShortcutKeys = Keys.Control | Keys.A;
            mnuSelectAll.Size = new Size(169, 22);
            mnuSelectAll.Text = "Select &All";
            mnuSelectAll.Click += mnuSelectAll_Click;
            // 
            // mnuTimeDate
            // 
            mnuTimeDate.Name = "mnuTimeDate";
            mnuTimeDate.ShortcutKeys = Keys.F5;
            mnuTimeDate.Size = new Size(169, 22);
            mnuTimeDate.Text = "Time/&Date";
            
            // 
            // mnuFormat
            // 
            mnuFormat.DropDownItems.AddRange(new ToolStripItem[] { mnuWordWrap, mnuFont });
            mnuFormat.Name = "mnuFormat";
            mnuFormat.Size = new Size(57, 20);
            mnuFormat.Text = "F&ormat";
            // 
            // mnuWordWrap
            // 
            mnuWordWrap.Checked = true;
            mnuWordWrap.CheckOnClick = true;
            mnuWordWrap.CheckState = CheckState.Checked;
            mnuWordWrap.Name = "mnuWordWrap";
            mnuWordWrap.Size = new Size(134, 22);
            mnuWordWrap.Text = "&Word Wrap";
            mnuWordWrap.Click += mnuWordWrap_Click;
            // 
            // mnuFont
            // 
            mnuFont.Name = "mnuFont";
            mnuFont.Size = new Size(134, 22);
            mnuFont.Text = "&Font...";
            mnuFont.Click += mnuFont_Click;
            // 
            // mnuView
            // 
            mnuView.DropDownItems.AddRange(new ToolStripItem[] { mnuZoomIn, mnuZoomOut, mnuZoomReset, sepView1, mnuStatusBar });
            mnuView.Name = "mnuView";
            mnuView.Size = new Size(44, 20);
            mnuView.Text = "&View";
            // 
            // mnuZoomIn
            // 
            mnuZoomIn.Name = "mnuZoomIn";
            mnuZoomIn.ShortcutKeys = Keys.Control | Keys.Oemplus;
            mnuZoomIn.Size = new Size(231, 22);
            mnuZoomIn.Text = "Zoom &In";
           
            // 
            // mnuZoomOut
            // 
            mnuZoomOut.Name = "mnuZoomOut";
            mnuZoomOut.ShortcutKeys = Keys.Control | Keys.OemMinus;
            mnuZoomOut.Size = new Size(231, 22);
            mnuZoomOut.Text = "Zoom &Out";
            
            // 
            // mnuZoomReset
            // 
            mnuZoomReset.Name = "mnuZoomReset";
            mnuZoomReset.ShortcutKeys = Keys.Control | Keys.D0;
            mnuZoomReset.Size = new Size(231, 22);
            mnuZoomReset.Text = "&Restore Default Zoom";
            
            // 
            // sepView1
            // 
            sepView1.Name = "sepView1";
            sepView1.Size = new Size(228, 6);
            // 
            // mnuStatusBar
            // 
            mnuStatusBar.Checked = true;
            mnuStatusBar.CheckOnClick = true;
            mnuStatusBar.CheckState = CheckState.Checked;
            mnuStatusBar.Name = "mnuStatusBar";
            mnuStatusBar.Size = new Size(231, 22);
            mnuStatusBar.Text = "&Status Bar";
            mnuStatusBar.Click += mnuStatusBar_Click;
            // 
            // mnuHelp
            // 
            mnuHelp.DropDownItems.AddRange(new ToolStripItem[] { mnuAbout });
            mnuHelp.Name = "mnuHelp";
            mnuHelp.Size = new Size(44, 20);
            mnuHelp.Text = "&Help";
            // 
            // mnuAbout
            // 
            mnuAbout.Name = "mnuAbout";
            mnuAbout.Size = new Size(107, 22);
            mnuAbout.Text = "&About";
            mnuAbout.Click += mnuAbout_Click;
            // 
            // tabEditors
            // 
            tabEditors.ContextMenuStrip = ctxTabs;
            tabEditors.Dock = DockStyle.Fill;
            tabEditors.Location = new Point(0, 24);
            tabEditors.Name = "tabEditors";
            tabEditors.SelectedIndex = 0;
            tabEditors.Size = new Size(1200, 654);
            tabEditors.TabIndex = 1;
            tabEditors.SelectedIndexChanged += tabEditors_SelectedIndexChanged;
            tabEditors.MouseUp += tabEditors_MouseUp;
            // 
            // ctxTabs
            // 
            ctxTabs.Items.AddRange(new ToolStripItem[] { ctxCloseTab, ctxCloseOtherTabs, ctxCloseAllTabs });
            ctxTabs.Name = "ctxTabs";
            ctxTabs.Size = new Size(164, 70);
            // 
            // ctxCloseTab
            // 
            ctxCloseTab.Name = "ctxCloseTab";
            ctxCloseTab.Size = new Size(163, 22);
            ctxCloseTab.Text = "Close Tab";
            ctxCloseTab.Click += ctxCloseTab_Click;
            // 
            // ctxCloseOtherTabs
            // 
            ctxCloseOtherTabs.Name = "ctxCloseOtherTabs";
            ctxCloseOtherTabs.Size = new Size(163, 22);
            ctxCloseOtherTabs.Text = "Close Other Tabs";
            ctxCloseOtherTabs.Click += ctxCloseOtherTabs_Click;
            // 
            // ctxCloseAllTabs
            // 
            ctxCloseAllTabs.Name = "ctxCloseAllTabs";
            ctxCloseAllTabs.Size = new Size(163, 22);
            ctxCloseAllTabs.Text = "Close All Tabs";
            ctxCloseAllTabs.Click += ctxCloseAllTabs_Click;
            // 
            // statusMain
            // 
            statusMain.Items.AddRange(new ToolStripItem[] { lblPosition, lblSpring, lblEncoding, lblZoom });
            statusMain.Location = new Point(0, 678);
            statusMain.Name = "statusMain";
            statusMain.Size = new Size(1200, 22);
            statusMain.TabIndex = 2;
            statusMain.Text = "statusMain";
            // 
            // lblPosition
            // 
            lblPosition.Name = "lblPosition";
            lblPosition.Size = new Size(62, 17);
            lblPosition.Text = "Ln 1, Col 1";
            // 
            // lblSpring
            // 
            lblSpring.Name = "lblSpring";
            lblSpring.Size = new Size(1018, 17);
            lblSpring.Spring = true;
            // 
            // lblEncoding
            // 
            lblEncoding.Name = "lblEncoding";
            lblEncoding.Size = new Size(39, 17);
            lblEncoding.Text = "UTF-8";
            // 
            // lblZoom
            // 
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(35, 17);
            lblZoom.Text = "100%";
            // 
            // printDocument1
            // 
            printDocument1.BeginPrint += printDocument1_BeginPrint;
            printDocument1.PrintPage += printDocument1_PrintPage;
            // 
            // printDialog1
            // 
            printDialog1.UseEXDialog = true;
            // 
            // mkEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 700);
            Controls.Add(tabEditors);
            Controls.Add(menuMain);
            Controls.Add(statusMain);
            MainMenuStrip = menuMain;
            Name = "mkEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MarkdownEditor";
            FormClosing += MainForm_FormClosing;
            menuMain.ResumeLayout(false);
            menuMain.PerformLayout();
            ctxTabs.ResumeLayout(false);
            statusMain.ResumeLayout(false);
            statusMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuMain;

        private ToolStripMenuItem mnuFile;
        private ToolStripMenuItem mnuNewTab;
        private ToolStripMenuItem mnuOpen;
        private ToolStripMenuItem mnuSave;
        private ToolStripMenuItem mnuSaveAs;
        private ToolStripSeparator sepFile1;
        private ToolStripMenuItem mnuPageSetup;
        private ToolStripMenuItem mnuPrint;
        private ToolStripSeparator sepFile2;
        private ToolStripMenuItem mnuCloseTab;
        private ToolStripSeparator sepFile3;
        private ToolStripMenuItem mnuExit;

        private ToolStripMenuItem mnuEdit;
        private ToolStripMenuItem mnuUndo;
        private ToolStripMenuItem mnuRedo;
        private ToolStripSeparator sepEdit1;
        private ToolStripMenuItem mnuCut;
        private ToolStripMenuItem mnuCopy;
        private ToolStripMenuItem mnuPaste;
        private ToolStripMenuItem mnuDelete;
        private ToolStripSeparator sepEdit2;
        private ToolStripMenuItem mnuFind;
        private ToolStripMenuItem mnuFindNext;
        private ToolStripMenuItem mnuReplace;
        private ToolStripMenuItem mnuGoTo;
        private ToolStripSeparator sepEdit3;
        private ToolStripMenuItem mnuSelectAll;
        private ToolStripMenuItem mnuTimeDate;

        private ToolStripMenuItem mnuFormat;
        private ToolStripMenuItem mnuWordWrap;
        private ToolStripMenuItem mnuFont;

        private ToolStripMenuItem mnuView;
        private ToolStripMenuItem mnuZoomIn;
        private ToolStripMenuItem mnuZoomOut;
        private ToolStripMenuItem mnuZoomReset;
        private ToolStripSeparator sepView1;
        private ToolStripMenuItem mnuStatusBar;

        private ToolStripMenuItem mnuHelp;
        private ToolStripMenuItem mnuAbout;

        private TabControl tabEditors;
        private ContextMenuStrip ctxTabs;
        private ToolStripMenuItem ctxCloseTab;
        private ToolStripMenuItem ctxCloseOtherTabs;
        private ToolStripMenuItem ctxCloseAllTabs;

        private StatusStrip statusMain;
        private ToolStripStatusLabel lblPosition;
        private ToolStripStatusLabel lblSpring;
        private ToolStripStatusLabel lblEncoding;
        private ToolStripStatusLabel lblZoom;

        private System.Drawing.Printing.PrintDocument printDocument1;
        private PageSetupDialog pageSetupDialog1;
        private PrintDialog printDialog1;
    }
}
