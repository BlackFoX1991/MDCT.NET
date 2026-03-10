using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using MarkdownGdi;

namespace MarkdownPad
{
    public sealed class padTab : TabPage
    {

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MarkdownGdiEditor Editor { get; set; } = new MarkdownGdiEditor();

        public bool Modified { get; private set; } = false;

        public padTab()
        {
            Editor.Dock = DockStyle.Fill;
            Controls.Add(Editor);
            Editor.TextChanged += Editor_TextChanged;
        }

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            
        }
    }
}
