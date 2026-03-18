namespace MarkdownPad;

partial class InsertProgressDialog
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        layoutPanel = new TableLayoutPanel();
        infoLabel = new Label();
        textLabel = new Label();
        textTextBox = new TextBox();
        percentLabel = new Label();
        percentNumericUpDown = new NumericUpDown();
        borderColorLabel = new Label();
        borderColorPanel = new TableLayoutPanel();
        borderColorTextBox = new TextBox();
        borderColorPreviewPanel = new Panel();
        borderColorButton = new Button();
        barColorLabel = new Label();
        barColorPanel = new TableLayoutPanel();
        barColorTextBox = new TextBox();
        barColorPreviewPanel = new Panel();
        barColorButton = new Button();
        previewLabel = new Label();
        previewTextBox = new TextBox();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        insertButton = new Button();
        layoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)percentNumericUpDown).BeginInit();
        borderColorPanel.SuspendLayout();
        barColorPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // layoutPanel
        // 
        layoutPanel.ColumnCount = 2;
        layoutPanel.ColumnStyles.Add(new ColumnStyle());
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutPanel.Controls.Add(infoLabel, 0, 0);
        layoutPanel.Controls.Add(textLabel, 0, 1);
        layoutPanel.Controls.Add(textTextBox, 1, 1);
        layoutPanel.Controls.Add(percentLabel, 0, 2);
        layoutPanel.Controls.Add(percentNumericUpDown, 1, 2);
        layoutPanel.Controls.Add(borderColorLabel, 0, 3);
        layoutPanel.Controls.Add(borderColorPanel, 1, 3);
        layoutPanel.Controls.Add(barColorLabel, 0, 4);
        layoutPanel.Controls.Add(barColorPanel, 1, 4);
        layoutPanel.Controls.Add(previewLabel, 0, 5);
        layoutPanel.Controls.Add(previewTextBox, 1, 5);
        layoutPanel.Controls.Add(buttonPanel, 0, 6);
        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.Location = new Point(10, 9);
        layoutPanel.Name = "layoutPanel";
        layoutPanel.RowCount = 7;
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.Size = new Size(654, 285);
        layoutPanel.TabIndex = 0;
        // 
        // infoLabel
        // 
        infoLabel.AutoSize = true;
        layoutPanel.SetColumnSpan(infoLabel, 2);
        infoLabel.Dock = DockStyle.Fill;
        infoLabel.Location = new Point(3, 0);
        infoLabel.Name = "infoLabel";
        infoLabel.Size = new Size(648, 15);
        infoLabel.TabIndex = 0;
        infoLabel.Text = "Insert an inline progress bar with percentage, label text, border color, and bar color.";
        // 
        // textLabel
        // 
        textLabel.Anchor = AnchorStyles.Left;
        textLabel.AutoSize = true;
        textLabel.Location = new Point(3, 22);
        textLabel.Name = "textLabel";
        textLabel.Size = new Size(28, 15);
        textLabel.TabIndex = 1;
        textLabel.Text = "Text";
        // 
        // textTextBox
        // 
        textTextBox.Dock = DockStyle.Fill;
        textTextBox.Location = new Point(91, 18);
        textTextBox.Name = "textTextBox";
        textTextBox.Size = new Size(560, 23);
        textTextBox.TabIndex = 0;
        // 
        // percentLabel
        // 
        percentLabel.Anchor = AnchorStyles.Left;
        percentLabel.AutoSize = true;
        percentLabel.Location = new Point(3, 51);
        percentLabel.Name = "percentLabel";
        percentLabel.Size = new Size(51, 15);
        percentLabel.TabIndex = 3;
        percentLabel.Text = "Percent";
        // 
        // percentNumericUpDown
        // 
        percentNumericUpDown.Location = new Point(91, 47);
        percentNumericUpDown.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
        percentNumericUpDown.Name = "percentNumericUpDown";
        percentNumericUpDown.Size = new Size(90, 23);
        percentNumericUpDown.TabIndex = 1;
        // 
        // borderColorLabel
        // 
        borderColorLabel.Anchor = AnchorStyles.Left;
        borderColorLabel.AutoSize = true;
        borderColorLabel.Location = new Point(3, 81);
        borderColorLabel.Name = "borderColorLabel";
        borderColorLabel.Size = new Size(71, 15);
        borderColorLabel.TabIndex = 5;
        borderColorLabel.Text = "Border color";
        // 
        // borderColorPanel
        // 
        borderColorPanel.ColumnCount = 3;
        borderColorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        borderColorPanel.ColumnStyles.Add(new ColumnStyle());
        borderColorPanel.ColumnStyles.Add(new ColumnStyle());
        borderColorPanel.Controls.Add(borderColorTextBox, 0, 0);
        borderColorPanel.Controls.Add(borderColorPreviewPanel, 1, 0);
        borderColorPanel.Controls.Add(borderColorButton, 2, 0);
        borderColorPanel.Dock = DockStyle.Fill;
        borderColorPanel.Location = new Point(91, 76);
        borderColorPanel.Margin = new Padding(3, 0, 3, 3);
        borderColorPanel.Name = "borderColorPanel";
        borderColorPanel.RowCount = 1;
        borderColorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        borderColorPanel.Size = new Size(560, 24);
        borderColorPanel.TabIndex = 2;
        // 
        // borderColorTextBox
        // 
        borderColorTextBox.Dock = DockStyle.Fill;
        borderColorTextBox.Location = new Point(3, 0);
        borderColorTextBox.Margin = new Padding(3, 0, 3, 0);
        borderColorTextBox.Name = "borderColorTextBox";
        borderColorTextBox.ReadOnly = true;
        borderColorTextBox.Size = new Size(456, 23);
        borderColorTextBox.TabIndex = 0;
        // 
        // borderColorPreviewPanel
        // 
        borderColorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
        borderColorPreviewPanel.Location = new Point(465, 1);
        borderColorPreviewPanel.Margin = new Padding(3, 1, 3, 1);
        borderColorPreviewPanel.Name = "borderColorPreviewPanel";
        borderColorPreviewPanel.Size = new Size(28, 22);
        borderColorPreviewPanel.TabIndex = 1;
        // 
        // borderColorButton
        // 
        borderColorButton.AutoSize = true;
        borderColorButton.Location = new Point(499, 0);
        borderColorButton.Margin = new Padding(3, 0, 0, 0);
        borderColorButton.Name = "borderColorButton";
        borderColorButton.Size = new Size(61, 24);
        borderColorButton.TabIndex = 2;
        borderColorButton.Text = "Choose";
        borderColorButton.UseVisualStyleBackColor = true;
        // 
        // barColorLabel
        // 
        barColorLabel.Anchor = AnchorStyles.Left;
        barColorLabel.AutoSize = true;
        barColorLabel.Location = new Point(3, 106);
        barColorLabel.Name = "barColorLabel";
        barColorLabel.Size = new Size(56, 15);
        barColorLabel.TabIndex = 7;
        barColorLabel.Text = "Bar color";
        // 
        // barColorPanel
        // 
        barColorPanel.ColumnCount = 3;
        barColorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        barColorPanel.ColumnStyles.Add(new ColumnStyle());
        barColorPanel.ColumnStyles.Add(new ColumnStyle());
        barColorPanel.Controls.Add(barColorTextBox, 0, 0);
        barColorPanel.Controls.Add(barColorPreviewPanel, 1, 0);
        barColorPanel.Controls.Add(barColorButton, 2, 0);
        barColorPanel.Dock = DockStyle.Fill;
        barColorPanel.Location = new Point(91, 103);
        barColorPanel.Margin = new Padding(3, 0, 3, 3);
        barColorPanel.Name = "barColorPanel";
        barColorPanel.RowCount = 1;
        barColorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        barColorPanel.Size = new Size(560, 24);
        barColorPanel.TabIndex = 3;
        // 
        // barColorTextBox
        // 
        barColorTextBox.Dock = DockStyle.Fill;
        barColorTextBox.Location = new Point(3, 0);
        barColorTextBox.Margin = new Padding(3, 0, 3, 0);
        barColorTextBox.Name = "barColorTextBox";
        barColorTextBox.ReadOnly = true;
        barColorTextBox.Size = new Size(456, 23);
        barColorTextBox.TabIndex = 0;
        // 
        // barColorPreviewPanel
        // 
        barColorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
        barColorPreviewPanel.Location = new Point(465, 1);
        barColorPreviewPanel.Margin = new Padding(3, 1, 3, 1);
        barColorPreviewPanel.Name = "barColorPreviewPanel";
        barColorPreviewPanel.Size = new Size(28, 22);
        barColorPreviewPanel.TabIndex = 1;
        // 
        // barColorButton
        // 
        barColorButton.AutoSize = true;
        barColorButton.Location = new Point(499, 0);
        barColorButton.Margin = new Padding(3, 0, 0, 0);
        barColorButton.Name = "barColorButton";
        barColorButton.Size = new Size(61, 24);
        barColorButton.TabIndex = 2;
        barColorButton.Text = "Choose";
        barColorButton.UseVisualStyleBackColor = true;
        // 
        // previewLabel
        // 
        previewLabel.Anchor = AnchorStyles.Left;
        previewLabel.AutoSize = true;
        previewLabel.Location = new Point(3, 159);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(48, 15);
        previewLabel.TabIndex = 9;
        previewLabel.Text = "Preview";
        // 
        // previewTextBox
        // 
        previewTextBox.Dock = DockStyle.Fill;
        previewTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        previewTextBox.Location = new Point(91, 133);
        previewTextBox.Multiline = true;
        previewTextBox.Name = "previewTextBox";
        previewTextBox.ReadOnly = true;
        previewTextBox.ScrollBars = ScrollBars.Vertical;
        previewTextBox.Size = new Size(560, 86);
        previewTextBox.TabIndex = 4;
        // 
        // buttonPanel
        // 
        buttonPanel.AutoSize = true;
        layoutPanel.SetColumnSpan(buttonPanel, 2);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(insertButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(3, 225);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(648, 57);
        buttonPanel.TabIndex = 5;
        buttonPanel.WrapContents = false;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(563, 0);
        cancelButton.Margin = new Padding(3, 0, 3, 0);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(82, 25);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // insertButton
        // 
        insertButton.AutoSize = true;
        insertButton.DialogResult = DialogResult.OK;
        insertButton.Location = new Point(495, 0);
        insertButton.Margin = new Padding(3, 0, 3, 0);
        insertButton.Name = "insertButton";
        insertButton.Size = new Size(62, 25);
        insertButton.TabIndex = 0;
        insertButton.Text = "Insert";
        insertButton.UseVisualStyleBackColor = true;
        // 
        // InsertProgressDialog
        // 
        AcceptButton = insertButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(674, 303);
        Controls.Add(layoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InsertProgressDialog";
        Padding = new Padding(10, 9, 10, 9);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Insert Progress";
        layoutPanel.ResumeLayout(false);
        layoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)percentNumericUpDown).EndInit();
        borderColorPanel.ResumeLayout(false);
        borderColorPanel.PerformLayout();
        barColorPanel.ResumeLayout(false);
        barColorPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layoutPanel;
    private Label infoLabel;
    private Label textLabel;
    private TextBox textTextBox;
    private Label percentLabel;
    private NumericUpDown percentNumericUpDown;
    private Label borderColorLabel;
    private TableLayoutPanel borderColorPanel;
    private TextBox borderColorTextBox;
    private Panel borderColorPreviewPanel;
    private Button borderColorButton;
    private Label barColorLabel;
    private TableLayoutPanel barColorPanel;
    private TextBox barColorTextBox;
    private Panel barColorPreviewPanel;
    private Button barColorButton;
    private Label previewLabel;
    private TextBox previewTextBox;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button insertButton;
}
