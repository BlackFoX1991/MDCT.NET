using System;
using System.Drawing;

namespace MarkdownGdi;

internal static class MarkdownTypography
{
    public static float HeadingScale(int level)
    {
        int l = Math.Clamp(level, 1, 6);
        return l switch
        {
            1 => 2.00f, // h1
            2 => 1.50f, // h2
            3 => 1.25f, // h3
            4 => 1.10f, // h4
            5 => 1.00f, // h5
            _ => 0.90f  // h6
        };
    }

    public static Font CreateHeadingFont(Font baseFont, int level)
    {
        float size = Math.Max(6f, baseFont.Size * HeadingScale(level));
        return new Font(baseFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
    }
}
