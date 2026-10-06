using System.Globalization;
using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// A saint whose feast the Church keeps today: enough to render a gentle "today the Church celebrates…"
/// highlight that links back to the saint's profile.
/// </summary>
/// <param name="Title">Display title, e.g. "St. Francis of Assisi".</param>
/// <param name="Id">URL slug used to link to <c>discover/saints/{Id}</c>.</param>
/// <param name="Dates">Free-text life dates (e.g. "1181–1226"), or null when unknown.</param>
/// <param name="Summary">Short one-line description of the saint, or null when unavailable.</param>
public sealed record FeastHighlight(string Title, string Id, string? Dates = null, string? Summary = null);

/// <summary>
/// Finds the saint (if any) whose feast the Church keeps on a given day.
/// </summary>
public interface IFeastOfTheDay
{
    /// <summary>
    /// Returns the saint celebrated today whose General Roman Calendar celebration links to a profile,
    /// or <c>null</c> when today's celebration honors no saint in the catalog.
    /// </summary>
    Task<FeastHighlight?> GetTodayAsync();

    /// <summary>
    /// Returns the ids of every saint whose fixed-date feast falls today (empty when none do),
    /// so the browse list can highlight the matching cards without re-parsing feast dates itself.
    /// </summary>
    Task<IReadOnlySet<string>> GetTodayFeastIdsAsync();

    /// <summary>
    /// True when <paramref name="feastDay"/> is a fixed "Month Day" string that falls on today's date,
    /// so a page can flag a single known feast day without fetching the whole catalog.
    /// </summary>
    bool IsToday(string? feastDay);
}

/// <summary>
/// Default <see cref="IFeastOfTheDay"/> backed by the liturgical calendar and the bundled saints catalog.
/// The calendar is the source of truth for which saint is celebrated today; the catalog supplies the
/// canonical display title for the linked profile. The current date is injected so matching stays testable.
/// </summary>
/// <param name="saints">The bundled saints catalog.</param>
/// <param name="calendar">The liturgical calendar, which resolves the day's celebrations and their saint-profile links.</param>
/// <param name="today">Supplies the current date.</param>
internal sealed class FeastOfTheDay(ISaintService saints, ILiturgicalCalendar calendar, Func<DateOnly> today) : IFeastOfTheDay
{
    public async Task<FeastHighlight?> GetTodayAsync()
    {
        var now = today();
        var day = calendar.ForDate(now);

        // The calendar decides what is celebrated today and whether it honors a saint in the catalog.
        // Prefer the principal celebration, then any optional memorial that links to a profile.
        var linked = LinkedCelebrations(day).FirstOrDefault();
        if (linked?.Id is not { } id)
        {
            return null;
        }

        // Use the catalog's canonical title when the profile exists, falling back to the dataset name.
        var record = await saints.GetByIdAsync(id);
        return new FeastHighlight(record?.Title ?? linked.Name, id, record?.Dates, record?.Summary);
    }

    public async Task<IReadOnlySet<string>> GetTodayFeastIdsAsync()
    {
        var now = today();
        var index = await saints.GetIndexAsync();

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in index.Entries)
        {
            var record = await saints.GetByIdAsync(entry.Id);
            if (record is not null && TryMatchFixedDate(record.FeastDay, now))
            {
                ids.Add(record.Id);
            }
        }

        return ids;
    }

    public bool IsToday(string? feastDay) => TryMatchFixedDate(feastDay, today());

    /// <summary>
    /// The celebrations claiming <paramref name="day"/> that link to a saint profile, principal first,
    /// then the optional memorials, in calendar order.
    /// </summary>
    private static IEnumerable<LiturgicalCelebration> LinkedCelebrations(LiturgicalDay day)
    {
        if (day.Celebration is { Id: not null } principal)
        {
            yield return principal;
        }

        foreach (var memorial in day.OptionalMemorials.Where(m => m.Id is not null))
        {
            yield return memorial;
        }
    }

    /// <summary>
    /// True when <paramref name="feastDay"/> is a fixed "Month Day" string (e.g. "October 4") that falls on <paramref name="date"/>.
    /// Blank, moveable, or otherwise unparseable values return false.
    /// </summary>
    internal static bool TryMatchFixedDate(string? feastDay, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(feastDay))
        {
            return false;
        }

        // Parse day-and-month only, independent of year, using a stable reference year so that feasts like "February 29" still fail gracefully in non-leap target years.
        var text = $"{feastDay.Trim()} {date.Year}";

        if (!DateTime.TryParseExact(text, ["MMMM d yyyy", "MMM d yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        return parsed.Month == date.Month && parsed.Day == date.Day;
    }
}
