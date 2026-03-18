namespace MarkdownPad;

partial class InsertFrameDialog
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
        contentLabel = new Label();
        contentTextBox = new TextBox();
        borderColorLabel = new Label();
        borderColorPanel = new TableLayoutPanel();
        borderColorTextBox = new TextBox();
        borderColorPreviewPanel = new Panel();
        borderColorButton = new Button();
        fillColorLabel = new Label();
        fillColorPanel = new TableLayoutPanel();
        fillColorTextBox = new TextBox();
        fillColorPreviewPanel = new Panel();
        fillColorButton = new Button();
        previewLabel = new Label();
        previewTextBox = new TextBox();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        insertButton = new Button();
        layoutPanel.SuspendLayout();
        borderColorPanel.SuspendLayout();
        fillColorPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // layoutPanel
        // 
        layoutPanel.ColumnCount = 2;
        layoutPanel.ColumnStyles.Add(new ColumnStyle());
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutPanel.Controls.Add(infoLabel, 0, 0);
        layoutPanel.Controls.Add(contentLabel, 0, 1);
        layoutPanel.Controls.Add(contentTextBox, 1, 1);
        layoutPanel.Controls.Add(borderColorLabel, 0, 2);
        layoutPanel.Controls.Add(borderColorPanel, 1, 2);
        layoutPanel.Controls.Add(fillColorLabel, 0, 3);
        layoutPanel.Controls.Add(fillColorPanel, 1, 3);
        layoutPanel.Controls.Add(previewLabel, 0, 4);
        layoutPanel.Controls.Add(previewTextBox, 1, 4);
        layoutPanel.Controls.Add(buttonPanel, 0, 5);
        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.Location = new Point(10, 9);
        layoutPanel.Name = "layoutPanel";
        layoutPanel.RowCount = 6;
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.Size = new Size(654, 379);
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
        infoLabel.Text = "Insert an inline frame or a multi-line frame block. Multi-line content is inserted as a frame block.";
        // 
        // contentLabel
        // 
        contentLabel.Anchor = AnchorStyles.Left;
        contentLabel.AutoSize = true;
        contentLabel.Location = new Point(3, 67);
        contentLabel.Name = "contentLabel";
        contentLabel.Size = new Size(50, 15);
        contentLabel.TabIndex = 1;
        contentLabel.Text = "Content";
        // 
        // contentTextBox
        // 
        contentTextBox.AcceptsReturn = true;
        contentTextBox.AcceptsTab = true;
        contentTextBox.Dock = DockStyle.Fill;
        contentTextBox.Location = new Point(90, 18);
        contentTextBox.Multiline = true;
        contentTextBox.Name = "contentTextBox";
        contentTextBox.ScrollBars = ScrollBars.Vertical;
        contentTextBox.Size = new Size(561, 114);
        contentTextBox.TabIndex = 0;
        // 
        // borderColorLabel
        // 
        borderColorLabel.Anchor = AnchorStyles.Left;
        borderColorLabel.AutoSize = true;
        borderColorLabel.Location = new Point(3, 142);
        borderColorLabel.Name = "borderColorLabel";
        borderColorLabel.Size = new Size(71, 15);
        borderColorLabel.TabIndex = 3;
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
        borderColorPanel.Location = new Point(90, 138);
        borderColorPanel.Name = "borderColorPanel";
        borderColorPanel.RowCount = 1;
        borderColorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        borderColorPanel.Size = new Size(561, 24);
        borderColorPanel.TabIndex = 1;
        // 
        // borderColorTextBox
        // 
        borderColorTextBox.Dock = DockStyle.Fill;
        borderColorTextBox.Location = new Point(3, 0);
        borderColorTextBox.Margin = new Padding(3, 0, 3, 0);
        borderColorTextBox.Name = "borderColorTextBox";
        borderColorTextBox.ReadOnly = true;
        borderColorTextBox.Size = new Size(457, 23);
        borderColorTextBox.TabIndex = 0;
        // 
        // borderColorPreviewPanel
        // 
        borderColorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
        borderColorPreviewPanel.Location = new Point(466, 1);
        borderColorPreviewPanel.Margin = new Padding(3, 1, 3, 1);
        borderColorPreviewPanel.Name = "borderColorPreviewPanel";
        borderColorPreviewPanel.Size = new Size(28, 22);
        borderColorPreviewPanel.TabIndex = 1;
        // 
        // borderColorButton
        // 
        borderColorButton.AutoSize = true;
        borderColorButton.Location = new Point(500, 0);
        borderColorButton.Margin = new Padding(3, 0, 0, 0);
        borderColorButton.Name = "borderColorButton";
        borderColorButton.Size = new Size(61, 24);
        borderColorButton.TabIndex = 2;
        borderColorButton.Text = "Choose";
        borderColorButton.UseVisualStyleBackColor = true;
        // 
        // fillColorLabel
        // 
        fillColorLabel.Anchor = AnchorStyles.Left;
        fillColorLabel.AutoSize = true;
        fillColorLabel.Location = new Point(3, 168);
        fillColorLabel.Name = "fillColorLabel";
        fillColorLabel.Size = new Size(49, 15);
        fillColorLabel.TabIndex = 5;
        fillColorLabel.Text = "Fill color";
        // 
        // fillColorPanel
        // 
        fillColorPanel.ColumnCount = 3;
        fillColorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        fillColorPanel.ColumnStyles.Add(new ColumnStyle());
        fillColorPanel.ColumnStyles.Add(new ColumnStyle());
        fillColorPanel.Controls.Add(fillColorTextBox, 0, 0);
        fillColorPanel.Controls.Add(fillColorPreviewPanel, 1, 0);
        fillColorPanel.Controls.Add(fillColorButton, 2, 0);
        fillColorPanel.Dock = DockStyle.Fill;
        fillColorPanel.Location = new Point(90, 165);
        fillColorPanel.Margin = new Padding(3, 0, 3, 3);
        fillColorPanel.Name = "fillColorPanel";
        fillColorPanel.RowCount = 1;
        fillColorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        fillColorPanel.Size = new Size(561, 24);
        fillColorPanel.TabIndex = 2;
        // 
        // fillColorTextBox
        // 
        fillColorTextBox.Dock = DockStyle.Fill;
        fillColorTextBox.Location = new Point(3, 0);
        fillColorTextBox.Margin = new Padding(3, 0, 3, 0);
        fillColorTextBox.Name = "fillColorTextBox";
        fillColorTextBox.ReadOnly = true;
        fillColorTextBox.Size = new Size(457, 23);
        fillColorTextBox.TabIndex = 0;
        // 
        // fillColorPreviewPanel
        // 
        fillColorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
        fillColorPreviewPanel.Location = new Point(466, 1);
        fillColorPreviewPanel.Margin = new Padding(3, 1, 3, 1);
        fillColorPreviewPanel.Name = "fillColorPreviewPanel";
        fillColorPreviewPanel.Size = new Size(28, 22);
        fillColorPreviewPanel.TabIndex = 1;
        // 
        // fillColorButton
        // 
        fillColorButton.AutoSize = true;
        fillColorButton.Location = new Point(500, 0);
        fillColorButton.Margin = new Padding(3, 0, 0, 0);
        fillColorButton.Name = "fillColorButton";
        fillColorButton.Size = new Size(61, 24);
        fillColorButton.TabIndex = 2;
        fillColorButton.Text = "Choose";
        fillColorButton.UseVisualStyleBackColor = true;
        // 
        // previewLabel
        // 
        previewLabel.Anchor = AnchorStyles.Left;
        previewLabel.AutoSize = true;
        previewLabel.Location = new Point(3, 240);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(48, 15);
        previewLabel.TabIndex = 7;
        previewLabel.Text = "Preview";
        // 
        // previewTextBox
        // 
        previewTextBox.Dock = DockStyle.Fill;
        previewTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        previewTextBox.Location = new Point(90, 195);
        previewTextBox.Multiline = true;
        previewTextBox.Name = "previewTextBox";
        previewTextBox.ReadOnly = true;
        previewTextBox.ScrollBars = ScrollBars.Vertical;
        previewTextBox.Size = new Size(561, 104);
        previewTextBox.TabIndex = 3;
        // 
        // buttonPanel
        // 
        buttonPanel.AutoSize = true;
        layoutPanel.SetColumnSpan(buttonPanel, 2);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(insertButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(3, 305);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(648, 71);
        buttonPanel.TabIndex = 4;
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
        // InsertFrameDialog
        // 
        AcceptButton = insertButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(674, 397);
        Controls.Add(layoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InsertFrameDialog";
        Padding = new Padding(10, 9, 10, 9);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Insert Frame";
        layoutPanel.ResumeLayout(false);
        layoutPanel.PerformLayout();
        borderColorPanel.ResumeLayout(false);
        borderColorPanel.PerformLayout();
        fillColorPanel.ResumeLayout(false);
        fillColorPanel.PerformLayout();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layoutPanel;
    private Label infoLabel;
    private Label contentLabel;
    private TextBox contentTextBox;
    private Label borderColorLabel;
    private TableLayoutPanel borderColorPanel;
    private TextBox borderColorTextBox;
    private Panel borderColorPreviewPanel;
    private Button borderColorButton;
    private Label fillColorLabel;
    private TableLayoutPanel fillColorPanel;
    private TextBox fillColorTextBox;
    private Panel fillColorPreviewPanel;
    private Button fillColorButton;
    private Label previewLabel;
    private TextBox previewTextBox;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button insertButton;
}
