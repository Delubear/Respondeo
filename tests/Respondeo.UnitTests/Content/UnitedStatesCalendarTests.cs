using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Verifies the USCCB (United States) calendar overlay and the calendar preference serialization.
/// </summary>
public class UnitedStatesCalendarTests
{
    private static readonly RomanCalendar Us = new(new LiturgicalCalendarOptions(CalendarRegion.UnitedStates));
    private static readonly RomanCalendar UsThursday = new(new LiturgicalCalendarOptions(CalendarRegion.UnitedStates, AscensionOnThursday: true));
    private static readonly RomanCalendar General = new();

    [Theory]
    [InlineData(2026, 5, 17, "The Ascension of the Lord")]
    [InlineData(2026, 6, 7, "The Most Holy Body and Blood of Christ")]
    [InlineData(2026, 1, 4, "The Epiphany of the Lord")]
    [InlineData(2026, 1, 11, "The Baptism of the Lord")]
    [InlineData(2028, 1, 2, "The Epiphany of the Lord")]
    [InlineData(2024, 1, 7, "The Epiphany of the Lord")]
    [InlineData(2024, 1, 8, "The Baptism of the Lord")]
    public void UnitedStates_PlacesTransferredCelebrations(int year, int month, int day, string expected) =>
        Assert.Equal(expected, Us.ForDate(new DateOnly(year, month, day)).Celebration?.Name);

    [Fact]
    public void UnitedStates_AscensionOnThursday_WhenOptionSet() =>
        Assert.Equal("The Ascension of the Lord", UsThursday.ForDate(new DateOnly(2026, 5, 14)).Celebration?.Name);

    [Theory]
    [InlineData(2026, 5, 14, "The Ascension of the Lord")]
    [InlineData(2026, 6, 4, "The Most Holy Body and Blood of Christ")]
    public void GeneralRoman_KeepsThursdayCelebrations(int year, int month, int day, string expected) =>
        Assert.Equal(expected, General.ForDate(new DateOnly(year, month, day)).Celebration?.Name);

    [Fact]
    public void UnitedStates_IncludesSaintKateri()
    {
        Assert.Contains("Kateri", Us.ForDate(new DateOnly(2026, 7, 14)).Celebration?.Name ?? string.Empty);
        Assert.DoesNotContain("Kateri", General.ForDate(new DateOnly(2026, 7, 14)).Celebration?.Name ?? string.Empty);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Preference_RoundTrips(string stored) =>
        Assert.Equal(stored, PreferredLiturgicalCalendar.Serialize(PreferredLiturgicalCalendar.Deserialize(stored)));

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public void Preference_InvalidValue_FallsBackToDefault(string? stored) =>
        Assert.Equal(LiturgicalCalendarOptions.Default, PreferredLiturgicalCalendar.Deserialize(stored));

    [Fact]
    public void UnitedStates_DayOfPrayer_MovesToMonday_WhenJan22IsSunday()
    {
        Assert.DoesNotContain("Day of Prayer", Name(Us, 2023, 1, 22));
        Assert.Equal("Day of Prayer for the Legal Protection of Unborn Children", Name(Us, 2023, 1, 23));
    }

    [Fact]
    public void UnitedStates_DayOfPrayer_StaysOnJan22_WhenNotSunday()
    {
        Assert.Equal("Day of Prayer for the Legal Protection of Unborn Children", Name(Us, 2024, 1, 22));
        Assert.DoesNotContain("Day of Prayer", Name(Us, 2024, 1, 23));
    }

    [Fact]
    public void GeneralRoman_HasNoDayOfPrayer() =>
        Assert.DoesNotContain("Day of Prayer", Name(General, 2023, 1, 23));

    private static string Name(RomanCalendar calendar, int y, int m, int d) =>
        calendar.ForDate(new DateOnly(y, m, d)).Celebration?.Name ?? string.Empty;

    private static IEnumerable<string> Optional(RomanCalendar calendar, int y, int m, int d) =>
        calendar.ForDate(new DateOnly(y, m, d)).OptionalMemorials.Select(c => c.Name);

    [Theory]
    [InlineData(2026, 5, 25)]
    [InlineData(2025, 6, 9)]
    public void MotherOfTheChurch_OnPentecostMonday(int y, int m, int d)
    {
        Assert.Contains("Mother of the Church", Name(Us, y, m, d));
        Assert.Contains("Mother of the Church", Name(General, y, m, d));
    }

    [Fact]
    public void ImmaculateHeart_WithoutClash_IsMemorial()
    {
        var day = General.ForDate(new DateOnly(2024, 6, 8));
        Assert.Contains("Immaculate Heart", day.Celebration?.Name ?? string.Empty);
        Assert.Equal(CelebrationRank.Memorial, day.Celebration?.Rank);
    }

    [Theory]
    [InlineData(2026, 6, 13, "Anthony")]
    [InlineData(2025, 6, 28, "Irenaeus")]
    public void ImmaculateHeart_ClashingWithMemorial_BothBecomeOptional(int y, int m, int d, string saint)
    {
        foreach (var calendar in new[] { Us, General })
        {
            Assert.NotEqual(CelebrationRank.Memorial, calendar.ForDate(new DateOnly(y, m, d)).Celebration?.Rank);
            var optional = Optional(calendar, y, m, d).ToList();
            Assert.Contains(optional, n => n.Contains("Immaculate Heart"));
            Assert.Contains(optional, n => n.Contains(saint));
        }
    }

    [Fact]
    public void Thanksgiving_OnlyInUnitedStates()
    {
        Assert.Contains(Optional(Us, 2025, 11, 27).Append(Name(Us, 2025, 11, 27)), n => n.Contains("Thanksgiving"));
        Assert.DoesNotContain(Optional(General, 2025, 11, 27).Append(Name(General, 2025, 11, 27)), n => n.Contains("Thanksgiving"));
    }

    [Fact]
    public void January22And23_FollowRegion()
    {
        Assert.Contains("Prayer", Name(Us, 2026, 1, 22));
        Assert.Contains(Optional(General, 2026, 1, 22).Append(Name(General, 2026, 1, 22)), n => n.Contains("Vincent"));
        var us23 = Optional(Us, 2026, 1, 23).Append(Name(Us, 2026, 1, 23)).ToList();
        Assert.Contains(us23, n => n.Contains("Vincent"));
        Assert.Contains(us23, n => n.Contains("Marianne Cope"));
    }

    [Fact]
    public void Camillus_DateFollowsRegion()
    {
        Assert.Contains(Optional(Us, 2026, 7, 18).Append(Name(Us, 2026, 7, 18)), n => n.Contains("Camillus"));
        Assert.Contains(Optional(General, 2026, 7, 14).Append(Name(General, 2026, 7, 14)), n => n.Contains("Camillus"));
    }
}
