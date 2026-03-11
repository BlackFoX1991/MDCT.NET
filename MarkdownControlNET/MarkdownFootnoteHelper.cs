using System.Collections.Generic;
using System.Text;

namespace MarkdownGdi;

public readonly record struct MarkdownFootnoteReference(
    string Label,
    string NormalizedLabel,
    int StartColumn,
    int LabelStartColumn,
    int LabelEndColumn,
    int EndColumnExclusive,
    int CaretColumn);

public sealed class MarkdownFootnoteIndex
{
    public static MarkdownFootnoteIndex Empty { get; } = new(
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, MarkdownPosition>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlyList<MarkdownPosition>>(StringComparer.OrdinalIgnoreCase));

    public MarkdownFootnoteIndex(
        IReadOnlyDictionary<string, int> numbersByLabel,
        IReadOnlyDictionary<string, MarkdownPosition> firstReferencePositions,
        IReadOnlyDictionary<string, IReadOnlyList<MarkdownPosition>> referencePositionsByLabel)
    {
        NumbersByLabel = numbersByLabel;
        FirstReferencePositions = firstReferencePositions;
        ReferencePositionsByLabel = referencePositionsByLabel;
    }

    public IReadOnlyDictionary<string, int> NumbersByLabel { get; }
    public IReadOnlyDictionary<string, MarkdownPosition> FirstReferencePositions { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<MarkdownPosition>> ReferencePositionsByLabel { get; }

    public bool TryGetNumber(string normalizedLabel, out int number)
        => NumbersByLabel.TryGetValue(normalizedLabel, out number);

    public bool TryGetFirstReferencePosition(string normalizedLabel, out MarkdownPosition position)
        => FirstReferencePositions.TryGetValue(normalizedLabel, out position);

    public IReadOnlyList<MarkdownPosition> GetReferencePositions(string normalizedLabel)
        => ReferencePositionsByLabel.TryGetValue(normalizedLabel, out IReadOnlyList<MarkdownPosition>? positions)
            ? positions
            : Array.Empty<MarkdownPosition>();

    public bool TryGetReferencePosition(string normalizedLabel, int occurrence, out MarkdownPosition position)
    {
        position = default;
        if (!ReferencePositionsByLabel.TryGetValue(normalizedLabel, out IReadOnlyList<MarkdownPosition>? positions))
            return false;

        if (occurrence <= 0 || occurrence > positions.Count)
            return false;

        position = positions[occurrence - 1];
        return true;
    }
}

public static class MarkdownFootnoteHelper
{
    public static MarkdownFootnoteIndex BuildIndex(IReadOnlyList<string> lines)
    {
        var numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var firstPositions = new Dictionary<string, MarkdownPosition>(StringComparer.OrdinalIgnoreCase);
        var positionsByLabel = new Dictionary<string, List<MarkdownPosition>>(StringComparer.OrdinalIgnoreCase);
        int nextNumber = 1;

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            string line = lines[lineIndex];
            foreach (MarkdownFootnoteReference reference in EnumerateReferences(line))
            {
                if (string.IsNullOrEmpty(reference.NormalizedLabel))
                    continue;

                if (IsFootnoteDefinitionMarker(line, reference))
                    continue;

                if (!numbers.ContainsKey(reference.NormalizedLabel))
                    numbers[reference.NormalizedLabel] = nextNumber++;

                if (!firstPositions.ContainsKey(reference.NormalizedLabel))
                {
                    firstPositions[reference.NormalizedLabel] = new MarkdownPosition(
                        lineIndex,
                        Math.Max(0, reference.CaretColumn));
                }

                if (!positionsByLabel.TryGetValue(reference.NormalizedLabel, out List<MarkdownPosition>? positions))
                {
                    positions = new List<MarkdownPosition>();
                    positionsByLabel[reference.NormalizedLabel] = positions;
                }

                positions.Add(new MarkdownPosition(
                    lineIndex,
                    Math.Max(0, reference.CaretColumn)));
            }
        }

        return new MarkdownFootnoteIndex(
            numbers,
            firstPositions,
            positionsByLabel.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<MarkdownPosition>)kv.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsFootnoteDefinitionMarker(string line, MarkdownFootnoteReference reference)
    {
        if (string.IsNullOrEmpty(line))
            return false;

        if (reference.StartColumn < 0 || reference.EndColumnExclusive < 0 || reference.EndColumnExclusive >= line.Length)
            return false;

        for (int i = 0; i < reference.StartColumn; i++)
        {
            if (line[i] != ' ')
                return false;
        }

        if (reference.StartColumn > 3)
            return false;

        return line[reference.EndColumnExclusive] == ':';
    }

    public static string NormalizeFootnoteLabel(string label)
    {
        string raw = (label ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var sb = new StringBuilder(raw.Length);
        bool pendingSeparator = false;

        foreach (char ch in raw)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSeparator && sb.Length > 0)
                    sb.Append('-');

                sb.Append(char.ToLowerInvariant(ch));
                pendingSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_')
                pendingSeparator = sb.Length > 0;
        }

        return sb.ToString();
    }

    public static string BuildReferenceAnchor(string label, int occurrence = 1)
    {
        string normalized = NormalizeFootnoteLabel(label);
        return string.IsNullOrEmpty(normalized)
            ? string.Empty
            : occurrence <= 1
                ? $"#fnref-{normalized}"
                : $"#fnref-{normalized}--{occurrence}";
    }

    public static string BuildDefinitionAnchor(string label)
    {
        string normalized = NormalizeFootnoteLabel(label);
        return string.IsNullOrEmpty(normalized)
            ? string.Empty
            : $"#fn-{normalized}";
    }

    public static bool TryParseReferenceAnchor(string anchor, out string normalizedLabel, out int occurrence)
    {
        normalizedLabel = string.Empty;
        occurrence = 1;

        string raw = (anchor ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        if (raw[0] == '#')
            raw = raw[1..];

        if (!raw.StartsWith("fnref-", StringComparison.OrdinalIgnoreCase))
            return false;

        string body = raw[6..];
        int split = body.LastIndexOf("--", StringComparison.Ordinal);
        if (split >= 0)
        {
            string occurrenceText = body[(split + 2)..];
            if (!int.TryParse(occurrenceText, out occurrence) || occurrence <= 0)
                return false;

            body = body[..split];
        }

        normalizedLabel = NormalizeFootnoteLabel(body);
        return !string.IsNullOrEmpty(normalizedLabel);
    }

    public static bool TryParseDefinitionAnchor(string anchor, out string normalizedLabel)
    {
        normalizedLabel = string.Empty;

        string raw = (anchor ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return false;

        if (raw[0] == '#')
            raw = raw[1..];

        if (!raw.StartsWith("fn-", StringComparison.OrdinalIgnoreCase))
            return false;

        normalizedLabel = NormalizeFootnoteLabel(raw[3..]);
        return !string.IsNullOrEmpty(normalizedLabel);
    }

    public static IReadOnlyList<MarkdownFootnoteReference> EnumerateReferences(string line)
    {
        if (string.IsNullOrEmpty(line))
            return Array.Empty<MarkdownFootnoteReference>();

        var references = new List<MarkdownFootnoteReference>();

        for (int i = 0; i < line.Length; i++)
        {
            if (!TryReadReference(line, i, out MarkdownFootnoteReference reference))
                continue;

            references.Add(reference);
            i = Math.Max(i, reference.EndColumnExclusive - 1);
        }

        return references;
    }

    private static bool TryReadReference(string line, int start, out MarkdownFootnoteReference reference)
    {
        reference = default;

        if (start < 0 || start + 3 >= line.Length || line[start] != '[' || line[start + 1] != '^')
            return false;

        if (IsEscapedAt(line, start))
            return false;

        int close = FindUnescapedChar(line, ']', start + 2);
        if (close < 0)
            return false;

        string label = line[(start + 2)..close].Trim();
        string normalized = NormalizeFootnoteLabel(label);
        if (string.IsNullOrEmpty(normalized))
            return false;

        reference = new MarkdownFootnoteReference(
            Label: label,
            NormalizedLabel: normalized,
            StartColumn: start,
            LabelStartColumn: start + 2,
            LabelEndColumn: close,
            EndColumnExclusive: close + 1,
            CaretColumn: close + 1);
        return true;
    }

    private static int FindUnescapedChar(string text, char ch, int startIndex)
    {
        for (int i = Math.Max(0, startIndex); i < text.Length; i++)
        {
            if (text[i] != ch)
                continue;

            if (!IsEscapedAt(text, i))
                return i;
        }

        return -1;
    }

    private static bool IsEscapedAt(string text, int index)
    {
        int backslashes = 0;
        for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
            backslashes++;

        return (backslashes & 1) == 1;
    }
}
