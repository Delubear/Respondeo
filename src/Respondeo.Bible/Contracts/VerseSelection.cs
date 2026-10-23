namespace Respondeo.Bible.Contracts;

/// <summary>A set of verse ranges within a chapter, such as <c>5-7,16</c>.</summary>
public sealed class VerseSelection
{
    private const int MaxVerse = 999;

    private VerseSelection(IReadOnlyList<(int Start, int End)> ranges) => Ranges = ranges;

    /// <summary>An empty selection.</summary>
    public static VerseSelection Empty { get; } = new([]);

    /// <summary>The sorted, merged, non-overlapping ranges.</summary>
    public IReadOnlyList<(int Start, int End)> Ranges { get; }

    /// <summary>Whether no verses are selected.</summary>
    public bool IsEmpty => Ranges.Count == 0;

    /// <summary>The first selected verse, or <c>null</c> when empty.</summary>
    public int? First => IsEmpty ? null : Ranges[0].Start;

    /// <summary>Whether <paramref name="verse"/> is selected.</summary>
    public bool Contains(int verse)
    {
        foreach (var (start, end) in Ranges)
        {
            if (verse >= start && verse <= end) return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a list such as <c>5-7,16</c>. A legacy <paramref name="to"/> value extends a single verse into a range.
    /// Invalid parts are ignored; reversed ranges are swapped; overlapping ranges are merged.
    /// </summary>
    public static VerseSelection Parse(string? value, int? to = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return Empty;

        var ranges = new List<(int Start, int End)>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bounds = part.Split(['-', '\u2013'], StringSplitOptions.TrimEntries);
            if (bounds.Length is < 1 or > 2) continue;
            if (!TryVerse(bounds[0], out var start)) continue;
            var end = start;
            if (bounds.Length == 2 && !TryVerse(bounds[1], out end)) continue;
            ranges.Add(start <= end ? (start, end) : (end, start));
        }

        if (to is { } legacyEnd && ranges.Count == 1 && ranges[0].Start == ranges[0].End && legacyEnd > ranges[0].Start && legacyEnd <= MaxVerse)
        {
            ranges[0] = (ranges[0].Start, legacyEnd);
        }

        return FromRanges(ranges);
    }

    /// <summary>Builds a selection from raw ranges, sorting and merging them.</summary>
    public static VerseSelection FromRanges(IEnumerable<(int Start, int End)> ranges)
    {
        var merged = new List<(int Start, int End)>();
        foreach (var range in ranges.OrderBy(r => r.Start))
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End + 1)
            {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, range.End));
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged.Count == 0 ? Empty : new VerseSelection(merged);
    }

    /// <summary>The query-string form, e.g. <c>5-7,16</c>.</summary>
    public override string ToString() =>
        string.Join(',', Ranges.Select(r => r.Start == r.End ? r.Start.ToString() : $"{r.Start}-{r.End}"));

    private static bool TryVerse(string text, out int verse) =>
        int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out verse) &&
        verse is >= 1 and <= MaxVerse;
}
