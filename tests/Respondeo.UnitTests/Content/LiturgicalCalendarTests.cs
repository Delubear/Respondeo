using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Unit tests for the Ordinary Form liturgical calendar engine (<see cref="RomanCalendar"/> and the
/// <see cref="Computus"/> Easter helper). Dates are checked against the published Roman calendar.
/// </summary>
public class LiturgicalCalendarTests
{
    private static readonly ILiturgicalCalendar Calendar = new RomanCalendar();

    [Theory]
    [InlineData(2023, 4, 9)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2000, 4, 23)]
    public void Computus_returns_known_Easter_dates(int year, int month, int day) => Assert.Equal(new DateOnly(year, month, day), Computus.GregorianEaster(year));

    [Fact]
    public void Christmas_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 25));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
        Assert.Equal("The Nativity of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Theory]
    [InlineData(2025, 12, 1)]
    [InlineData(2025, 12, 2)]
    public void Advent_days_are_violet(int year, int month, int day)
    {
        var result = Calendar.ForDate(new DateOnly(year, month, day));

        Assert.Equal(LiturgicalSeason.Advent, result.Season);
        Assert.Equal(LiturgicalColor.Violet, result.Color);
    }

    [Fact]
    public void First_Sunday_of_Advent_2025_is_the_thirtieth_of_November()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 11, 30));

        Assert.Equal(LiturgicalSeason.Advent, day.Season);
    }

    [Fact]
    public void Day_before_Advent_is_Ordinary_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 11, 29));

        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(LiturgicalColor.Green, day.Color);
    }

    [Fact]
    public void Gaudete_Sunday_is_rose()
    {
        // Third Sunday of Advent 2025 = 14 December.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 14));

        Assert.Equal(LiturgicalColor.Rose, day.Color);
    }

    [Fact]
    public void Ash_Wednesday_2025_opens_Lent()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 3, 5));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal("Ash Wednesday", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.Violet, day.Color);
    }

    [Fact]
    public void Laetare_Sunday_2025_is_rose()
    {
        // Fourth Sunday of Lent 2025 = 30 March.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 30));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal(LiturgicalColor.Rose, day.Color);
    }

    [Fact]
    public void Holy_Thursday_begins_the_Triduum()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 17));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
    }

    [Fact]
    public void Easter_Sunday_2025_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 20));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Easter_Monday_is_Easter_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 21));

        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Pentecost_2025_is_red_and_ends_Easter_Time()
    {
        // Easter 2025 = 20 Apr; Pentecost = +49 days = 8 June.
        var day = Calendar.ForDate(new DateOnly(2025, 6, 8));

        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.Equal("Pentecost Sunday", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void Day_after_Pentecost_is_Ordinary_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 6, 9));

        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
    }

    [Fact]
    public void New_Years_Day_is_still_Christmas_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 1, 1));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
        Assert.Equal("Mary, the Holy Mother of God", day.Celebration?.Name);
    }

    [Fact]
    public void Baptism_of_the_Lord_2025_closes_Christmas_Time()
    {
        // Epiphany 6 Jan 2025 (Monday); Baptism = following Sunday = 12 Jan.
        var baptism = Calendar.ForDate(new DateOnly(2025, 1, 12));
        var dayAfter = Calendar.ForDate(new DateOnly(2025, 1, 13));

        Assert.Equal(LiturgicalSeason.Christmas, baptism.Season);
        Assert.Equal(LiturgicalSeason.OrdinaryTime, dayAfter.Season);
    }

    [Fact]
    public void Ordinary_weekday_has_no_celebration()
    {
        // 1 July 2025 (Tuesday) carries no celebration in the general calendar.
        var day = Calendar.ForDate(new DateOnly(2025, 7, 1));

        Assert.Null(day.Celebration);
        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(LiturgicalColor.Green, day.Color);
        Assert.Equal("Tuesday of the Thirteenth Week in Ordinary Time", day.FerialName);
    }

    [Fact]
    public void Assumption_is_a_fixed_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 8, 15));

        Assert.Equal("The Assumption of the Blessed Virgin Mary", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Obligatory_memorial_is_the_principal_on_an_ordinary_weekday()
    {
        // 15 July 2025 (Tuesday) is the Memorial of Saint Bonaventure.
        var day = Calendar.ForDate(new DateOnly(2025, 7, 15));

        Assert.Equal("Saint Bonaventure, Bishop and Doctor of the Church", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Memorial, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Optional_memorial_is_offered_but_not_the_principal()
    {
        // 13 January 2025 (Monday) is the optional Memorial of Saint Hilary.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 13));

        Assert.Null(day.Celebration);
        var memorial = Assert.Single(day.OptionalMemorials);
        Assert.Equal("Saint Hilary, Bishop and Doctor of the Church", memorial.Name);
    }

    [Fact]
    public void Feast_of_the_Lord_replaces_an_Ordinary_Time_Sunday()
    {
        // 2 February 2025 is a Sunday; the Presentation of the Lord outranks it.
        var day = Calendar.ForDate(new DateOnly(2025, 2, 2));

        Assert.Equal("The Presentation of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
    }

    [Fact]
    public void A_saints_feast_yields_to_a_Sunday_of_Advent()
    {
        // 30 November 2025 is both Saint Andrew (a feast) and the First Sunday of Advent; the Sunday wins.
        var day = Calendar.ForDate(new DateOnly(2025, 11, 30));

        Assert.Null(day.Celebration);
        Assert.Equal(LiturgicalSeason.Advent, day.Season);
        Assert.Equal("First Sunday of Advent", day.FerialName);
    }

    [Fact]
    public void An_obligatory_memorial_is_reduced_to_optional_on_a_Lenten_weekday()
    {
        // 7 March 2025 (Friday of Lent) is the Memorial of Saints Perpetua and Felicity, made optional.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 7));

        Assert.Null(day.Celebration);
        var memorial = Assert.Single(day.OptionalMemorials);
        Assert.Equal("Saints Perpetua and Felicity, Martyrs", memorial.Name);
    }

    [Fact]
    public void Christ_the_King_is_the_last_Sunday_before_Advent()
    {
        // Advent 2025 begins 30 November, so Christ the King is 23 November.
        var day = Calendar.ForDate(new DateOnly(2025, 11, 23));

        Assert.Equal("Our Lord Jesus Christ, King of the Universe", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Holy_Family_is_the_Sunday_within_the_Christmas_Octave()
    {
        // Christmas 2025 is a Thursday, so the Holy Family falls on Sunday 28 December.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 28));

        Assert.Equal("The Holy Family of Jesus, Mary and Joseph", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
    }

    [Fact]
    public void Baptism_of_the_Lord_is_a_named_feast()
    {
        // Epiphany 6 Jan 2025 (Monday); Baptism = following Sunday = 12 Jan.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 12));

        Assert.Equal("The Baptism of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
    }

    [Fact]
    public void Lenten_weekday_carries_a_ferial_name()
    {
        // 10 March 2025 is the Monday of the First Week of Lent.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 10));

        Assert.Null(day.Celebration);
        Assert.Equal("Monday of the First Week of Lent", day.FerialName);
    }

    [Fact]
    public void Annunciation_falls_on_its_ordinary_date_when_free()
    {
        // 25 March 2025 is a Tuesday in Lent, unimpeded.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 25));

        Assert.Equal("The Annunciation of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Annunciation_impeded_by_Holy_Week_is_suppressed_on_its_ordinary_date()
    {
        // Easter 2024 is 31 March, so 25 March 2024 is Monday of Holy Week.
        var day = Calendar.ForDate(new DateOnly(2024, 3, 25));

        Assert.NotEqual("The Annunciation of the Lord", day.Celebration?.Name);
    }

    [Fact]
    public void Annunciation_impeded_by_Holy_Week_transfers_after_the_Octave_of_Easter()
    {
        // Monday after the Second Sunday of Easter 2024 = 8 April 2024.
        var day = Calendar.ForDate(new DateOnly(2024, 4, 8));

        Assert.Equal("The Annunciation of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Saint_Joseph_impeded_by_a_Lenten_Sunday_transfers_to_the_following_Monday()
    {
        // 19 March 2017 is the Third Sunday of Lent, so Saint Joseph moves to Monday 20 March 2017.
        var impeded = Calendar.ForDate(new DateOnly(2017, 3, 19));
        Assert.NotEqual("Saint Joseph, Spouse of the Blessed Virgin Mary", impeded.Celebration?.Name);

        var transferred = Calendar.ForDate(new DateOnly(2017, 3, 20));
        Assert.Equal("Saint Joseph, Spouse of the Blessed Virgin Mary", transferred.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, transferred.Celebration?.Rank);
    }

    [Fact]
    public void A_wired_memorial_links_to_its_saint_profile()
    {
        // 4 October 2025 is a Saturday: Saint Francis is the principal celebration and links to his profile.
        var day = Calendar.ForDate(new DateOnly(2025, 10, 4));

        Assert.Equal("Saint Francis of Assisi", day.Celebration?.Name);
        Assert.Equal("francis-of-assisi", day.Celebration?.Id);
    }

    [Fact]
    public void An_unlinked_celebration_has_no_saint_profile_id()
    {
        // 25 December has no saint profile slug in the dataset.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 25));

        Assert.Null(day.Celebration?.Id);
    }
}
