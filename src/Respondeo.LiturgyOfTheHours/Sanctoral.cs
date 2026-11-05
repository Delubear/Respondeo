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

    private static string? Pick(SanctoralReadings? readings, Hour hour) => hour switch
    {
        Hour.MorningPrayer => readings?.Morning,
        Hour.EveningPrayer => readings?.Evening,
        _ => null,
    };
}
