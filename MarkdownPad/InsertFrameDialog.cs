using System.Drawing;

namespace MarkdownPad;

internal sealed partial class InsertFrameDialog : Form
{
    private Color _borderColor;
    private Color _fillColor;

    public InsertFrameDialog(string? initialText = null, Color borderColor = default, Color fillColor = default)
    {
        _borderColor = borderColor.IsEmpty ? Color.FromArgb(47, 93, 255) : borderColor;
        _fillColor = fillColor.IsEmpty ? Color.FromArgb(238, 243, 255) : fillColor;

        InitializeComponent();
        UpdateColorControls();

        contentTextBox.Text = initialText ?? string.Empty;
        contentTextBox.TextChanged += (_, _) => UpdatePreview();
        borderColorButton.Click += (_, _) => PickBorderColor();
        fillColorButton.Click += (_, _) => PickFillColor();

        UpdatePreview();
    }

    public string GeneratedMarkdown { get; private set; } = string.Empty;
    public Color BorderColor => _borderColor;
    public Color FillColor => _fillColor;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        contentTextBox.Focus();
        contentTextBox.SelectAll();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            GeneratedMarkdown = BuildMarkdown();
            if (string.IsNullOrWhiteSpace(GeneratedMarkdown))
            {
                MessageBox.Show(
                    this,
                    "Please enter frame content.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }

    private void PickBorderColor()
    {
        if (!TryPickColor(_borderColor, out Color selected))
            return;

        _borderColor = selected;
        UpdateColorControls();
        UpdatePreview();
    }

    private void PickFillColor()
    {
        if (!TryPickColor(_fillColor, out Color selected))
            return;

        _fillColor = selected;
        UpdateColorControls();
        UpdatePreview();
    }

    private bool TryPickColor(Color initialColor, out Color color)
    {
        using var dialog = new ColorDialog
        {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
            Color = initialColor
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            color = initialColor;
            return false;
        }

        color = dialog.Color;
        return true;
    }

    private void UpdateColorControls()
    {
        borderColorTextBox.Text = ColorToMarkdownHex(_borderColor);
        fillColorTextBox.Text = ColorToMarkdownHex(_fillColor);
        borderColorPreviewPanel.BackColor = _borderColor;
        fillColorPreviewPanel.BackColor = _fillColor;
    }

    private void UpdatePreview()
    {
        previewTextBox.Text = BuildMarkdown();
        insertButton.Enabled = !string.IsNullOrWhiteSpace(contentTextBox.Text);
    }

    private string BuildMarkdown()
    {
        string normalized = NormalizeContent(contentTextBox.Text);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        string borderHex = ColorToMarkdownHex(_borderColor);
        string fillHex = ColorToMarkdownHex(_fillColor);

        if (normalized.IndexOf('\n') < 0)
            return $"![FRAME:{borderHex}:{fillHex}]({normalized})";

        string newline = Environment.NewLine;
        string blockContent = normalized.Replace("\n", newline, StringComparison.Ordinal);
        return $"![FRAME:{borderHex}:{fillHex}]({newline}{blockContent}{newline})";
    }

    private static string NormalizeContent(string? content)
    {
        return (content ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static string ColorToMarkdownHex(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
