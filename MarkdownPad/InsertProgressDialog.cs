using System.Drawing;

namespace MarkdownPad;

internal sealed partial class InsertProgressDialog : Form
{
    private Color _borderColor;
    private Color _barColor;

    public InsertProgressDialog(string? initialText = null, int initialPercent = 50, Color borderColor = default, Color barColor = default)
    {
        _borderColor = borderColor.IsEmpty ? Color.FromArgb(80, 120, 200) : borderColor;
        _barColor = barColor.IsEmpty ? Color.FromArgb(123, 201, 111) : barColor;

        InitializeComponent();

        textTextBox.Text = NormalizeText(initialText);
        percentNumericUpDown.Value = Math.Clamp(initialPercent, 0, 100);

        textTextBox.TextChanged += (_, _) => UpdatePreview();
        percentNumericUpDown.ValueChanged += (_, _) => UpdatePreview();
        borderColorButton.Click += (_, _) => PickBorderColor();
        barColorButton.Click += (_, _) => PickBarColor();

        UpdateColorControls();
        UpdatePreview();
    }

    public string GeneratedMarkdown { get; private set; } = string.Empty;
    public Color BorderColor => _borderColor;
    public Color BarColor => _barColor;
    public int ProgressPercent => (int)percentNumericUpDown.Value;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        textTextBox.Focus();
        textTextBox.SelectAll();
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
                    "Please enter progress text.",
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

    private void PickBarColor()
    {
        if (!TryPickColor(_barColor, out Color selected))
            return;

        _barColor = selected;
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
        barColorTextBox.Text = ColorToMarkdownHex(_barColor);
        borderColorPreviewPanel.BackColor = _borderColor;
        barColorPreviewPanel.BackColor = _barColor;
    }

    private void UpdatePreview()
    {
        previewTextBox.Text = BuildMarkdown();
        insertButton.Enabled = !string.IsNullOrWhiteSpace(textTextBox.Text);
    }

    private string BuildMarkdown()
    {
        string text = NormalizeText(textTextBox.Text);
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return $"![PROGRESS:{(int)percentNumericUpDown.Value}:{text}:{ColorToMarkdownHex(_borderColor)}:{ColorToMarkdownHex(_barColor)}]";
    }

    private static string NormalizeText(string? text)
    {
        return (text ?? string.Empty)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Trim();
    }

    private static string ColorToMarkdownHex(Color color)
        => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
