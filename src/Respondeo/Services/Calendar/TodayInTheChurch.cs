using Respondeo.LiturgicalCalendar;

namespace Respondeo.Services.Calendar;

/// <summary>
/// The display text for the "Today in the Church" dialog: a liturgical day turned into the season name,
/// color wording, day label, and the single celebration card to show. Pure and synchronous, so it is unit-testable without rendering the orb.
/// </summary>
public sealed class TodayInTheChurch
{
    private readonly LiturgicalDay _day;
    private readonly IReadOnlyList<LiturgicalCelebration> _optionalMemorials;
    private int _index;

    private TodayInTheChurch(LiturgicalDay day, int startIndex)
    {
        _day = day;
        _optionalMemorials = day.Celebration is null ? day.OptionalMemorials : [];
        _index = startIndex;
    }

    /// <summary>
    /// Builds the view for a day. On a ferial day with several optional memorials, the first one shown is picked at random,
    /// and the reader can step through the rest with <see cref="Next"/> and <see cref="Previous"/>.
    /// </summary>
    public static TodayInTheChurch For(LiturgicalDay day, Random? random = null)
    {
        var count = day.Celebration is null ? day.OptionalMemorials.Count : 0;
        var start = count > 1 ? (random ?? Random.Shared).Next(count) : 0;
        return new TodayInTheChurch(day, start);
    }

    /// <summary>
    /// The celebration shown as a card: the principal celebration when there is one, otherwise the currently selected optional memorial of a ferial day.
    /// </summary>
    public LiturgicalCelebration? Celebration => _day.Celebration ?? (_optionalMemorials.Count > 0 ? _optionalMemorials[_index] : null);

    /// <summary>True when the card is an optional memorial rather than the day's principal celebration.</summary>
    public bool IsOptionalMemorial => _day.Celebration is null && _optionalMemorials.Count > 0;

    /// <summary>The number of optional memorials the reader can step through (zero when the day has a principal celebration).</summary>
    public int OptionalMemorialCount => _optionalMemorials.Count;

    /// <summary>The zero-based position of the optional memorial currently shown.</summary>
    public int OptionalMemorialIndex => _index;

    /// <summary>True when there is more than one optional memorial, so previous/next controls should be shown.</summary>
    public bool CanCycle => _optionalMemorials.Count > 1;

    /// <summary>
    /// The color the selected optional memorial would wear if it is kept, when that differs from the day's color; otherwise null.
    /// An optional memorial does not change the day's color unless the celebrant chooses to keep it.
    /// </summary>
    public LiturgicalColor? OptionalMemorialColor =>
        IsOptionalMemorial && Celebration is { } memorial && memorial.Color != _day.Color ? memorial.Color : null;

    /// <summary>Shows the next optional memorial, wrapping to the first.</summary>
    public void Next()
    {
        if (CanCycle)
        {
            _index = (_index + 1) % _optionalMemorials.Count;
        }
    }

    /// <summary>Shows the previous optional memorial, wrapping to the last.</summary>
    public void Previous()
    {
        if (CanCycle)
        {
            _index = (_index - 1 + _optionalMemorials.Count) % _optionalMemorials.Count;
        }
    }

    /// <summary>Slugs of every saint the day links to, so their profiles can be loaded together.</summary>
    public IEnumerable<string> LinkedSlugs()
    {
        if (_day.Celebration?.Id is { } celebrationId)
        {
            yield return celebrationId;
        }

        foreach (var memorial in _day.OptionalMemorials)
        {
            if (memorial.Id is { } memorialId)
            {
                yield return memorialId;
            }
        }
    }

    /// <summary>A short lead-in above the card, phrased to fit the celebration's rank.</summary>
    public string CelebrationIntro
    {
        get
        {
            if (IsOptionalMemorial)
            {
                return "The Church also remembers today";
            }

            return _day.Celebration?.Rank switch
            {
                CelebrationRank.Solemnity => "Today the Church celebrates",
                CelebrationRank.Feast => "Today the Church keeps the feast of",
                _ => "Today the Church honors",
            };
        }
    }

    /// <summary>
    /// A short explanation shown only when the day's color departs from its season's usual color,
    /// so the reader understands why today looks different (e.g. white for a saint during green Ordinary Time).
    /// </summary>
    public string? ColorClue
    {
        get
        {
            if (_day.Color == SeasonDefaultColor)
            {
                return null;
            }

            return _day.Color switch
            {
                LiturgicalColor.White => "White today — honoring the Lord, Our Lady, or a saint who was not a martyr.",
                LiturgicalColor.Red => "Red today — for a martyr, an apostle, or the Holy Spirit.",
                LiturgicalColor.Rose => "Rose today — a note of joy that lightens this penitential season.",
                LiturgicalColor.Violet => "Violet today — a penitential note set within the season.",
                LiturgicalColor.Green => "Green today — the ordinary color returns for this day.",
                _ => null,
            };
        }
    }

    // The color a day would wear from its season alone, before any celebration overrides it.
    private LiturgicalColor SeasonDefaultColor => _day.Season switch
    {
        LiturgicalSeason.Advent => LiturgicalColor.Violet,
        LiturgicalSeason.Lent => LiturgicalColor.Violet,
        LiturgicalSeason.Christmas => LiturgicalColor.White,
        LiturgicalSeason.Easter => LiturgicalColor.White,
        LiturgicalSeason.Triduum => LiturgicalColor.Red,
        _ => LiturgicalColor.Green,
    };

    public string SeasonName => _day.Season switch
    {
        LiturgicalSeason.Advent => "Advent",
        LiturgicalSeason.Christmas => "Christmas Time",
        LiturgicalSeason.Lent => "Lent",
        LiturgicalSeason.Triduum => "The Sacred Paschal Triduum",
        LiturgicalSeason.Easter => "Easter Time",
        _ => "Ordinary Time",
    };

    public string ColorName => _day.Color.ToString();

    public string ColorClass => _day.Color.ToString().ToLowerInvariant();

    /// <summary>
    /// The headline's second line. On a ferial day it is the "Monday of the Nth Week…" title;
    /// on a celebration day it names the day by rank, e.g. "The Feast of the Transfiguration" or "The Memorial of Saint Benedict".
    /// </summary>
    public string DayLabel
    {
        get
        {
            if (_day.Celebration is not { } celebration)
            {
                return _day.FerialName ?? SeasonName;
            }

            var name = LowerLeadingThe(celebration.Name);

            return celebration.Rank switch
            {
                CelebrationRank.Solemnity => $"The Solemnity of {name}",
                CelebrationRank.Feast => $"The Feast of {name}",
                _ => $"The Memorial of {name}",
            };
        }
    }

    // Lowercases a leading "The " so rank-prefixed labels read naturally: "The Feast of the Transfiguration"
    // and "The Memorial of the Passion of Saint John the Baptist", while plain "Saint Benedict" is untouched.
    private static string LowerLeadingThe(string name) => name.StartsWith("The ", StringComparison.Ordinal) ? string.Concat("the ", name.AsSpan(4)) : name;
}
