using System.Collections.Frozen;
using System.Globalization;

namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// Loads the bundled General Roman Calendar (fixed civil-date celebrations) from the embedded <c>GeneralRomanCalendar.txt</c> resource exactly once and exposes it as a month/day lookup.
/// The parse is lazy and cached so the pure <see cref="RomanCalendar"/> engine stays synchronous.
/// </summary>
internal static class GeneralRomanCalendarData
{
    private const string ResourceName = "Respondeo.LiturgicalCalendar.GeneralRomanCalendar.txt";
    private const string UnitedStatesResourceName = "Respondeo.LiturgicalCalendar.UnitedStatesProper.txt";

    private static readonly Lazy<IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>>> Entries = new(LoadGeneral);

    private static readonly Lazy<IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>>> UnitedStatesEntries = new(BuildUnitedStates);

    private readonly record struct Removal((int Month, int Day) Date, string Name, int Line);

    /// <summary>Returns the fixed-date celebrations for the given month and day, or an empty list when none.</summary>
    public static IReadOnlyList<LiturgicalCelebration> ForMonthDay(int month, int day) => ForMonthDay(month, day, CalendarRegion.GeneralRoman);

    /// <summary>Returns the fixed-date celebrations for the given month and day in the given region's calendar.</summary>
    public static IReadOnlyList<LiturgicalCelebration> ForMonthDay(int month, int day, CalendarRegion region)
    {
        var entries = ForRegion(region);
        return entries.TryGetValue((month, day), out var celebrations) ? celebrations : [];
    }

    internal static IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>> ForRegion(CalendarRegion region) =>
        region == CalendarRegion.UnitedStates ? UnitedStatesEntries.Value : Entries.Value;

    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> Freeze(
        Dictionary<(int, int), List<LiturgicalCelebration>> map, string source)
    {
        var duplicate = map.Values.SelectMany(entries => entries).Where(entry => entry.CalendarKey is not null)
            .GroupBy(entry => entry.CalendarKey).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Calendar '{source}' contains duplicate calendar key '{duplicate.Key}' after regional merging.");
        }

        return map.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlyList<LiturgicalCelebration>)Array.AsReadOnly(pair.Value.ToArray()));
    }

    private static StreamReader OpenResource(string name) =>
        new(typeof(GeneralRomanCalendarData).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded liturgical calendar resource '{name}' was not found."));

    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> LoadGeneral()
    {
        using var reader = OpenResource(ResourceName);
        return Load(reader, ResourceName);
    }

    private static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> BuildUnitedStates()
    {
        using var general = OpenResource(ResourceName);
        using var proper = OpenResource(UnitedStatesResourceName);
        return Load(general, ResourceName, proper, UnitedStatesResourceName);
    }

    internal static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> Load(TextReader reader, string source) =>
        Freeze(Parse(reader, source), source);

    /// <summary>Applies a regional proper to a fresh General catalog; readers remain owned by the caller.</summary>
    internal static IReadOnlyDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>> Load(
        TextReader general, string generalSource, TextReader proper, string properSource)
    {
        var map = Parse(general, generalSource);
        var removals = new List<Removal>();
        var overlay = Parse(proper, properSource, removals);

        foreach (var removal in removals)
        {
            if (!map.TryGetValue(removal.Date, out var list) || list.RemoveAll(c => c.Name == removal.Name) != 1)
            {
                throw InvalidRow(properSource, removal.Line,
                    $"Removal '{removal.Name}' on {removal.Date.Month:00}-{removal.Date.Day:00} must match exactly one General entry");
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
                    var original = list[existing];
                    if (celebration.CalendarKey is not null && original.CalendarKey is not null && celebration.CalendarKey != original.CalendarKey)
                    {
                        throw new InvalidOperationException($"Regional calendar '{properSource}' changes the stable identity of '{celebration.Name}'.");
                    }

                    list[existing] = celebration with
                    {
                        Id = original.Id,
                        Summary = celebration.Summary ?? original.Summary,
                        CalendarKey = original.CalendarKey ?? celebration.CalendarKey,
                        Precedence = celebration.Precedence ?? (celebration.Rank == original.Rank ? original.Precedence : null),
                    };
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

        return Freeze(map, properSource);
    }

    private static Dictionary<(int, int), List<LiturgicalCelebration>> Parse(TextReader reader, string source, List<Removal>? removals = null)
    {
        var map = new Dictionary<(int, int), List<LiturgicalCelebration>>();
        var identities = new HashSet<((int, int) Date, string Name)>();
        var lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('|', StringSplitOptions.TrimEntries);
            var removal = parts[0].StartsWith('-');
            if (removal && (removals is null || parts.Length != 2))
            {
                throw InvalidRow(source, lineNumber, "Removals are permitted only in a regional proper as '-MM-DD | Name'");
            }

            if (!removal && parts.Length is not (4 or 5 or 6 or 8))
            {
                throw InvalidRow(source, lineNumber, "Expected 4 to 6 legacy fields or 8 fields including calendar key and precedence");
            }

            var date = ParseMonthDay(removal ? parts[0][1..] : parts[0], source, lineNumber);
            var name = parts[removal ? 1 : 3];
            if (name.Length == 0)
            {
                throw InvalidRow(source, lineNumber, "Celebration name must not be blank");
            }

            if (!identities.Add((date, name)))
            {
                throw InvalidRow(source, lineNumber, $"Duplicate or conflicting operation for '{name}' on {date.Month:00}-{date.Day:00}");
            }

            if (removal)
            {
                removals!.Add(new Removal(date, name, lineNumber));
                continue;
            }

            var rank = ParseEnum<CelebrationRank>(parts[1], source, lineNumber);
            var color = ParseEnum<LiturgicalColor>(parts[2], source, lineNumber);
            var slug = parts.Length >= 5 && parts[4].Length > 0 ? parts[4] : null;
            var summary = parts.Length >= 6 && parts[5].Length > 0 ? parts[5] : null;
            var key = parts.Length == 8 ? parts[6] : null;
            LiturgicalPrecedenceClass? precedence = parts.Length == 8 ? ParseEnum<LiturgicalPrecedenceClass>(parts[7], source, lineNumber) : null;
            if (key is not null && !CalendarDataValidation.IsCalendarKey(key))
            {
                throw InvalidRow(source, lineNumber, "Calendar key must be nonempty lowercase kebab-case");
            }

            if (key is not null && map.Values.SelectMany(entries => entries).Any(entry => entry.CalendarKey == key))
            {
                throw InvalidRow(source, lineNumber, $"Duplicate calendar key '{key}'");
            }

            if (precedence is { } category && !CalendarDataValidation.IsCompatible(rank, category))
            {
                throw InvalidRow(source, lineNumber, $"Precedence '{precedence}' is incompatible with rank '{rank}'");
            }

            var celebration = new LiturgicalCelebration(name, rank, color) { Id = slug, Summary = summary, CalendarKey = key, Precedence = precedence };

            if (!map.TryGetValue(date, out var list))
            {
                list = [];
                map[date] = list;
            }

            list.Add(celebration);
        }

        return map;
    }

    private static (int Month, int Day) ParseMonthDay(string field, string source, int line)
    {
        // A leap-year anchor admits February 29 without accepting impossible recurring dates.
        if (field.Length != 5 || !DateOnly.TryParseExact($"2000-{field}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw InvalidRow(source, line, $"Invalid date '{field}'; expected a real 'MM-DD' date");
        }

        return (date.Month, date.Day);
    }

    private static T ParseEnum<T>(string field, string source, int line) where T : struct, Enum
    {
        if (!CalendarDataValidation.TryParseEnum<T>(field, out var value))
        {
            throw InvalidRow(source, line, $"Invalid {typeof(T).Name} '{field}'; expected a defined enum name");
        }

        return value;
    }

    private static InvalidOperationException InvalidRow(string source, int line, string message) =>
        new($"Invalid liturgical calendar data in '{source}' at line {line}: {message}.");
}
