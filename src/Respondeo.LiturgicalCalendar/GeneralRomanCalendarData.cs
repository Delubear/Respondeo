namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// Loads the bundled General Roman Calendar (fixed civil-date celebrations) from the embedded <c>GeneralRomanCalendar.txt</c> resource exactly once and exposes it as a month/day lookup.
/// The parse is lazy and cached so the pure <see cref="RomanCalendar"/> engine stays synchronous.
/// </summary>
internal static class GeneralRomanCalendarData
{
    private const string ResourceName = "Respondeo.LiturgicalCalendar.GeneralRomanCalendar.txt";

    private static readonly Lazy<IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>>> Entries = new(Parse);

    /// <summary>Returns the fixed-date celebrations for the given month and day, or an empty list when none.</summary>
    public static IReadOnlyList<LiturgicalCelebration> ForMonthDay(int month, int day) =>
        Entries.Value.TryGetValue((month, day), out var celebrations) ? celebrations : [];

    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> Parse()
    {
        var map = new Dictionary<(int, int), List<LiturgicalCelebration>>();

        using var stream = typeof(GeneralRomanCalendarData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded liturgical calendar resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length is not (4 or 5 or 6))
            {
                throw new InvalidOperationException($"Malformed liturgical calendar line (expected 4 to 6 '|'-delimited fields): {trimmed}");
            }

            var (month, day) = ParseMonthDay(parts[0]);
            var rank = Enum.Parse<CelebrationRank>(parts[1], ignoreCase: true);
            var color = Enum.Parse<LiturgicalColor>(parts[2], ignoreCase: true);
            var slug = parts.Length >= 5 && parts[4].Length > 0 ? parts[4] : null;
            var summary = parts.Length == 6 && parts[5].Length > 0 ? parts[5] : null;
            var celebration = new LiturgicalCelebration(parts[3], rank, color) { Id = slug, Summary = summary };

            if (!map.TryGetValue((month, day), out var list))
            {
                list = [];
                map[(month, day)] = list;
            }

            list.Add(celebration);
        }

        return map.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<LiturgicalCelebration>)pair.Value);
    }

    private static (int Month, int Day) ParseMonthDay(string field)
    {
        var halves = field.Split('-');
        if (halves.Length != 2
            || !int.TryParse(halves[0], out var month)
            || !int.TryParse(halves[1], out var day))
        {
            throw new InvalidOperationException($"Malformed liturgical calendar date (expected 'MM-DD'): {field}");
        }

        return (month, day);
    }
}
