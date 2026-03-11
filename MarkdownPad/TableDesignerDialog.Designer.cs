namespace MarkdownPad
{
    partial class TableDesignerDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TableDesignerDialog));
            infoLabel = new Label();
            commandPanel = new FlowLayoutPanel();
            addColumnButton = new Button();
            removeColumnButton = new Button();
            addRowButton = new Button();
            removeRowButton = new Button();
            tableGrid = new DataGridView();
            previewLabel = new Label();
            previewTextBox = new TextBox();
            buttonPanel = new FlowLayoutPanel();
            cancelButton = new Button();
            insertButton = new Button();
            layoutPanel = new TableLayoutPanel();
            commandPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tableGrid).BeginInit();
            buttonPanel.SuspendLayout();
            layoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // infoLabel
            // 
            infoLabel.AutoSize = true;
            infoLabel.Dock = DockStyle.Fill;
            infoLabel.Location = new Point(3, 0);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new Size(667, 15);
            infoLabel.TabIndex = 0;
            infoLabel.Text = "The first row becomes the table header. Add or remove columns and rows as needed.";
            // 
            // commandPanel
            // 
            commandPanel.AutoSize = true;
            commandPanel.Controls.Add(addColumnButton);
            commandPanel.Controls.Add(removeColumnButton);
            commandPanel.Controls.Add(addRowButton);
            commandPanel.Controls.Add(removeRowButton);
            commandPanel.Dock = DockStyle.Fill;
            commandPanel.Location = new Point(3, 17);
            commandPanel.Margin = new Padding(3, 2, 3, 2);
            commandPanel.Name = "commandPanel";
            commandPanel.Size = new Size(667, 29);
            commandPanel.TabIndex = 1;
            // 
            // addColumnButton
            // 
            addColumnButton.AutoSize = true;
            addColumnButton.Location = new Point(3, 2);
            addColumnButton.Margin = new Padding(3, 2, 3, 2);
            addColumnButton.Name = "addColumnButton";
            addColumnButton.Size = new Size(100, 25);
            addColumnButton.TabIndex = 0;
            addColumnButton.Text = "Add Column";
            addColumnButton.UseVisualStyleBackColor = true;
            // 
            // removeColumnButton
            // 
            removeColumnButton.AutoSize = true;
            removeColumnButton.Location = new Point(109, 2);
            removeColumnButton.Margin = new Padding(3, 2, 3, 2);
            removeColumnButton.Name = "removeColumnButton";
            removeColumnButton.Size = new Size(122, 25);
            removeColumnButton.TabIndex = 1;
            removeColumnButton.Text = "Remove Column";
            removeColumnButton.UseVisualStyleBackColor = true;
            // 
            // addRowButton
            // 
            addRowButton.AutoSize = true;
            addRowButton.Location = new Point(237, 2);
            addRowButton.Margin = new Padding(3, 2, 3, 2);
            addRowButton.Name = "addRowButton";
            addRowButton.Size = new Size(78, 25);
            addRowButton.TabIndex = 2;
            addRowButton.Text = "Add Row";
            addRowButton.UseVisualStyleBackColor = true;
            // 
            // removeRowButton
            // 
            removeRowButton.AutoSize = true;
            removeRowButton.Location = new Point(321, 2);
            removeRowButton.Margin = new Padding(3, 2, 3, 2);
            removeRowButton.Name = "removeRowButton";
            removeRowButton.Size = new Size(101, 25);
            removeRowButton.TabIndex = 3;
            removeRowButton.Text = "Remove Row";
            removeRowButton.UseVisualStyleBackColor = true;
            // 
            // tableGrid
            // 
            tableGrid.BackgroundColor = SystemColors.Window;
            tableGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tableGrid.Dock = DockStyle.Fill;
            tableGrid.Location = new Point(3, 50);
            tableGrid.Margin = new Padding(3, 2, 3, 2);
            tableGrid.Name = "tableGrid";
            tableGrid.RowHeadersWidth = 51;
            tableGrid.Size = new Size(667, 227);
            tableGrid.TabIndex = 2;
            // 
            // previewLabel
            // 
            previewLabel.AutoSize = true;
            previewLabel.Dock = DockStyle.Fill;
            previewLabel.Location = new Point(3, 279);
            previewLabel.Name = "previewLabel";
            previewLabel.Size = new Size(667, 15);
            previewLabel.TabIndex = 3;
            previewLabel.Text = "Markdown Preview";
            // 
            // previewTextBox
            // 
            previewTextBox.Dock = DockStyle.Fill;
            previewTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            previewTextBox.Location = new Point(3, 296);
            previewTextBox.Margin = new Padding(3, 2, 3, 2);
            previewTextBox.Multiline = true;
            previewTextBox.Name = "previewTextBox";
            previewTextBox.ReadOnly = true;
            previewTextBox.ScrollBars = ScrollBars.Vertical;
            previewTextBox.Size = new Size(667, 93);
            previewTextBox.TabIndex = 4;
            // 
            // buttonPanel
            // 
            buttonPanel.AutoSize = true;
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(insertButton);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Location = new Point(3, 393);
            buttonPanel.Margin = new Padding(3, 2, 3, 2);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(667, 29);
            buttonPanel.TabIndex = 5;
            buttonPanel.WrapContents = false;
            // 
            // cancelButton
            // 
            cancelButton.AutoSize = true;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(582, 2);
            cancelButton.Margin = new Padding(3, 2, 3, 2);
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
            insertButton.Location = new Point(514, 2);
            insertButton.Margin = new Padding(3, 2, 3, 2);
            insertButton.Name = "insertButton";
            insertButton.Size = new Size(62, 25);
            insertButton.TabIndex = 0;
            insertButton.Text = "Insert";
            insertButton.UseVisualStyleBackColor = true;
            // 
            // layoutPanel
            // 
            layoutPanel.ColumnCount = 1;
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutPanel.Controls.Add(infoLabel, 0, 0);
            layoutPanel.Controls.Add(commandPanel, 0, 1);
            layoutPanel.Controls.Add(tableGrid, 0, 2);
            layoutPanel.Controls.Add(previewLabel, 0, 3);
            layoutPanel.Controls.Add(previewTextBox, 0, 4);
            layoutPanel.Controls.Add(buttonPanel, 0, 5);
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.Location = new Point(10, 9);
            layoutPanel.Margin = new Padding(3, 2, 3, 2);
            layoutPanel.Name = "layoutPanel";
            layoutPanel.RowCount = 6;
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 97F));
            layoutPanel.RowStyles.Add(new RowStyle());
            layoutPanel.Size = new Size(673, 424);
            layoutPanel.TabIndex = 0;
            // 
            // TableDesignerDialog
            // 
            AcceptButton = insertButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(693, 442);
            Controls.Add(layoutPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TableDesignerDialog";
            Padding = new Padding(10, 9, 10, 9);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Table Designer";
            commandPanel.ResumeLayout(false);
            commandPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)tableGrid).EndInit();
            buttonPanel.ResumeLayout(false);
            buttonPanel.PerformLayout();
            layoutPanel.ResumeLayout(false);
            layoutPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label infoLabel;
        private FlowLayoutPanel commandPanel;
        private Button addColumnButton;
        private Button removeColumnButton;
        private Button addRowButton;
        private Button removeRowButton;
        private DataGridView tableGrid;
        private Label previewLabel;
        private TextBox previewTextBox;
        private FlowLayoutPanel buttonPanel;
        private Button cancelButton;
        private Button insertButton;
        private TableLayoutPanel layoutPanel;
    }
}
