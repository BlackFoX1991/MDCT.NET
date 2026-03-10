using MarkdownGdi;

namespace MarkdownPad;

internal sealed partial class FindDialog : Form
{
    public FindDialog(string? currentQuery, FindOptions currentOptions)
    {
        InitializeComponent();

        queryTextBox.Text = currentQuery ?? string.Empty;
        caseSensitiveCheckBox.Checked = currentOptions.CaseSensitive;
        wholeWordCheckBox.Checked = currentOptions.WholeWord;
        interpretEscapesCheckBox.Checked = currentOptions.InterpretEscapeSequences;
        wrapAroundCheckBox.Checked = currentOptions.WrapAround;

        escapeModeComboBox.Items.AddRange(
        [
            new EscapeModeItem("Any", EscapeSearchMode.Any),
            new EscapeModeItem("Escaped Only", EscapeSearchMode.OnlyEscaped),
            new EscapeModeItem("Unescaped Only", EscapeSearchMode.OnlyUnescaped)
        ]);
        escapeModeComboBox.SelectedIndex = FindEscapeModeIndex(currentOptions.EscapeMode);

        Shown += (_, _) => queryTextBox.SelectAll();
    }

    public string SearchText => queryTextBox.Text;

    public FindOptions SelectedOptions => new()
    {
        CaseSensitive = caseSensitiveCheckBox.Checked,
        WholeWord = wholeWordCheckBox.Checked,
        InterpretEscapeSequences = interpretEscapesCheckBox.Checked,
        WrapAround = wrapAroundCheckBox.Checked,
        EscapeMode = SelectedEscapeMode
    };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && SearchText.Length == 0)
        {
            MessageBox.Show(this, "Please enter a search term.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    private EscapeSearchMode SelectedEscapeMode
        => escapeModeComboBox.SelectedItem is EscapeModeItem item
            ? item.Value
            : EscapeSearchMode.Any;

    private static int FindEscapeModeIndex(EscapeSearchMode mode)
    {
        return mode switch
        {
            EscapeSearchMode.OnlyEscaped => 1,
            EscapeSearchMode.OnlyUnescaped => 2,
            _ => 0
        };
    }

    private sealed record EscapeModeItem(string Label, EscapeSearchMode Value)
    {
        public override string ToString() => Label;
    }
}
