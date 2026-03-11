using MarkdownGdi;

namespace MarkdownPad;

internal enum InsertMediaKind
{
    Link,
    Image
}

internal sealed partial class InsertMediaDialog : Form
{
    private static readonly HttpClient PreviewHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private static readonly string[] ImageExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".bmp",
        ".webp",
        ".svg"
    ];

    private readonly InsertMediaKind _kind;
    private readonly string? _documentBasePath;
    private readonly IReadOnlyList<MarkdownHeadingAnchor> _headingAnchors;
    private Image? _previewImage;
    private int _previewGeneration;

    public InsertMediaDialog(
        InsertMediaKind kind,
        string? documentBasePath,
        string? initialTitle = null,
        string? initialTarget = null,
        string? documentMarkdown = null)
    {
        _kind = kind;
        _documentBasePath = string.IsNullOrWhiteSpace(documentBasePath) ? null : Path.GetFullPath(documentBasePath);
        _headingAnchors = BuildHeadingAnchors(documentMarkdown);

        InitializeComponent();
        ConfigureAppearance();
        PopulateHeadingTargets();

        titleTextBox.Text = initialTitle ?? string.Empty;
        targetTextBox.Text = initialTarget ?? string.Empty;
        QueueRefresh();
    }

    public string GeneratedMarkdown { get; private set; } = string.Empty;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (string.IsNullOrWhiteSpace(titleTextBox.Text))
            titleTextBox.Focus();
        else
            targetTextBox.Focus();
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
                    "Please enter a path or URL.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposePreviewImage();
        base.OnFormClosed(e);
    }

    private void ConfigureAppearance()
    {
        Text = _kind == InsertMediaKind.Link ? "Insert Link" : "Insert Image";
        titleLabel.Text = _kind == InsertMediaKind.Link ? "Link text" : "Alt text";
        targetLabel.Text = _kind == InsertMediaKind.Link ? "Path or URL" : "Image path or URL";
        infoLabel.Text = _kind == InsertMediaKind.Link
            ? "Insert a web link or a local file. Relative paths are based on the active markdown document."
            : "Insert a local or remote image. Relative paths are based on the active markdown document.";

        bool showHeadingTargets = _kind == InsertMediaKind.Link && _headingAnchors.Count > 0;
        headingLabel.Visible = showHeadingTargets;
        headingComboBox.Visible = showHeadingTargets;
        layoutPanel.RowStyles[3].SizeType = SizeType.Absolute;
        layoutPanel.RowStyles[3].Height = showHeadingTargets ? 34 : 0;

        if (_kind == InsertMediaKind.Link)
        {
            if (showHeadingTargets)
                infoLabel.Text += " You can also pick a heading from the current document.";

            mediaPreviewLabel.Visible = false;
            previewHostPanel.Visible = false;
            layoutPanel.RowStyles[6].Height = 0;
            layoutPanel.RowStyles[6].SizeType = SizeType.Absolute;
            ClientSize = new Size(ClientSize.Width, showHeadingTargets ? 278 : 244);
        }

        titleTextBox.TextChanged += (_, _) => QueueRefresh();
        targetTextBox.TextChanged += (_, _) => QueueRefresh();
        browseButton.Click += (_, _) => BrowseForTarget();
        headingComboBox.SelectionChangeCommitted += HeadingComboBox_SelectionChangeCommitted;
    }

    private void BrowseForTarget()
    {
        using var dialog = new OpenFileDialog
        {
            Title = _kind == InsertMediaKind.Link ? "Select Link Target" : "Select Image",
            Filter = _kind == InsertMediaKind.Link
                ? "All files (*.*)|*.*"
                : "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(_documentBasePath) && Directory.Exists(_documentBasePath))
            dialog.InitialDirectory = _documentBasePath;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        targetTextBox.Text = ToPreferredTarget(dialog.FileName);
    }

    private void QueueRefresh()
    {
        int generation = ++_previewGeneration;
        _ = RefreshStateAsync(generation);
    }

    private void PopulateHeadingTargets()
    {
        headingComboBox.Items.Clear();

        foreach (MarkdownHeadingAnchor anchor in _headingAnchors)
        {
            headingComboBox.Items.Add(new HeadingTargetOption(
                $"{new string('#', Math.Clamp(anchor.Heading.Level, 1, 6))} {anchor.Heading.Text}",
                anchor.Target,
                anchor.Heading.Text));
        }

        headingComboBox.SelectedIndex = -1;
    }

    private void HeadingComboBox_SelectionChangeCommitted(object? sender, EventArgs e)
    {
        if (headingComboBox.SelectedItem is not HeadingTargetOption option)
            return;

        targetTextBox.Text = option.Target;
        if (string.IsNullOrWhiteSpace(titleTextBox.Text))
            titleTextBox.Text = option.Title;
    }

    private async Task RefreshStateAsync(int generation)
    {
        TargetAnalysis analysis = AnalyzeTarget(targetTextBox.Text);

        previewTextBox.Text = BuildMarkdown();
        insertButton.Enabled = !string.IsNullOrWhiteSpace(analysis.RawTarget);
        ApplyStatus(analysis);

        if (_kind != InsertMediaKind.Image)
            return;

        await UpdateImagePreviewAsync(analysis, generation);
    }

    private void ApplyStatus(TargetAnalysis analysis)
    {
        statusLabel.ForeColor = analysis.StatusColor;
        statusLabel.Text = analysis.StatusText;
    }

    private async Task UpdateImagePreviewAsync(TargetAnalysis analysis, int generation)
    {
        if (_kind != InsertMediaKind.Image)
            return;

        if (string.IsNullOrWhiteSpace(analysis.RawTarget))
        {
            SetPreviewImage(null);
            return;
        }

        if (!analysis.CanAttemptPreview)
        {
            SetPreviewImage(null);
            return;
        }

        try
        {
            byte[] bytes = analysis.RemoteUri is not null
                ? await PreviewHttpClient.GetByteArrayAsync(analysis.RemoteUri)
                : File.ReadAllBytes(analysis.ResolvedTarget);

            Image image = MarkdownImageLoader.LoadImage(bytes, analysis.PreviewSourceHint);

            if (generation != _previewGeneration || IsDisposed)
            {
                image.Dispose();
                return;
            }

            SetPreviewImage(image);
        }
        catch
        {
            if (generation == _previewGeneration)
                SetPreviewImage(null);
        }
    }

    private string BuildMarkdown()
    {
        string title = titleTextBox.Text.Trim();
        string target = targetTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        return _kind == InsertMediaKind.Link
            ? $"[{ResolveLinkText(title, target)}]({target})"
            : $"![{title}]({target})";
    }

    private TargetAnalysis AnalyzeTarget(string? rawInput)
    {
        string rawTarget = (rawInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawTarget))
        {
            return new TargetAnalysis(
                rawTarget,
                string.Empty,
                null,
                "Enter a path or URL.",
                SystemColors.GrayText,
                CanAttemptPreview: false);
        }

        if (Uri.TryCreate(rawTarget, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                string status = _kind == InsertMediaKind.Image
                    ? $"Valid web URL. Image preview will load if reachable: {absoluteUri.AbsoluteUri}"
                    : $"Valid web URL: {absoluteUri.AbsoluteUri}";

                return new TargetAnalysis(
                    rawTarget,
                    absoluteUri.AbsoluteUri,
                    absoluteUri,
                    status,
                    Color.FromArgb(33, 110, 57),
                    CanAttemptPreview: _kind == InsertMediaKind.Image,
                    PreviewSourceHint: absoluteUri.AbsoluteUri);
            }

            if (absoluteUri.IsFile)
                return AnalyzeLocalPath(rawTarget, absoluteUri.LocalPath);

            return new TargetAnalysis(
                rawTarget,
                rawTarget,
                null,
                $"Unsupported URI scheme: {absoluteUri.Scheme}",
                Color.DarkOrange,
                CanAttemptPreview: false);
        }

        return AnalyzeLocalPath(rawTarget, rawTarget);
    }

    private TargetAnalysis AnalyzeLocalPath(string rawTarget, string pathCandidate)
    {
        try
        {
            string resolvedPath = Path.GetFullPath(
                Path.IsPathRooted(pathCandidate)
                    ? pathCandidate
                    : Path.Combine(_documentBasePath ?? Environment.CurrentDirectory, pathCandidate));

            bool exists = File.Exists(resolvedPath);
            if (exists)
            {
                string status = _kind == InsertMediaKind.Image && !LooksLikeImagePath(resolvedPath)
                    ? $"File found, but the extension does not look like an image: {resolvedPath}"
                    : $"Resolved to: {resolvedPath}";

                Color color = _kind == InsertMediaKind.Image && !LooksLikeImagePath(resolvedPath)
                    ? Color.DarkOrange
                    : Color.FromArgb(33, 110, 57);

                return new TargetAnalysis(
                    rawTarget,
                    resolvedPath,
                    null,
                    status,
                    color,
                    CanAttemptPreview: _kind == InsertMediaKind.Image && LooksLikeImagePath(resolvedPath),
                    PreviewSourceHint: resolvedPath);
            }

            return new TargetAnalysis(
                rawTarget,
                resolvedPath,
                null,
                $"Resolves to missing file: {resolvedPath}",
                Color.DarkOrange,
                CanAttemptPreview: false,
                PreviewSourceHint: resolvedPath);
        }
        catch (Exception ex)
        {
            return new TargetAnalysis(
                rawTarget,
                rawTarget,
                null,
                $"Invalid path: {ex.Message}",
                Color.Firebrick,
                CanAttemptPreview: false);
        }
    }

    private string ToPreferredTarget(string selectedPath)
    {
        string fullPath = Path.GetFullPath(selectedPath);

        if (string.IsNullOrWhiteSpace(_documentBasePath) || !Directory.Exists(_documentBasePath))
            return fullPath;

        try
        {
            string relative = Path.GetRelativePath(_documentBasePath, fullPath);
            return string.IsNullOrWhiteSpace(relative) ? fullPath : relative;
        }
        catch
        {
            return fullPath;
        }
    }

    private void SetPreviewImage(Image? image)
    {
        Image? old = _previewImage;
        _previewImage = image;
        previewPictureBox.Image = image;
        old?.Dispose();
    }

    private void DisposePreviewImage()
    {
        Image? old = _previewImage;
        _previewImage = null;
        previewPictureBox.Image = null;
        old?.Dispose();
    }

    private static bool LooksLikeImagePath(string path)
    {
        string extension = Path.GetExtension(path);
        return ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveLinkText(string title, string target)
        => string.IsNullOrWhiteSpace(title) ? target : title;

    private static IReadOnlyList<MarkdownHeadingAnchor> BuildHeadingAnchors(string? documentMarkdown)
    {
        if (string.IsNullOrWhiteSpace(documentMarkdown))
            return Array.Empty<MarkdownHeadingAnchor>();

        string normalized = documentMarkdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        IReadOnlyList<MarkdownBlock> blocks = MarkdownParser.Parse(normalized.Split('\n'));
        return MarkdownAnchorHelper.BuildHeadingAnchors(blocks.OfType<HeadingBlock>());
    }

    private sealed record TargetAnalysis(
        string RawTarget,
        string ResolvedTarget,
        Uri? RemoteUri,
        string StatusText,
        Color StatusColor,
        bool CanAttemptPreview,
        string PreviewSourceHint = "");

    private sealed record HeadingTargetOption(string Display, string Target, string Title)
    {
        public override string ToString() => Display;
    }
}
