namespace MarkdownPad
{
    partial class FindDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindDialog));
            queryLabel = new Label();
            queryTextBox = new TextBox();
            caseSensitiveCheckBox = new CheckBox();
            wholeWordCheckBox = new CheckBox();
            interpretEscapesCheckBox = new CheckBox();
            wrapAroundCheckBox = new CheckBox();
            escapeModeLabel = new Label();
            escapeModeComboBox = new ComboBox();
            buttonPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            findButton = new Button();
            layoutPanel = new TableLayoutPanel();
            buttonPanel.SuspendLayout();
            layoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // queryLabel
            // 
            queryLabel.Anchor = AnchorStyles.Left;
            queryLabel.AutoSize = true;
            queryLabel.Location = new Point(3, 6);
            queryLabel.Name = "queryLabel";
            queryLabel.Size = new Size(62, 15);
            queryLabel.TabIndex = 0;
            queryLabel.Text = "Find what:";
            // 
            // queryTextBox
            // 
            queryTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            queryTextBox.Location = new Point(89, 2);
            queryTextBox.Margin = new Padding(3, 2, 3, 2);
            queryTextBox.Name = "queryTextBox";
            queryTextBox.Size = new Size(266, 23);
            queryTextBox.TabIndex = 1;
            // 
            // caseSensitiveCheckBox
            // 
            caseSensitiveCheckBox.AutoSize = true;
            caseSensitiveCheckBox.Location = new Point(89, 29);
            caseSensitiveCheckBox.Margin = new Padding(3, 2, 3, 2);
            caseSensitiveCheckBox.Name = "caseSensitiveCheckBox";
            caseSensitiveCheckBox.Size = new Size(86, 19);
            caseSensitiveCheckBox.TabIndex = 2;
            caseSensitiveCheckBox.Text = "Match case";
            caseSensitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // wholeWordCheckBox
            // 
            wholeWordCheckBox.AutoSize = true;
            wholeWordCheckBox.Location = new Point(89, 52);
            wholeWordCheckBox.Margin = new Padding(3, 2, 3, 2);
            wholeWordCheckBox.Name = "wholeWordCheckBox";
            wholeWordCheckBox.Size = new Size(151, 19);
            wholeWordCheckBox.TabIndex = 3;
            wholeWordCheckBox.Text = "Match whole word only";
            wholeWordCheckBox.UseVisualStyleBackColor = true;
            // 
            // interpretEscapesCheckBox
            // 
            interpretEscapesCheckBox.AutoSize = true;
            interpretEscapesCheckBox.Location = new Point(89, 75);
            interpretEscapesCheckBox.Margin = new Padding(3, 2, 3, 2);
            interpretEscapesCheckBox.Name = "interpretEscapesCheckBox";
            interpretEscapesCheckBox.Size = new Size(168, 19);
            interpretEscapesCheckBox.TabIndex = 4;
            interpretEscapesCheckBox.Text = "Interpret escape sequences";
            interpretEscapesCheckBox.UseVisualStyleBackColor = true;
            // 
            // wrapAroundCheckBox
            // 
            wrapAroundCheckBox.AutoSize = true;
            wrapAroundCheckBox.Location = new Point(89, 98);
            wrapAroundCheckBox.Margin = new Padding(3, 2, 3, 2);
            wrapAroundCheckBox.Name = "wrapAroundCheckBox";
            wrapAroundCheckBox.Size = new Size(189, 19);
            wrapAroundCheckBox.TabIndex = 5;
            wrapAroundCheckBox.Text = "Wrap around at document end";
            wrapAroundCheckBox.UseVisualStyleBackColor = true;
            // 
            // escapeModeLabel
            // 
            escapeModeLabel.Anchor = AnchorStyles.Left;
            escapeModeLabel.AutoSize = true;
            escapeModeLabel.Location = new Point(3, 125);
            escapeModeLabel.Name = "escapeModeLabel";
            escapeModeLabel.Size = new Size(80, 15);
            escapeModeLabel.TabIndex = 6;
            escapeModeLabel.Text = "Escape mode:";
            // 
            // escapeModeComboBox
            // 
            escapeModeComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            escapeModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            escapeModeComboBox.FormattingEnabled = true;
            escapeModeComboBox.Location = new Point(89, 121);
            escapeModeComboBox.Margin = new Padding(3, 2, 3, 2);
            escapeModeComboBox.Name = "escapeModeComboBox";
            escapeModeComboBox.Size = new Size(266, 23);
            escapeModeComboBox.TabIndex = 7;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoSize = true;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(findButton);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new Point(89, 148);
            buttonPanel.Margin = new Padding(3, 2, 3, 2);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(266, 31);
            buttonPanel.TabIndex = 8;
            buttonPanel.WrapContents = false;
            // 
            // cancelButton
            // 
            cancelButton.AutoSize = true;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(181, 2);
            cancelButton.Margin = new Padding(3, 2, 3, 2);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(82, 25);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // findButton
            // 
            findButton.AutoSize = true;
            findButton.DialogResult = DialogResult.OK;
            findButton.Location = new Point(111, 2);
            findButton.Margin = new Padding(3, 2, 3, 2);
            findButton.Name = "findButton";
            findButton.Size = new Size(64, 25);
            findButton.TabIndex = 0;
            findButton.Text = "Find";
            findButton.UseVisualStyleBackColor = true;
            // 
            // layoutPanel
            // 
            layoutPanel.ColumnCount = 2;
            layoutPanel.ColumnStyles.Add(new ColumnStyle());
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutPanel.Controls.Add(queryLabel, 0, 0);
            layoutPanel.Controls.Add(queryTextBox, 1, 0);
            layoutPanel.Controls.Add(caseSensitiveCheckBox, 1, 1);
            layoutPanel.Controls.Add(wholeWordCheckBox, 1, 2);
            layoutPanel.Controls.Add(interpretEscapesCheckBox, 1, 3);
            layoutPanel.Controls.Add(wrapAroundCheckBox, 1, 4);
            layoutPanel.Controls.Add(escapeModeLabel, 0, 5);
            layoutPanel.Controls.Add(escapeModeComboBox, 1, 5);
            layoutPanel.Controls.Add(buttonPanel, 1, 6);
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.Location = new Point(10, 9);
            layoutPanel.Margin = new Padding(3, 2, 3, 2);
            layoutPanel.Name = "layoutPanel";
            layoutPanel.RowCount = 7;
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPanel.Size = new Size(358, 181);
            layoutPanel.TabIndex = 0;
            // 
            // FindDialog
            // 
            AcceptButton = findButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(378, 199);
            Controls.Add(layoutPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FindDialog";
            Padding = new Padding(10, 9, 10, 9);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Find";
            buttonPanel.ResumeLayout(false);
            buttonPanel.PerformLayout();
            layoutPanel.ResumeLayout(false);
            layoutPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label queryLabel;
        private TextBox queryTextBox;
        private CheckBox caseSensitiveCheckBox;
        private CheckBox wholeWordCheckBox;
        private CheckBox interpretEscapesCheckBox;
        private CheckBox wrapAroundCheckBox;
        private Label escapeModeLabel;
        private ComboBox escapeModeComboBox;
        private FlowLayoutPanel buttonPanel;
        private Button cancelButton;
        private Button findButton;
        private TableLayoutPanel layoutPanel;
    }
}
