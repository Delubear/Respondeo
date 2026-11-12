using System.Collections.Frozen;

namespace Respondeo.LiturgicalCalendar;

/// <summary>Loads authored metadata for celebrations whose dates are computed by domain rules.</summary>
internal static class ComputedCelebrationData
{
    private const string ResourceName = "Respondeo.LiturgicalCalendar.ComputedCelebrations.txt";
    private static readonly Lazy<IReadOnlyDictionary<string, LiturgicalCelebration>> Definitions = new(LoadEmbedded);

    public static IEnumerable<LiturgicalCelebration> All => Definitions.Value.Values;

    public static LiturgicalCelebration Get(string calendarKey) =>
        Definitions.Value.TryGetValue(calendarKey, out var celebration)
            ? celebration
            : throw new InvalidOperationException($"No computed calendar celebration is defined for key '{calendarKey}'.");

    internal static IReadOnlyDictionary<string, LiturgicalCelebration> Load(TextReader reader, string source)
    {
        var definitions = new Dictionary<string, LiturgicalCelebration>(StringComparer.Ordinal);
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

            var fields = trimmed.Split('|', StringSplitOptions.TrimEntries);
            if (fields.Length != 5)
            {
                throw InvalidRow(source, lineNumber, "Expected five fields: key | rank | color | name | precedence");
            }

            var key = fields[0];
            if (!CalendarDataValidation.IsCalendarKey(key))
            {
                throw InvalidRow(source, lineNumber, "Calendar key must be lowercase kebab-case");
            }

            var rank = ParseEnum<CelebrationRank>(fields[1], source, lineNumber);
            var color = ParseEnum<LiturgicalColor>(fields[2], source, lineNumber);
            var name = fields[3];
            if (name.Length == 0)
            {
                throw InvalidRow(source, lineNumber, "Celebration name must not be blank");
            }

            var precedence = ParseEnum<LiturgicalPrecedenceClass>(fields[4], source, lineNumber);
            if (!CalendarDataValidation.IsCompatible(rank, precedence))
            {
                throw InvalidRow(source, lineNumber, $"Precedence '{precedence}' is incompatible with rank '{rank}'");
            }

            var celebration = new LiturgicalCelebration(name, rank, color)
            {
                CalendarKey = key,
                Precedence = precedence,
            };
            if (!definitions.TryAdd(key, celebration))
            {
                throw InvalidRow(source, lineNumber, $"Duplicate calendar key '{key}'");
            }
        }

        return definitions.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, LiturgicalCelebration> LoadEmbedded()
    {
        using var reader = new StreamReader(typeof(ComputedCelebrationData).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded calendar resource '{ResourceName}' was not found."));
        return Load(reader, ResourceName);
    }

    private static T ParseEnum<T>(string field, string source, int line) where T : struct, Enum
    {
        if (!CalendarDataValidation.TryParseEnum<T>(field, out var value))
        {
            throw InvalidRow(source, line, $"Invalid {typeof(T).Name} '{field}'");
        }

        return value;
    }

    private static InvalidOperationException InvalidRow(string source, int line, string message) =>
        new($"Invalid computed calendar data in '{source}' at line {line}: {message}.");
}
