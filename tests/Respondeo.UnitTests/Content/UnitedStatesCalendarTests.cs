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
}
