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

    // -----------------------------------------------------------------------
    // Fixed solemnities of the universal calendar.
    // -----------------------------------------------------------------------

    [Fact]
    public void Epiphany_is_a_white_solemnity_within_Christmas_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 1, 6));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
        Assert.Equal("The Epiphany of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Immaculate_Conception_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 8));

        Assert.Equal("The Immaculate Conception of the Blessed Virgin Mary", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void All_Saints_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 11, 1));

        Assert.Equal("All Saints", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Nativity_of_John_the_Baptist_is_a_fixed_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 6, 24));

        Assert.Equal("The Nativity of Saint John the Baptist", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Saints_Peter_and_Paul_is_a_red_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 6, 29));

        Assert.Equal("Saints Peter and Paul, Apostles", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    // -----------------------------------------------------------------------
    // Moveable solemnities, each a fixed offset from Easter.
    // -----------------------------------------------------------------------

    [Fact]
    public void Palm_Sunday_2025_is_a_red_solemnity_in_Lent()
    {
        // Easter 2025 = 20 April; Palm Sunday = 13 April.
        var day = Calendar.ForDate(new DateOnly(2025, 4, 13));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal("Palm Sunday of the Passion of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void Good_Friday_2025_is_red_and_in_the_Triduum()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 18));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
        Assert.Equal("Good Friday of the Passion of the Lord", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void Holy_Saturday_2025_is_in_the_Triduum()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 19));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
        Assert.Equal("Holy Saturday", day.Celebration?.Name);
    }

    [Fact]
    public void Ascension_2025_is_a_white_solemnity_in_Easter_Time()
    {
        // Easter 2025 = 20 April; Ascension = +39 days = 29 May.
        var day = Calendar.ForDate(new DateOnly(2025, 5, 29));

        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.Equal("The Ascension of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Most_Holy_Trinity_2025_is_the_Sunday_after_Pentecost()
    {
        // Easter 2025 = 20 April; Trinity = +56 days = 15 June.
        var day = Calendar.ForDate(new DateOnly(2025, 6, 15));

        Assert.Equal("The Most Holy Trinity", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Corpus_Christi_2025_is_a_white_solemnity()
    {
        // Easter 2025 = 20 April; Corpus Christi = +60 days = 19 June.
        var day = Calendar.ForDate(new DateOnly(2025, 6, 19));

        Assert.Equal("The Most Holy Body and Blood of Christ", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Sacred_Heart_2025_is_a_white_solemnity()
    {
        // Easter 2025 = 20 April; Sacred Heart = +68 days = 27 June.
        var day = Calendar.ForDate(new DateOnly(2025, 6, 27));

        Assert.Equal("The Most Sacred Heart of Jesus", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    // -----------------------------------------------------------------------
    // Feasts of the Lord that replace a Sunday of Ordinary Time.
    // -----------------------------------------------------------------------

    [Fact]
    public void Transfiguration_replaces_an_Ordinary_Time_Sunday()
    {
        // 6 August 2023 is a Sunday; the Transfiguration (a feast of the Lord) outranks it.
        var day = Calendar.ForDate(new DateOnly(2023, 8, 6));

        Assert.Equal("The Transfiguration of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
    }

    [Fact]
    public void Exaltation_of_the_Holy_Cross_replaces_an_Ordinary_Time_Sunday()
    {
        // 14 September 2025 is a Sunday; the Exaltation of the Holy Cross outranks it.
        var day = Calendar.ForDate(new DateOnly(2025, 9, 14));

        Assert.Equal("The Exaltation of the Holy Cross", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void All_Souls_replaces_an_Ordinary_Time_Sunday()
    {
        // 2 November 2025 is a Sunday; the Commemoration of All the Faithful Departed outranks it.
        var day = Calendar.ForDate(new DateOnly(2025, 11, 2));

        Assert.Equal("The Commemoration of All the Faithful Departed", day.Celebration?.Name);
    }

    [Fact]
    public void Dedication_of_the_Lateran_Basilica_replaces_an_Ordinary_Time_Sunday()
    {
        // 9 November 2025 is a Sunday; the Dedication of the Lateran Basilica outranks it.
        var day = Calendar.ForDate(new DateOnly(2025, 11, 9));

        Assert.Equal("The Dedication of the Lateran Basilica", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, day.Celebration?.Rank);
    }

    [Fact]
    public void An_ordinary_saints_feast_does_not_replace_a_Sunday()
    {
        // 25 April 2027 is a Sunday; Saint Mark (a saint's feast, not of the Lord) is omitted.
        var day = Calendar.ForDate(new DateOnly(2027, 4, 25));

        Assert.Null(day.Celebration);
        Assert.Equal(LiturgicalSeason.Easter, day.Season);
    }

    // -----------------------------------------------------------------------
    // Memorials and optional memorials.
    // -----------------------------------------------------------------------

    [Fact]
    public void A_martyrs_memorial_is_red()
    {
        // 21 January 2025 is the Memorial of Saint Agnes, Virgin and Martyr.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 21));

        Assert.Equal("Saint Agnes, Virgin and Martyr", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Memorial, day.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void Several_optional_memorials_are_all_offered_on_an_ordinary_weekday()
    {
        // 20 January 2025 (Monday) offers both Saint Fabian and Saint Sebastian as optional memorials.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 20));

        Assert.Null(day.Celebration);
        Assert.Equal(2, day.OptionalMemorials.Count);
        Assert.Contains(day.OptionalMemorials, m => m.Name == "Saint Fabian, Pope and Martyr");
        Assert.Contains(day.OptionalMemorials, m => m.Name == "Saint Sebastian, Martyr");
    }

    [Fact]
    public void Optional_memorials_are_suppressed_on_a_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 25));

        Assert.Empty(day.OptionalMemorials);
    }

    [Fact]
    public void Optional_memorials_are_suppressed_on_a_Sunday()
    {
        // 19 January 2025 is the Second Sunday in Ordinary Time: no memorials are offered.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 19));

        Assert.Empty(day.OptionalMemorials);
    }

    // -----------------------------------------------------------------------
    // Color and celebration coupling.
    // -----------------------------------------------------------------------

    [Fact]
    public void A_solemnity_color_overrides_the_season_color()
    {
        // 25 March 2025 is a weekday in Lent (season color violet), but the Annunciation is white.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 25));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void A_day_with_a_principal_celebration_has_no_ferial_name()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 25));

        Assert.NotNull(day.Celebration);
        Assert.Null(day.FerialName);
    }

    [Fact]
    public void An_authored_summary_is_exposed_on_the_celebration()
    {
        // The Assumption carries an authored one-line summary in the dataset.
        var day = Calendar.ForDate(new DateOnly(2025, 8, 15));

        Assert.False(string.IsNullOrWhiteSpace(day.Celebration?.Summary));
    }

    // -----------------------------------------------------------------------
    // Ferial naming across the seasons.
    // -----------------------------------------------------------------------

    [Fact]
    public void Advent_weekday_carries_a_ferial_name()
    {
        // 2 December 2025 (Tuesday) in the First Week of Advent.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 2));

        Assert.Null(day.Celebration);
        Assert.Equal("Tuesday of the First Week of Advent", day.FerialName);
    }

    [Fact]
    public void Day_after_Ash_Wednesday_is_named_relative_to_it()
    {
        // 6 March 2025 (Thursday) falls before the First Sunday of Lent.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 6));

        Assert.Equal("Thursday after Ash Wednesday", day.FerialName);
    }

    [Fact]
    public void Holy_Week_weekday_carries_a_ferial_name()
    {
        // 15 April 2025 (Tuesday) is in Holy Week (Easter = 20 April).
        var day = Calendar.ForDate(new DateOnly(2025, 4, 15));

        Assert.Equal("Tuesday of Holy Week", day.FerialName);
    }

    [Fact]
    public void Easter_Octave_weekday_carries_a_ferial_name()
    {
        // 22 April 2025 (Tuesday) is within the Octave of Easter.
        var day = Calendar.ForDate(new DateOnly(2025, 4, 22));

        Assert.Equal("Tuesday within the Octave of Easter", day.FerialName);
    }

    [Fact]
    public void Christmas_Octave_weekday_is_numbered_within_the_octave()
    {
        // 29 December 2025 (Monday) is the Fifth Day within the Octave of the Nativity.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 29));

        Assert.Equal("Fifth Day within the Octave of the Nativity", day.FerialName);
    }

    [Fact]
    public void Ordinary_Time_first_span_weekday_is_named_from_the_Baptism_of_the_Lord()
    {
        // 14 January 2025 (Tuesday) in the First Week of Ordinary Time (Baptism = 12 January).
        var day = Calendar.ForDate(new DateOnly(2025, 1, 14));

        Assert.Equal("Tuesday of the First Week in Ordinary Time", day.FerialName);
    }

    [Fact]
    public void An_Ordinary_Time_Sunday_is_named_as_a_Sunday()
    {
        // 19 January 2025 is the Second Sunday in Ordinary Time.
        var day = Calendar.ForDate(new DateOnly(2025, 1, 19));

        Assert.Null(day.Celebration);
        Assert.Equal("Second Sunday in Ordinary Time", day.FerialName);
    }

    [Fact]
    public void Ordinary_Time_second_span_weekday_is_numbered_back_from_Christ_the_King()
    {
        // 1 July 2025 (Tuesday) is in the second span of Ordinary Time, the Thirteenth Week.
        var day = Calendar.ForDate(new DateOnly(2025, 7, 1));

        Assert.Equal("Tuesday of the Thirteenth Week in Ordinary Time", day.FerialName);
    }

    [Fact]
    public void The_last_week_of_Ordinary_Time_is_the_Thirty_Fourth()
    {
        // 25 November 2025 (Tuesday) follows Christ the King (23 Nov), the Thirty-Fourth Week before Advent.
        var day = Calendar.ForDate(new DateOnly(2025, 11, 25));

        Assert.Equal("Tuesday of the Thirty-Fourth Week in Ordinary Time", day.FerialName);
    }

    // -----------------------------------------------------------------------
    // Data loader behavior.
    // -----------------------------------------------------------------------

    [Fact]
    public void Fixed_date_lookup_returns_empty_for_a_day_with_no_entries()
    {
        // 1 July has no fixed celebration in the general calendar dataset.
        Assert.Empty(GeneralRomanCalendarData.ForMonthDay(7, 1));
    }

    [Fact]
    public void Fixed_date_lookup_returns_every_celebration_sharing_a_date()
    {
        // 20 January has two optional memorials in the dataset.
        var celebrations = GeneralRomanCalendarData.ForMonthDay(1, 20);

        Assert.Equal(2, celebrations.Count);
    }

    // -----------------------------------------------------------------------
    // Season boundaries.
    // -----------------------------------------------------------------------

    [Fact]
    public void Christmas_Eve_is_the_last_day_of_Advent()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 24));

        Assert.Equal(LiturgicalSeason.Advent, day.Season);
    }

    [Fact]
    public void The_last_day_of_the_year_is_still_Christmas_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 31));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
    }

    [Fact]
    public void The_day_before_Ash_Wednesday_is_Ordinary_Time()
    {
        // Ash Wednesday 2025 = 5 March, so Shrove Tuesday 4 March is still Ordinary Time.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 4));

        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(LiturgicalColor.Green, day.Color);
    }

    [Fact]
    public void Holy_Family_falls_on_30_December_when_Christmas_is_a_Sunday()
    {
        // Christmas 2022 is a Sunday, so the Holy Family is kept on 30 December.
        var day = Calendar.ForDate(new DateOnly(2022, 12, 30));

        Assert.Equal("The Holy Family of Jesus, Mary and Joseph", day.Celebration?.Name);
    }

    [Fact]
    public void Baptism_of_the_Lord_is_the_Sunday_after_Epiphany_when_Epiphany_is_a_Sunday()
    {
        // 6 January 2019 is a Sunday (Epiphany), so the Baptism is the following Sunday, 13 January.
        var day = Calendar.ForDate(new DateOnly(2019, 1, 13));

        Assert.Equal("The Baptism of the Lord", day.Celebration?.Name);
        Assert.Equal(LiturgicalSeason.Christmas, day.Season);

        var dayAfter = Calendar.ForDate(new DateOnly(2019, 1, 14));
        Assert.Equal(LiturgicalSeason.OrdinaryTime, dayAfter.Season);
    }
}
