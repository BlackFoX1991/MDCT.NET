using System;
using System.Collections.Generic;
using System.Text;

namespace MarkdownGdi;

public readonly record struct MarkdownHeadingAnchor(HeadingBlock Heading, string Slug, string Target);

public static class MarkdownAnchorHelper
{
    public static IReadOnlyList<MarkdownHeadingAnchor> BuildHeadingAnchors(IEnumerable<HeadingBlock> headings)
    {
        var anchors = new List<MarkdownHeadingAnchor>();
        var slugCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (HeadingBlock heading in headings)
        {
            string baseSlug = NormalizeHeadingSlug(heading.Text);
            if (string.IsNullOrEmpty(baseSlug))
                continue;

            int occurrence = slugCounts.TryGetValue(baseSlug, out int seen) ? seen + 1 : 0;
            slugCounts[baseSlug] = occurrence;

            string effectiveSlug = occurrence == 0 ? baseSlug : $"{baseSlug}-{occurrence}";
            string target = BuildHeadingTarget(heading.Level, effectiveSlug);
            anchors.Add(new MarkdownHeadingAnchor(heading, effectiveSlug, target));
        }

        return anchors;
    }

    public static bool TryParseHeadingAnchor(
        string anchor,
        out int? requestedLevel,
        out string lookupText,
        out string lookupSlug)
    {
        requestedLevel = null;
        lookupText = string.Empty;
        lookupSlug = string.Empty;

        string raw = (anchor ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        if (raw[0] == '#')
        {
            int hashCount = 0;
            while (hashCount < raw.Length && raw[hashCount] == '#')
                hashCount++;

            requestedLevel = hashCount;
            raw = raw[hashCount..];
        }

        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        lookupText = NormalizeHeadingText(raw);
        lookupSlug = NormalizeHeadingSlug(raw);
        return lookupText.Length > 0 || lookupSlug.Length > 0;
    }

    public static string BuildHeadingTarget(int level, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        int safeLevel = Math.Clamp(level, 1, 6);
        return $"{new string('#', safeLevel)}{slug}";
    }

    public static string NormalizeHeadingText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        bool pendingSeparator = false;

        foreach (char ch in value.Trim())
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSeparator && sb.Length > 0)
                    sb.Append(' ');

                sb.Append(char.ToLowerInvariant(ch));
                pendingSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
                pendingSeparator = sb.Length > 0;
        }

        return sb.ToString();
    }

    public static string NormalizeHeadingSlug(string value)
        => NormalizeHeadingText(value).Replace(' ', '-');
}
