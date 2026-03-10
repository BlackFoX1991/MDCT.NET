namespace MarkdownPad
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            padMenu = new MenuStrip();
            dateiToolStripMenuItem = new ToolStripMenuItem();
            neuToolStripMenuItem = new ToolStripMenuItem();
            öffnenToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator = new ToolStripSeparator();
            speichernToolStripMenuItem = new ToolStripMenuItem();
            speichernunterToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            druckenToolStripMenuItem = new ToolStripMenuItem();
            seitenansichtToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            beendenToolStripMenuItem = new ToolStripMenuItem();
            bearbeitenToolStripMenuItem = new ToolStripMenuItem();
            rückgängigToolStripMenuItem = new ToolStripMenuItem();
            wiederholenToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            ausschneidenToolStripMenuItem = new ToolStripMenuItem();
            kopierenToolStripMenuItem = new ToolStripMenuItem();
            einfügenToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            allesauswählenToolStripMenuItem = new ToolStripMenuItem();
            padToolStrip = new ToolStrip();
            tabControl1 = new TabControl();
            padStatusToolStrip = new ToolStrip();
            padMenu.SuspendLayout();
            SuspendLayout();
            // 
            // padMenu
            // 
            padMenu.ImageScalingSize = new Size(20, 20);
            padMenu.Items.AddRange(new ToolStripItem[] { dateiToolStripMenuItem, bearbeitenToolStripMenuItem });
            padMenu.Location = new Point(0, 0);
            padMenu.Name = "padMenu";
            padMenu.Size = new Size(1215, 28);
            padMenu.TabIndex = 0;
            padMenu.Text = "menuStrip1";
            // 
            // dateiToolStripMenuItem
            // 
            dateiToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { neuToolStripMenuItem, öffnenToolStripMenuItem, toolStripSeparator, speichernToolStripMenuItem, speichernunterToolStripMenuItem, toolStripSeparator1, druckenToolStripMenuItem, seitenansichtToolStripMenuItem, toolStripSeparator2, beendenToolStripMenuItem });
            dateiToolStripMenuItem.Name = "dateiToolStripMenuItem";
            dateiToolStripMenuItem.Size = new Size(59, 24);
            dateiToolStripMenuItem.Text = "&Datei";
            // 
            // neuToolStripMenuItem
            // 
            neuToolStripMenuItem.Image = (Image)resources.GetObject("neuToolStripMenuItem.Image");
            neuToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            neuToolStripMenuItem.Name = "neuToolStripMenuItem";
            neuToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            neuToolStripMenuItem.Size = new Size(211, 26);
            neuToolStripMenuItem.Text = "&Neu";
            // 
            // öffnenToolStripMenuItem
            // 
            öffnenToolStripMenuItem.Image = (Image)resources.GetObject("öffnenToolStripMenuItem.Image");
            öffnenToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            öffnenToolStripMenuItem.Name = "öffnenToolStripMenuItem";
            öffnenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            öffnenToolStripMenuItem.Size = new Size(211, 26);
            öffnenToolStripMenuItem.Text = "Ö&ffnen";
            // 
            // toolStripSeparator
            // 
            toolStripSeparator.Name = "toolStripSeparator";
            toolStripSeparator.Size = new Size(208, 6);
            // 
            // speichernToolStripMenuItem
            // 
            speichernToolStripMenuItem.Image = (Image)resources.GetObject("speichernToolStripMenuItem.Image");
            speichernToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            speichernToolStripMenuItem.Name = "speichernToolStripMenuItem";
            speichernToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            speichernToolStripMenuItem.Size = new Size(211, 26);
            speichernToolStripMenuItem.Text = "&Speichern";
            // 
            // speichernunterToolStripMenuItem
            // 
            speichernunterToolStripMenuItem.Name = "speichernunterToolStripMenuItem";
            speichernunterToolStripMenuItem.Size = new Size(211, 26);
            speichernunterToolStripMenuItem.Text = "Speichern &unter";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(208, 6);
            // 
            // druckenToolStripMenuItem
            // 
            druckenToolStripMenuItem.Image = (Image)resources.GetObject("druckenToolStripMenuItem.Image");
            druckenToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            druckenToolStripMenuItem.Name = "druckenToolStripMenuItem";
            druckenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.P;
            druckenToolStripMenuItem.Size = new Size(211, 26);
            druckenToolStripMenuItem.Text = "&Drucken";
            // 
            // seitenansichtToolStripMenuItem
            // 
            seitenansichtToolStripMenuItem.Image = (Image)resources.GetObject("seitenansichtToolStripMenuItem.Image");
            seitenansichtToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            seitenansichtToolStripMenuItem.Name = "seitenansichtToolStripMenuItem";
            seitenansichtToolStripMenuItem.Size = new Size(211, 26);
            seitenansichtToolStripMenuItem.Text = "&Seitenansicht";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(208, 6);
            // 
            // beendenToolStripMenuItem
            // 
            beendenToolStripMenuItem.Name = "beendenToolStripMenuItem";
            beendenToolStripMenuItem.Size = new Size(211, 26);
            beendenToolStripMenuItem.Text = "&Beenden";
            // 
            // bearbeitenToolStripMenuItem
            // 
            bearbeitenToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { rückgängigToolStripMenuItem, wiederholenToolStripMenuItem, toolStripSeparator3, ausschneidenToolStripMenuItem, kopierenToolStripMenuItem, einfügenToolStripMenuItem, toolStripSeparator4, allesauswählenToolStripMenuItem });
            bearbeitenToolStripMenuItem.Name = "bearbeitenToolStripMenuItem";
            bearbeitenToolStripMenuItem.Size = new Size(95, 24);
            bearbeitenToolStripMenuItem.Text = "&Bearbeiten";
            // 
            // rückgängigToolStripMenuItem
            // 
            rückgängigToolStripMenuItem.Name = "rückgängigToolStripMenuItem";
            rückgängigToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            rückgängigToolStripMenuItem.Size = new Size(237, 26);
            rückgängigToolStripMenuItem.Text = "&Rückgängig";
            // 
            // wiederholenToolStripMenuItem
            // 
            wiederholenToolStripMenuItem.Name = "wiederholenToolStripMenuItem";
            wiederholenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            wiederholenToolStripMenuItem.Size = new Size(237, 26);
            wiederholenToolStripMenuItem.Text = "&Wiederholen";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(234, 6);
            // 
            // ausschneidenToolStripMenuItem
            // 
            ausschneidenToolStripMenuItem.Image = (Image)resources.GetObject("ausschneidenToolStripMenuItem.Image");
            ausschneidenToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            ausschneidenToolStripMenuItem.Name = "ausschneidenToolStripMenuItem";
            ausschneidenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            ausschneidenToolStripMenuItem.Size = new Size(237, 26);
            ausschneidenToolStripMenuItem.Text = "Aussc&hneiden";
            // 
            // kopierenToolStripMenuItem
            // 
            kopierenToolStripMenuItem.Image = (Image)resources.GetObject("kopierenToolStripMenuItem.Image");
            kopierenToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            kopierenToolStripMenuItem.Name = "kopierenToolStripMenuItem";
            kopierenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            kopierenToolStripMenuItem.Size = new Size(237, 26);
            kopierenToolStripMenuItem.Text = "&Kopieren";
            // 
            // einfügenToolStripMenuItem
            // 
            einfügenToolStripMenuItem.Image = (Image)resources.GetObject("einfügenToolStripMenuItem.Image");
            einfügenToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            einfügenToolStripMenuItem.Name = "einfügenToolStripMenuItem";
            einfügenToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            einfügenToolStripMenuItem.Size = new Size(237, 26);
            einfügenToolStripMenuItem.Text = "&Einfügen";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(234, 6);
            // 
            // allesauswählenToolStripMenuItem
            // 
            allesauswählenToolStripMenuItem.Name = "allesauswählenToolStripMenuItem";
            allesauswählenToolStripMenuItem.Size = new Size(237, 26);
            allesauswählenToolStripMenuItem.Text = "&Alles auswählen";
            // 
            // padToolStrip
            // 
            padToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padToolStrip.ImageScalingSize = new Size(20, 20);
            padToolStrip.Location = new Point(0, 28);
            padToolStrip.Name = "padToolStrip";
            padToolStrip.Size = new Size(1215, 25);
            padToolStrip.TabIndex = 1;
            padToolStrip.Text = "toolStrip1";
            // 
            // tabControl1
            // 
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 53);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.ShowToolTips = true;
            tabControl1.Size = new Size(1215, 684);
            tabControl1.TabIndex = 2;
            // 
            // padStatusToolStrip
            // 
            padStatusToolStrip.Dock = DockStyle.Bottom;
            padStatusToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            padStatusToolStrip.ImageScalingSize = new Size(20, 20);
            padStatusToolStrip.Location = new Point(0, 737);
            padStatusToolStrip.Name = "padStatusToolStrip";
            padStatusToolStrip.Size = new Size(1215, 25);
            padStatusToolStrip.TabIndex = 3;
            padStatusToolStrip.Text = "toolStrip2";
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
            padMenu.ResumeLayout(false);
            padMenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip padMenu;
        private ToolStripMenuItem dateiToolStripMenuItem;
        private ToolStripMenuItem neuToolStripMenuItem;
        private ToolStripMenuItem öffnenToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripMenuItem speichernToolStripMenuItem;
        private ToolStripMenuItem speichernunterToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem druckenToolStripMenuItem;
        private ToolStripMenuItem seitenansichtToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem beendenToolStripMenuItem;
        private ToolStripMenuItem bearbeitenToolStripMenuItem;
        private ToolStripMenuItem rückgängigToolStripMenuItem;
        private ToolStripMenuItem wiederholenToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem ausschneidenToolStripMenuItem;
        private ToolStripMenuItem kopierenToolStripMenuItem;
        private ToolStripMenuItem einfügenToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem allesauswählenToolStripMenuItem;
        private ToolStrip padToolStrip;
        private TabControl tabControl1;
        private ToolStrip padStatusToolStrip;
    }
}
