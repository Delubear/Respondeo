namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Short-reading citations for celebrations of the saints: a celebration's own citations first, then those of its Common.
/// Every celebration and its Common are listed explicitly in <c>Data/sanctoral.json</c>.
/// </summary>
internal static class Sanctoral
{
    public static string? ShortReading(Hour hour, string celebrationName)
    {
        var data = OfficeData.Sanctoral;
        var id = OrdinaryFormOffice.CelebrationId(celebrationName);
        data.Celebrations.TryGetValue(id, out var proper);

        var own = Pick(proper, hour);
        if (own is not null)
        {
            return own;
        }

        var common = proper?.Common;
        return common is not null && data.Commons.TryGetValue(common, out var readings) ? Pick(readings, hour) : null;
    }

    /// <summary>The proper or Common Evening Prayer psalmody; a Common alone never replaces a memorial's weekday psalter.</summary>
    public static IReadOnlyList<string>? EveningPsalmody(string celebrationName, bool includeCommon = true)
    {
        var data = OfficeData.Sanctoral;
        if (!data.Celebrations.TryGetValue(OrdinaryFormOffice.CelebrationId(celebrationName), out var proper))
        {
            return null;
        }

        if (proper.EveningPsalmody is not null)
        {
            return proper.EveningPsalmody;
        }

        return includeCommon && proper.Common is not null && data.Commons.TryGetValue(proper.Common, out var common) ? common.EveningPsalmody : null;
    }

    internal static SanctoralReadings? Celebration(string name) =>
        OfficeData.Sanctoral.Celebrations.GetValueOrDefault(OrdinaryFormOffice.CelebrationId(name));

    /// <summary>The celebration's proper Morning Prayer psalmody; a Common alone does not replace a memorial's weekday psalter.</summary>
    public static IReadOnlyList<string>? MorningPsalmody(string celebrationName) =>
        OfficeData.Sanctoral.Celebrations.TryGetValue(OrdinaryFormOffice.CelebrationId(celebrationName), out var proper)
            ? proper.MorningPsalmody : null;

    private static string? Pick(SanctoralReadings? readings, Hour hour) => hour switch
    {
        Hour.MorningPrayer => readings?.Morning,
        Hour.EveningPrayer => readings?.Evening,
        _ => null,
    };
}
