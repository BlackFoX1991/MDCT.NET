namespace MarkdownPad;

internal sealed partial class TableDesignerDialog : Form
{
    private const int MinimumColumnCount = 1;
    private const int MinimumRowCount = 2;

    public TableDesignerDialog()
    {
        InitializeComponent();

        ConfigureGrid();
        AddInitialStructure();
        UpdatePreview();
    }

    public string GeneratedMarkdown { get; private set; } = string.Empty;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (tableGrid.ColumnCount > 0 && tableGrid.RowCount > 0)
        {
            tableGrid.CurrentCell ??= tableGrid[0, 0];
            tableGrid.BeginEdit(selectAll: true);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        tableGrid.EndEdit();

        if (DialogResult == DialogResult.OK)
        {
            GeneratedMarkdown = BuildMarkdown();
            if (string.IsNullOrWhiteSpace(GeneratedMarkdown))
            {
                MessageBox.Show(
                    this,
                    "Please define at least one column and one body row.",
                    "Table Designer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }

    private void ConfigureGrid()
    {
        tableGrid.AutoGenerateColumns = false;
        tableGrid.AllowUserToAddRows = false;
        tableGrid.AllowUserToDeleteRows = false;
        tableGrid.AllowUserToResizeRows = false;
        tableGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        tableGrid.MultiSelect = false;
        tableGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        tableGrid.RowHeadersWidth = 88;

        tableGrid.CellValueChanged += (_, _) => UpdatePreview();
        tableGrid.CellEndEdit += (_, _) => UpdatePreview();
        tableGrid.RowsAdded += (_, _) =>
        {
            UpdateRowHeaders();
            UpdatePreview();
        };
        tableGrid.RowsRemoved += (_, _) =>
        {
            UpdateRowHeaders();
            UpdatePreview();
        };
        tableGrid.ColumnAdded += (_, _) => UpdatePreview();
        tableGrid.ColumnRemoved += (_, _) => UpdatePreview();

        addColumnButton.Click += (_, _) => AddColumn();
        removeColumnButton.Click += (_, _) => RemoveSelectedColumn();
        addRowButton.Click += (_, _) => AddRow();
        removeRowButton.Click += (_, _) => RemoveSelectedRow();
    }

    private void AddInitialStructure()
    {
        for (int column = 0; column < 3; column++)
            AddColumn();

        for (int row = 0; row < MinimumRowCount; row++)
            AddRow();
    }

    private void AddColumn()
    {
        int nextNumber = tableGrid.ColumnCount + 1;
        var column = new DataGridViewTextBoxColumn
        {
            Name = $"column{nextNumber}",
            HeaderText = $"Column {nextNumber}",
            SortMode = DataGridViewColumnSortMode.NotSortable
        };

        tableGrid.Columns.Add(column);
        UpdatePreview();
    }

    private void RemoveSelectedColumn()
    {
        if (tableGrid.ColumnCount <= MinimumColumnCount)
        {
            MessageBox.Show(
                this,
                "The table needs at least one column.",
                "Table Designer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        int targetIndex = tableGrid.CurrentCell?.ColumnIndex ?? (tableGrid.ColumnCount - 1);
        tableGrid.Columns.RemoveAt(targetIndex);
        RenumberColumns();
        UpdatePreview();
    }

    private void AddRow()
    {
        int rowIndex = tableGrid.Rows.Add();
        UpdateRowHeaders();

        if (tableGrid.ColumnCount > 0)
            tableGrid.CurrentCell = tableGrid[0, rowIndex];
    }

    private void RemoveSelectedRow()
    {
        if (tableGrid.RowCount <= MinimumRowCount)
        {
            MessageBox.Show(
                this,
                "The table needs at least one header row and one body row.",
                "Table Designer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        int targetIndex = tableGrid.CurrentCell?.RowIndex ?? (tableGrid.RowCount - 1);
        tableGrid.Rows.RemoveAt(targetIndex);
        UpdateRowHeaders();
        UpdatePreview();
    }

    private void RenumberColumns()
    {
        for (int index = 0; index < tableGrid.Columns.Count; index++)
        {
            DataGridViewColumn column = tableGrid.Columns[index];
            column.Name = $"column{index + 1}";
            column.HeaderText = $"Column {index + 1}";
        }
    }

    private void UpdateRowHeaders()
    {
        for (int rowIndex = 0; rowIndex < tableGrid.Rows.Count; rowIndex++)
        {
            DataGridViewRow row = tableGrid.Rows[rowIndex];
            row.HeaderCell.Value = rowIndex == 0
                ? "Header"
                : $"Row {rowIndex}";
        }
    }

    private void UpdatePreview()
    {
        previewTextBox.Text = BuildMarkdown();
    }

    private string BuildMarkdown()
    {
        if (tableGrid.ColumnCount < MinimumColumnCount || tableGrid.RowCount < MinimumRowCount)
            return string.Empty;

        var lines = new List<string>
        {
            BuildRow(0),
            "| " + string.Join(" | ", Enumerable.Repeat("---", tableGrid.ColumnCount)) + " |"
        };

        for (int rowIndex = 1; rowIndex < tableGrid.RowCount; rowIndex++)
            lines.Add(BuildRow(rowIndex));

        return string.Join(Environment.NewLine, lines);
    }

    private string BuildRow(int rowIndex)
    {
        var cells = new List<string>(tableGrid.ColumnCount);

        for (int columnIndex = 0; columnIndex < tableGrid.ColumnCount; columnIndex++)
        {
            string? value = Convert.ToString(tableGrid[columnIndex, rowIndex].Value);
            cells.Add(EscapeCell(value));
        }

        return "| " + string.Join(" | ", cells) + " |";
    }

    private static string EscapeCell(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);
    }
}
