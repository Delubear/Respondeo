namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// Loads the bundled General Roman Calendar (fixed civil-date celebrations) from the embedded <c>GeneralRomanCalendar.txt</c> resource exactly once and exposes it as a month/day lookup.
/// The parse is lazy and cached so the pure <see cref="RomanCalendar"/> engine stays synchronous.
/// </summary>
internal static class GeneralRomanCalendarData
{
    private const string ResourceName = "Respondeo.LiturgicalCalendar.GeneralRomanCalendar.txt";
    private const string UnitedStatesResourceName = "Respondeo.LiturgicalCalendar.UnitedStatesProper.txt";

    private static readonly Lazy<IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>>> Entries = new(() => Freeze(Parse(ResourceName)));

    private static readonly Lazy<IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>>> UnitedStatesEntries = new(BuildUnitedStates);

    /// <summary>Returns the fixed-date celebrations for the given month and day, or an empty list when none.</summary>
    public static IReadOnlyList<LiturgicalCelebration> ForMonthDay(int month, int day) => ForMonthDay(month, day, CalendarRegion.GeneralRoman);

    /// <summary>Returns the fixed-date celebrations for the given month and day in the given region's calendar.</summary>
    public static IReadOnlyList<LiturgicalCelebration> ForMonthDay(int month, int day, CalendarRegion region)
    {
        var entries = region == CalendarRegion.UnitedStates ? UnitedStatesEntries.Value : Entries.Value;
        return entries.TryGetValue((month, day), out var celebrations) ? celebrations : [];
    }

    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> Freeze(Dictionary<(int, int), List<LiturgicalCelebration>> map) =>
        map.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<LiturgicalCelebration>)pair.Value);

    /// <summary>Applies the United States proper over a fresh copy of the General Roman data: removals, same-name replacements, and additions.</summary>
    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> BuildUnitedStates()
    {
        var map = Parse(ResourceName);
        var removals = new List<((int, int) Date, string Name)>();
        var overlay = Parse(UnitedStatesResourceName, removals);

        foreach (var (date, name) in removals)
        {
            if (map.TryGetValue(date, out var list))
            {
                list.RemoveAll(c => c.Name == name);
            }
        }

        foreach (var (date, celebrations) in overlay)
        {
            if (!map.TryGetValue(date, out var list))
            {
                list = [];
                map[date] = list;
            }

            foreach (var celebration in celebrations)
            {
                var existing = list.FindIndex(c => c.Name == celebration.Name);
                if (existing >= 0)
                {
                    list[existing] = celebration with { Id = list[existing].Id, Summary = celebration.Summary ?? list[existing].Summary };
                }
                else
                {
                    list.Add(celebration);
                }
            }

            // Keep obligatory memorials and higher ahead of optional memorials so precedence picks them first.
            var ordered = list.OrderBy(c => c.Rank).ToList();
            list.Clear();
            list.AddRange(ordered);
        }

        return Freeze(map);
    }

    private static Dictionary<(int, int), List<LiturgicalCelebration>> Parse(string resourceName, List<((int, int), string)>? removals = null)
    {
        var map = new Dictionary<(int, int), List<LiturgicalCelebration>>();

        using var stream = typeof(GeneralRomanCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded liturgical calendar resource '{resourceName}' was not found.");
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
            if (removals is not null && trimmed.StartsWith('-') && parts.Length == 2)
            {
                removals.Add((ParseMonthDay(parts[0][1..]), parts[1]));
                continue;
            }

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

        return map;
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
