namespace MarkdownPad;

partial class InsertMediaDialog
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertMediaDialog));
        layoutPanel = new TableLayoutPanel();
        infoLabel = new Label();
        titleLabel = new Label();
        titleTextBox = new TextBox();
        targetLabel = new Label();
        targetPanel = new TableLayoutPanel();
        targetTextBox = new TextBox();
        browseButton = new Button();
        headingLabel = new Label();
        headingComboBox = new ComboBox();
        statusLabel = new Label();
        previewLabel = new Label();
        previewTextBox = new TextBox();
        mediaPreviewLabel = new Label();
        previewHostPanel = new Panel();
        previewPictureBox = new PictureBox();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        insertButton = new Button();
        layoutPanel.SuspendLayout();
        targetPanel.SuspendLayout();
        previewHostPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // layoutPanel
        // 
        layoutPanel.ColumnCount = 2;
        layoutPanel.ColumnStyles.Add(new ColumnStyle());
        layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutPanel.Controls.Add(infoLabel, 0, 0);
        layoutPanel.Controls.Add(titleLabel, 0, 1);
        layoutPanel.Controls.Add(titleTextBox, 1, 1);
        layoutPanel.Controls.Add(targetLabel, 0, 2);
        layoutPanel.Controls.Add(targetPanel, 1, 2);
        layoutPanel.Controls.Add(headingLabel, 0, 3);
        layoutPanel.Controls.Add(headingComboBox, 1, 3);
        layoutPanel.Controls.Add(statusLabel, 0, 4);
        layoutPanel.Controls.Add(previewLabel, 0, 5);
        layoutPanel.Controls.Add(previewTextBox, 1, 5);
        layoutPanel.Controls.Add(mediaPreviewLabel, 0, 6);
        layoutPanel.Controls.Add(previewHostPanel, 1, 6);
        layoutPanel.Controls.Add(buttonPanel, 0, 7);
        layoutPanel.Dock = DockStyle.Fill;
        layoutPanel.Location = new Point(10, 9);
        layoutPanel.Name = "layoutPanel";
        layoutPanel.RowCount = 8;
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
        layoutPanel.RowStyles.Add(new RowStyle());
        layoutPanel.Size = new Size(644, 390);
        layoutPanel.TabIndex = 0;
        // 
        // infoLabel
        // 
        infoLabel.AutoSize = true;
        layoutPanel.SetColumnSpan(infoLabel, 2);
        infoLabel.Dock = DockStyle.Fill;
        infoLabel.Location = new Point(3, 0);
        infoLabel.Name = "infoLabel";
        infoLabel.Size = new Size(638, 15);
        infoLabel.TabIndex = 0;
        infoLabel.Text = "Insert markdown using a local path, a relative path, or a full http/https URL.";
        // 
        // titleLabel
        // 
        titleLabel.Anchor = AnchorStyles.Left;
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(3, 22);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(52, 15);
        titleLabel.TabIndex = 1;
        titleLabel.Text = "Title text";
        // 
        // titleTextBox
        // 
        titleTextBox.Dock = DockStyle.Fill;
        titleTextBox.Location = new Point(93, 18);
        titleTextBox.Name = "titleTextBox";
        titleTextBox.Size = new Size(548, 23);
        titleTextBox.TabIndex = 0;
        // 
        // targetLabel
        // 
        targetLabel.Anchor = AnchorStyles.Left;
        targetLabel.AutoSize = true;
        targetLabel.Location = new Point(3, 54);
        targetLabel.Name = "targetLabel";
        targetLabel.Size = new Size(67, 15);
        targetLabel.TabIndex = 3;
        targetLabel.Text = "Path or link";
        // 
        // targetPanel
        // 
        targetPanel.ColumnCount = 2;
        targetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        targetPanel.ColumnStyles.Add(new ColumnStyle());
        targetPanel.Controls.Add(targetTextBox, 0, 0);
        targetPanel.Controls.Add(browseButton, 1, 0);
        targetPanel.Dock = DockStyle.Fill;
        targetPanel.Location = new Point(93, 47);
        targetPanel.Name = "targetPanel";
        targetPanel.RowCount = 1;
        targetPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        targetPanel.Size = new Size(548, 29);
        targetPanel.TabIndex = 1;
        // 
        // targetTextBox
        // 
        targetTextBox.Dock = DockStyle.Fill;
        targetTextBox.Location = new Point(3, 3);
        targetTextBox.Name = "targetTextBox";
        targetTextBox.Size = new Size(461, 23);
        targetTextBox.TabIndex = 1;
        // 
        // browseButton
        // 
        browseButton.AutoSize = true;
        browseButton.Location = new Point(470, 2);
        browseButton.Margin = new Padding(3, 2, 3, 2);
        browseButton.Name = "browseButton";
        browseButton.Size = new Size(75, 25);
        browseButton.TabIndex = 2;
        browseButton.Text = "Browse...";
        browseButton.UseVisualStyleBackColor = true;
        // 
        // headingLabel
        // 
        headingLabel.Anchor = AnchorStyles.Left;
        headingLabel.AutoSize = true;
        headingLabel.Location = new Point(3, 86);
        headingLabel.Name = "headingLabel";
        headingLabel.Size = new Size(52, 15);
        headingLabel.TabIndex = 4;
        headingLabel.Text = "Heading";
        headingLabel.Visible = false;
        // 
        // headingComboBox
        // 
        headingComboBox.Dock = DockStyle.Fill;
        headingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        headingComboBox.FormattingEnabled = true;
        headingComboBox.Location = new Point(93, 82);
        headingComboBox.Name = "headingComboBox";
        headingComboBox.Size = new Size(548, 23);
        headingComboBox.TabIndex = 3;
        headingComboBox.Visible = false;
        // 
        // statusLabel
        // 
        statusLabel.AutoEllipsis = true;
        layoutPanel.SetColumnSpan(statusLabel, 2);
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.Location = new Point(3, 108);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(638, 18);
        statusLabel.TabIndex = 5;
        statusLabel.Text = "Enter a path or URL.";
        // 
        // previewLabel
        // 
        previewLabel.Anchor = AnchorStyles.Left;
        previewLabel.AutoSize = true;
        previewLabel.Location = new Point(3, 164);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(48, 15);
        previewLabel.TabIndex = 6;
        previewLabel.Text = "Preview";
        // 
        // previewTextBox
        // 
        previewTextBox.Dock = DockStyle.Fill;
        previewTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        previewTextBox.Location = new Point(93, 129);
        previewTextBox.Multiline = true;
        previewTextBox.Name = "previewTextBox";
        previewTextBox.ReadOnly = true;
        previewTextBox.ScrollBars = ScrollBars.Vertical;
        previewTextBox.Size = new Size(548, 86);
        previewTextBox.TabIndex = 4;
        // 
        // mediaPreviewLabel
        // 
        mediaPreviewLabel.Anchor = AnchorStyles.Left;
        mediaPreviewLabel.AutoSize = true;
        mediaPreviewLabel.Location = new Point(3, 295);
        mediaPreviewLabel.Name = "mediaPreviewLabel";
        mediaPreviewLabel.Size = new Size(84, 15);
        mediaPreviewLabel.TabIndex = 7;
        mediaPreviewLabel.Text = "Image preview";
        // 
        // previewHostPanel
        // 
        previewHostPanel.BackColor = Color.FromArgb(245, 247, 250);
        previewHostPanel.BorderStyle = BorderStyle.FixedSingle;
        previewHostPanel.Controls.Add(previewPictureBox);
        previewHostPanel.Dock = DockStyle.Fill;
        previewHostPanel.Location = new Point(93, 221);
        previewHostPanel.Name = "previewHostPanel";
        previewHostPanel.Padding = new Padding(10);
        previewHostPanel.Size = new Size(548, 164);
        previewHostPanel.TabIndex = 8;
        // 
        // previewPictureBox
        // 
        previewPictureBox.BackColor = Color.Transparent;
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.Location = new Point(10, 10);
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.Size = new Size(526, 142);
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.TabIndex = 0;
        previewPictureBox.TabStop = false;
        // 
        // buttonPanel
        // 
        buttonPanel.AutoSize = true;
        layoutPanel.SetColumnSpan(buttonPanel, 2);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(insertButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(3, 391);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Size = new Size(638, 25);
        buttonPanel.TabIndex = 5;
        buttonPanel.WrapContents = false;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(553, 0);
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
        insertButton.Location = new Point(485, 0);
        insertButton.Margin = new Padding(3, 0, 3, 0);
        insertButton.Name = "insertButton";
        insertButton.Size = new Size(62, 25);
        insertButton.TabIndex = 0;
        insertButton.Text = "Insert";
        insertButton.UseVisualStyleBackColor = true;
        // 
        // InsertMediaDialog
        // 
        AcceptButton = insertButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(664, 408);
        Controls.Add(layoutPanel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "InsertMediaDialog";
        Padding = new Padding(10, 9, 10, 9);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Insert";
        layoutPanel.ResumeLayout(false);
        layoutPanel.PerformLayout();
        targetPanel.ResumeLayout(false);
        targetPanel.PerformLayout();
        previewHostPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel layoutPanel;
    private Label infoLabel;
    private Label titleLabel;
    private TextBox titleTextBox;
    private Label targetLabel;
    private TableLayoutPanel targetPanel;
    private TextBox targetTextBox;
    private Button browseButton;
    private Label headingLabel;
    private ComboBox headingComboBox;
    private Label statusLabel;
    private Label previewLabel;
    private TextBox previewTextBox;
    private Label mediaPreviewLabel;
    private Panel previewHostPanel;
    private PictureBox previewPictureBox;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button insertButton;
}
