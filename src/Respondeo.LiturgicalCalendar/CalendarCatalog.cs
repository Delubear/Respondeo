namespace Respondeo.LiturgicalCalendar;

/// <summary>Validates authored identities required by calendar algorithms before any date is resolved.</summary>
internal static class CalendarCatalog
{
    internal static IReadOnlyList<(int Month, int Day, string Key)> TransferableSolemnities { get; } = Array.AsReadOnly(new[]
    {
        (3, 19, "saint-joseph"),
        (3, 25, "annunciation"),
        (6, 24, "nativity-of-saint-john-the-baptist"),
        (6, 24, "sacred-heart"),
        (6, 29, "saints-peter-and-paul"),
        (12, 8, "immaculate-conception"),
    });

    private static readonly string[] RequiredComputedKeys =
    [
        "ash-wednesday", "palm-sunday", "holy-thursday", "good-friday", "holy-saturday", "easter-sunday",
        "ascension", "pentecost", "trinity", "corpus-christi", "mother-of-the-church", "sacred-heart",
        "immaculate-heart", "thanksgiving", "holy-family", "epiphany", "baptism-of-the-lord", "christ-the-king",
    ];

    private static readonly Lazy<bool> GeneralValidated = new(() => ValidateEmbedded(CalendarRegion.GeneralRoman));
    private static readonly Lazy<bool> UnitedStatesValidated = new(() => ValidateEmbedded(CalendarRegion.UnitedStates));

    internal static void EnsureValid(CalendarRegion region) =>
        _ = region == CalendarRegion.UnitedStates ? UnitedStatesValidated.Value : GeneralValidated.Value;

    private static bool ValidateEmbedded(CalendarRegion region)
    {
        Validate(region, GeneralRomanCalendarData.ForRegion(region), ComputedCelebrationData.All.ToArray());
        return true;
    }

    internal static void Validate(CalendarRegion region,
        IReadOnlyDictionary<(int Month, int Day), IReadOnlyList<LiturgicalCelebration>> fixedEntries,
        IReadOnlyList<LiturgicalCelebration> computedEntries)
    {
        var fixedCelebrations = fixedEntries.Values.SelectMany(entries => entries).ToArray();
        var definitions = fixedCelebrations.Concat(computedEntries).ToArray();
        foreach (var celebration in definitions)
        {
            if (string.IsNullOrWhiteSpace(celebration.CalendarKey) || celebration.Precedence is null)
            {
                throw Invalid(region, $"Celebration '{celebration.Name}' requires an authored calendar key and precedence");
            }
        }

        foreach (var key in RequiredComputedKeys)
        {
            if (computedEntries.Count(entry => entry.CalendarKey == key) != 1)
            {
                throw Invalid(region, $"Computed identity '{key}' must be defined exactly once");
            }
        }

        foreach (var group in definitions.GroupBy(entry => entry.CalendarKey, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            // Epiphany has both fixed-date and Sunday-placement definitions for the two supported profiles.
            if (group.Key != "epiphany" || group.Count() != 2
                || fixedCelebrations.Count(entry => entry.CalendarKey == group.Key) != 1
                || computedEntries.Count(entry => entry.CalendarKey == group.Key) != 1
                || group.Select(entry => (entry.Name, entry.Rank, entry.Color, entry.Precedence)).Distinct().Count() != 1)
            {
                throw Invalid(region, $"Conflicting or duplicate identity '{group.Key}' across calendar catalogs");
            }
        }

        foreach (var (month, day, key) in TransferableSolemnities.Where(item => item.Key != "sacred-heart"))
        {
            RequireFixed(month, day, key, CelebrationRank.Solemnity);
        }

        RequireFixed(1, 6, "epiphany", CelebrationRank.Solemnity);
        RequireFixed(11, 2, "all-souls", CelebrationRank.Feast);
        if (region == CalendarRegion.UnitedStates)
        {
            RequireFixed(1, 22, "day-of-prayer-for-unborn-children", CelebrationRank.Memorial);
        }

        if (computedEntries.Single(entry => entry.CalendarKey == "sacred-heart").Rank != CelebrationRank.Solemnity)
        {
            throw Invalid(region, "Transferable computed identity 'sacred-heart' must be a solemnity");
        }

        void RequireFixed(int month, int day, string key, CelebrationRank rank)
        {
            if (!fixedEntries.TryGetValue((month, day), out var entries)
                || entries.SingleOrDefault(entry => entry.CalendarKey == key) is not { } celebration || celebration.Rank != rank)
            {
                throw Invalid(region, $"Required {rank} '{key}' is missing or has the wrong rank on {month:00}-{day:00}");
            }
        }
    }

    private static InvalidOperationException Invalid(CalendarRegion region, string message) =>
        new($"Invalid '{region}' calendar catalog: {message}.");
}
