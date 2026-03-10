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
            components = new System.ComponentModel.Container();
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
            queryLabel.Location = new Point(3, 15);
            queryLabel.Name = "queryLabel";
            queryLabel.Size = new Size(67, 20);
            queryLabel.TabIndex = 0;
            queryLabel.Text = "Find what:";
            // 
            // queryTextBox
            // 
            queryTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            queryTextBox.Location = new Point(99, 11);
            queryTextBox.Name = "queryTextBox";
            queryTextBox.Size = new Size(306, 27);
            queryTextBox.TabIndex = 1;
            // 
            // caseSensitiveCheckBox
            // 
            caseSensitiveCheckBox.AutoSize = true;
            caseSensitiveCheckBox.Location = new Point(99, 49);
            caseSensitiveCheckBox.Name = "caseSensitiveCheckBox";
            caseSensitiveCheckBox.Size = new Size(251, 24);
            caseSensitiveCheckBox.TabIndex = 2;
            caseSensitiveCheckBox.Text = "Match case";
            caseSensitiveCheckBox.UseVisualStyleBackColor = true;
            // 
            // wholeWordCheckBox
            // 
            wholeWordCheckBox.AutoSize = true;
            wholeWordCheckBox.Location = new Point(99, 79);
            wholeWordCheckBox.Name = "wholeWordCheckBox";
            wholeWordCheckBox.Size = new Size(137, 24);
            wholeWordCheckBox.TabIndex = 3;
            wholeWordCheckBox.Text = "Match whole word only";
            wholeWordCheckBox.UseVisualStyleBackColor = true;
            // 
            // interpretEscapesCheckBox
            // 
            interpretEscapesCheckBox.AutoSize = true;
            interpretEscapesCheckBox.Location = new Point(99, 109);
            interpretEscapesCheckBox.Name = "interpretEscapesCheckBox";
            interpretEscapesCheckBox.Size = new Size(240, 24);
            interpretEscapesCheckBox.TabIndex = 4;
            interpretEscapesCheckBox.Text = "Interpret escape sequences";
            interpretEscapesCheckBox.UseVisualStyleBackColor = true;
            // 
            // wrapAroundCheckBox
            // 
            wrapAroundCheckBox.AutoSize = true;
            wrapAroundCheckBox.Location = new Point(99, 139);
            wrapAroundCheckBox.Name = "wrapAroundCheckBox";
            wrapAroundCheckBox.Size = new Size(220, 24);
            wrapAroundCheckBox.TabIndex = 5;
            wrapAroundCheckBox.Text = "Wrap around at document end";
            wrapAroundCheckBox.UseVisualStyleBackColor = true;
            // 
            // escapeModeLabel
            // 
            escapeModeLabel.Anchor = AnchorStyles.Left;
            escapeModeLabel.AutoSize = true;
            escapeModeLabel.Location = new Point(3, 174);
            escapeModeLabel.Name = "escapeModeLabel";
            escapeModeLabel.Size = new Size(93, 20);
            escapeModeLabel.TabIndex = 6;
            escapeModeLabel.Text = "Escape mode:";
            // 
            // escapeModeComboBox
            // 
            escapeModeComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            escapeModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            escapeModeComboBox.FormattingEnabled = true;
            escapeModeComboBox.Location = new Point(99, 170);
            escapeModeComboBox.Name = "escapeModeComboBox";
            escapeModeComboBox.Size = new Size(306, 28);
            escapeModeComboBox.TabIndex = 7;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoSize = true;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(findButton);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new Point(99, 204);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(306, 6);
            buttonPanel.TabIndex = 8;
            buttonPanel.WrapContents = false;
            // 
            // cancelButton
            // 
            cancelButton.AutoSize = true;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(209, 3);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(94, 30);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // findButton
            // 
            findButton.AutoSize = true;
            findButton.DialogResult = DialogResult.OK;
            findButton.Location = new Point(130, 3);
            findButton.Name = "findButton";
            findButton.Size = new Size(73, 30);
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
            layoutPanel.Location = new Point(12, 12);
            layoutPanel.Name = "layoutPanel";
            layoutPanel.RowCount = 7;
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPanel.Size = new Size(408, 213);
            layoutPanel.TabIndex = 0;
            // 
            // FindDialog
            // 
            AcceptButton = findButton;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(432, 237);
            Controls.Add(layoutPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FindDialog";
            Padding = new Padding(12);
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
