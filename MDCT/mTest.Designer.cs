namespace MDCT
{
    partial class mTest
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
            markdownGdiEditor1 = new MarkdownGdi.MarkdownGdiEditor();
            SuspendLayout();
            // 
            // markdownGdiEditor1
            // 
            markdownGdiEditor1.AutoScroll = true;
            markdownGdiEditor1.AutoScrollMinSize = new Size(800, 450);
            markdownGdiEditor1.BackColor = Color.White;
            markdownGdiEditor1.Dock = DockStyle.Fill;
            markdownGdiEditor1.ForeColor = Color.Black;
            markdownGdiEditor1.Location = new Point(0, 0);
            markdownGdiEditor1.Markdown = "";
            markdownGdiEditor1.Name = "markdownGdiEditor1";
            markdownGdiEditor1.Size = new Size(800, 450);
            markdownGdiEditor1.TabIndex = 0;
            // 
            // mTest
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(markdownGdiEditor1);
            Name = "mTest";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private MarkdownGdi.MarkdownGdiEditor markdownGdiEditor1;
    }
}
