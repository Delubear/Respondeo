using Respondeo.LiturgicalCalendar;

namespace Respondeo.Services;

/// <summary>
/// Chooses the masthead portrait of St. Thomas Aquinas for the day, letting him mark the great seasons:
/// a festive hat through Christmas Time, ashes on Ash Wednesday, and his usual likeness the rest of the year.
/// </summary>
public interface ISeasonalPortrait
{
    /// <summary>The portrait image path (relative to wwwroot) proper to today.</summary>
    string CurrentImagePath { get; }
}

/// <summary>
/// Default <see cref="ISeasonalPortrait"/>: maps the day the liturgical calendar resolves to a portrait asset.
/// The current date is injected so the mapping stays deterministic and testable.
/// </summary>
/// <param name="calendar">The liturgical calendar, the source of truth for the day's season and celebration.</param>
/// <param name="today">Supplies the current date.</param>
internal sealed class SeasonalPortrait(ILiturgicalCalendar calendar, Func<DateOnly> today) : ISeasonalPortrait
{
    /// <summary>The display name the calendar gives Ash Wednesday; the hook for its dedicated portrait.</summary>
    internal const string AshWednesdayName = "Ash Wednesday";

    /// <summary>The ordinary, year-round portrait.</summary>
    internal const string DefaultImagePath = "images/aquinas.webp";

    /// <summary>The festive portrait shown throughout Christmas Time.</summary>
    internal const string ChristmasImagePath = "images/aquinas-christmas.webp";

    /// <summary>The ashes portrait shown on Ash Wednesday.</summary>
    internal const string AshWednesdayImagePath = "images/aquinas-ash-wednesday.webp";

    public string CurrentImagePath => Resolve(calendar.ForDate(today()));

    /// <summary>
    /// Pure mapping from a resolved <see cref="LiturgicalDay"/> to its portrait asset.
    /// Ash Wednesday takes precedence over its season so the single penitential day reads before Christmas Time.
    /// </summary>
    internal static string Resolve(LiturgicalDay day) => day switch
    {
        { Celebration.Name: AshWednesdayName } => AshWednesdayImagePath,
        { Season: LiturgicalSeason.Christmas } => ChristmasImagePath,
        _ => DefaultImagePath,
    };
}
