using System.Drawing;
using System.Text;

namespace MarkdownGdi;

public static class MarkdownImageLoader
{
    public static Image LoadImage(byte[] bytes, string sourceHint, int svgMaxWidth = 1600, int svgMaxHeight = 1600)
    {
        if (bytes.Length == 0)
            throw new InvalidOperationException("Image stream is empty.");

        if (LooksLikeSvg(sourceHint, bytes))
            return LoadSvgBitmap(bytes, svgMaxWidth, svgMaxHeight);

        using var stream = new MemoryStream(bytes, writable: false);
        using var loaded = Image.FromStream(stream);
        return new Bitmap(loaded);
    }

    private static bool LooksLikeSvg(string sourceHint, byte[] bytes)
    {
        if (!string.IsNullOrWhiteSpace(sourceHint))
        {
            string candidate = sourceHint;
            if (Uri.TryCreate(sourceHint, UriKind.Absolute, out Uri? uri))
                candidate = uri.AbsolutePath;

            if (string.Equals(Path.GetExtension(candidate), ".svg", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        string prefix = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 2048)).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        return prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
               (prefix.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
                prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase));
    }

    private static Image LoadSvgBitmap(byte[] bytes, int svgMaxWidth, int svgMaxHeight)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var svg = new Svg.Skia.SKSvg();
        SkiaSharp.SKPicture picture = svg.Load(stream)
            ?? throw new InvalidOperationException("SVG could not be parsed.");

        SkiaSharp.SKRect bounds = picture.CullRect;
        float sourceWidth = bounds.Width > 0 ? bounds.Width : 256f;
        float sourceHeight = bounds.Height > 0 ? bounds.Height : 256f;

        float scale = Math.Min(
            1f,
            Math.Min(
                svgMaxWidth / sourceWidth,
                svgMaxHeight / sourceHeight));

        int width = Math.Max(1, (int)Math.Ceiling(sourceWidth * scale));
        int height = Math.Max(1, (int)Math.Ceiling(sourceHeight * scale));

        using var bitmap = new SkiaSharp.SKBitmap(
            new SkiaSharp.SKImageInfo(
                width,
                height,
                SkiaSharp.SKColorType.Bgra8888,
                SkiaSharp.SKAlphaType.Premul));
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        canvas.Scale(width / sourceWidth, height / sourceHeight);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using SkiaSharp.SKData data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var encoded = new MemoryStream(data.ToArray(), writable: false);
        using var loaded = Image.FromStream(encoded);
        return new Bitmap(loaded);
    }
}
