using System;
using System.Drawing;

namespace MarkdownGdi;

internal static class InlineImageMetrics
{
    public const int MaxWidth = 320;
    public const int MaxHeight = 160;
    public const int PlaceholderWidth = 96;
    public const int PlaceholderHeight = 72;

    public static Size CalculateSize(string imageSource, Func<string, Size?>? imageSizeProvider)
    {
        if (imageSizeProvider is not null && !string.IsNullOrWhiteSpace(imageSource))
        {
            Size? actual = imageSizeProvider(imageSource);
            if (actual is { Width: > 0, Height: > 0 })
            {
                float scale = Math.Min(
                    1f,
                    Math.Min(
                        MaxWidth / (float)actual.Value.Width,
                        MaxHeight / (float)actual.Value.Height));

                int width = Math.Max(1, (int)Math.Round(actual.Value.Width * scale));
                int height = Math.Max(1, (int)Math.Round(actual.Value.Height * scale));
                return new Size(width, height);
            }
        }

        return new Size(PlaceholderWidth, PlaceholderHeight);
    }
}
