using System.Collections.Generic;

namespace MarkdownGdi;

public static class InlineDecorationMetrics
{
    public const int FramePaddingX = 6;
    public const int FramePaddingY = 3;
    public const int ProgressPaddingX = 8;
    public const int ProgressPaddingY = 3;
    public const int ProgressMinWidth = 120;
    public const int ProgressMinHeight = 18;
    public const int ProgressCornerRadius = 4;

    public static int GetLeadingFramePadding(IReadOnlyList<InlineFrameDecoration> decorations)
    {
        int padding = 0;
        for (int i = 0; i < decorations.Count; i++)
        {
            if (decorations[i].IncludeLeadingPadding)
                padding += FramePaddingX;
        }

        return padding;
    }

    public static int GetTrailingFramePadding(IReadOnlyList<InlineFrameDecoration> decorations)
    {
        int padding = 0;
        for (int i = 0; i < decorations.Count; i++)
        {
            if (decorations[i].IncludeTrailingPadding)
                padding += FramePaddingX;
        }

        return padding;
    }

    public static int GetLeadingFramePaddingBefore(IReadOnlyList<InlineFrameDecoration> decorations, int decorationIndex)
    {
        int padding = 0;
        int count = Math.Min(decorationIndex, decorations.Count);
        for (int i = 0; i < count; i++)
        {
            if (decorations[i].IncludeLeadingPadding)
                padding += FramePaddingX;
        }

        return padding;
    }

    public static int GetTrailingFramePaddingBefore(IReadOnlyList<InlineFrameDecoration> decorations, int decorationIndex)
    {
        int padding = 0;
        int count = Math.Min(decorationIndex, decorations.Count);
        for (int i = 0; i < count; i++)
        {
            if (decorations[i].IncludeTrailingPadding)
                padding += FramePaddingX;
        }

        return padding;
    }

    public static int GetTotalFrameVerticalPadding(IReadOnlyList<InlineFrameDecoration> decorations)
        => decorations.Count * FramePaddingY * 2;

    public static int GetFrameVerticalPaddingFromDepth(int activeFrameCount, int decorationIndex)
        => Math.Max(0, activeFrameCount - decorationIndex) * FramePaddingY * 2;

    public static int GetProgressWidth(int labelWidth)
        => Math.Max(ProgressMinWidth, Math.Max(0, labelWidth) + ProgressPaddingX * 2);

    public static int GetProgressHeight(int textHeight)
        => Math.Max(ProgressMinHeight, Math.Max(1, textHeight) + ProgressPaddingY * 2);
}
