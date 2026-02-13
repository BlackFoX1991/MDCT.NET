using MarkdownGdi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MarkdownEditor
{
    internal class mkTabPage : TabPage
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MarkdownGdiEditor editControl { get => _editControl ?? new(); set=> _editControl = value; }

        private MarkdownGdiEditor _editControl;

        public mkTabPage()
        {
            _editControl = new();
            editControl.Dock = DockStyle.Fill;
            
        }

    }
}
