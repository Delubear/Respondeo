using System.Globalization;
using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// A saint whose fixed-date feast falls on the day being asked about: enough to render a gentle
/// "today the Church celebrates…" highlight that links back to the saint's profile.
/// </summary>
/// <param name="Title">Display title, e.g. "St. Francis of Assisi".</param>
/// <param name="Id">URL slug used to link to <c>discover/saints/{Id}</c>.</param>
public sealed record FeastHighlight(string Title, string Id);

/// <summary>
/// Finds the saint (if any) whose feast the Church keeps on a given day. v1 matches only fixed
/// calendar dates such as "October 4"; moveable feasts (e.g. "Corpus Christi") are ignored.
/// </summary>
public interface IFeastOfTheDay
{
    /// <summary>Returns the saint whose fixed-date feast falls today, or <c>null</c> when none matches.</summary>
    Task<FeastHighlight?> GetTodayAsync();
}

/// <summary>
/// Default <see cref="IFeastOfTheDay"/> backed by the bundled saints catalog. The current date is
/// injected so the matching logic stays deterministic and unit-testable.
/// </summary>
internal sealed class FeastOfTheDay(ISaintService saints, Func<DateOnly> today) : IFeastOfTheDay
{
    public async Task<FeastHighlight?> GetTodayAsync()
    {
        var now = today();
        var index = await saints.GetIndexAsync();

        foreach (var entry in index.Entries)
        {
            var record = await saints.GetByIdAsync(entry.Id);
            if (record is null || !TryMatchFixedDate(record.FeastDay, now))
            {
                continue;
            }

            return new FeastHighlight(record.Title, record.Id);
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="feastDay"/> is a fixed "Month Day" string (e.g. "October 4") that
    /// falls on <paramref name="date"/>. Blank, moveable, or otherwise unparseable values return false.
    /// </summary>
    internal static bool TryMatchFixedDate(string? feastDay, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(feastDay))
        {
            return false;
        }

        // Parse day-and-month only, independent of year, using a stable reference year so that
        // feasts like "February 29" still fail gracefully in non-leap target years.
        var text = $"{feastDay.Trim()} {date.Year}";

        if (!DateTime.TryParseExact(
                text,
                ["MMMM d yyyy", "MMM d yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return false;
        }

        return parsed.Month == date.Month && parsed.Day == date.Day;
    }
}
