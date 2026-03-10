namespace MarkdownPad
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void markdownGdiEditor1_Click(object sender, EventArgs e)
        {

        }

        private void markdownGdiEditor1_TextChanged(object sender, EventArgs e)
        {
            this.Text = "Changed";
        }
    }
}
