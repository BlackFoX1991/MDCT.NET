using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MarkdownPad;

internal sealed class PageCanvasPanel : Panel
{
    private Rectangle _pageBounds;
    private Color _shadowColor = Color.FromArgb(26, 34, 41, 51);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle PageBounds
    {
        get => _pageBounds;
        set
        {
            if (_pageBounds == value)
                return;

            Rectangle dirty = _pageBounds.IsEmpty ? value : Rectangle.Union(_pageBounds, value);
            _pageBounds = value;
            Invalidate(dirty);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ShadowColor
    {
        get => _shadowColor;
        set
        {
            if (_shadowColor.ToArgb() == value.ToArgb())
                return;

            _shadowColor = value;
            Invalidate();
        }
    }

    public PageCanvasPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_pageBounds.Width <= 0 || _pageBounds.Height <= 0)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.None;
        DrawShadow(e.Graphics, _pageBounds);
    }

    private void DrawShadow(Graphics graphics, Rectangle pageBounds)
    {
        const int layers = 6;

        for (int i = layers; i >= 1; i--)
        {
            int alpha = Math.Max(4, (int)Math.Round(_shadowColor.A * (i / (double)layers)));
            Rectangle shadowRect = pageBounds;
            shadowRect.Offset(i * 2, i * 2);

            using var brush = new SolidBrush(Color.FromArgb(alpha, _shadowColor));
            graphics.FillRectangle(brush, shadowRect);
        }
    }
}

internal sealed class PageSurfacePanel : Panel
{
    private Color _borderColor = Color.FromArgb(214, 220, 228);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor.ToArgb() == value.ToArgb())
                return;

            _borderColor = value;
            Invalidate();
        }
    }

    public PageSurfacePanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Rectangle borderRect = ClientRectangle;
        if (borderRect.Width <= 1 || borderRect.Height <= 1)
            return;

        borderRect.Width -= 1;
        borderRect.Height -= 1;

        using var pen = new Pen(_borderColor);
        e.Graphics.DrawRectangle(pen, borderRect);
    }
}
