using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class HolyWeekMorningTests
{
    [Theory]
    [InlineData("general", 2027, 3, 25)]
    [InlineData("us", 2027, 3, 25)]
    [InlineData("us-thursday", 2027, 3, 25)]
    [InlineData("general", 2028, 4, 13)]
    [InlineData("us", 2028, 4, 13)]
    [InlineData("us-thursday", 2028, 4, 13)]
    public void Holy_Thursday_Lauds_keeps_the_Holy_Week_psalter_and_Lenten_keys(string profile, int year, int month, int day)
    {
        var provider = new NoTextProvider();
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, provider);
        var date = new DateOnly(year, month, day);
        var office = hours.Compose(date, Hour.MorningPrayer);

        Assert.Equal(date, office.Day.Date);
        Assert.Equal(LiturgicalSeason.Triduum, office.Day.Season);
        Assert.Equal(2, office.PsalterWeek);
        Assert.False(office.IsFirstVespers);
        Assert.Equal(new[] { "Psalm 80", "Isaiah 12:1-6", "Psalm 81" }, Psalmody(office));
        Assert.Contains("lent/w2/thursday/morning/hymn", provider.RequestedKeys);
        Assert.Contains("proper/holy-thursday/morning/psalm-antiphon-1", provider.RequestedKeys);
        Assert.DoesNotContain(provider.RequestedKeys, key => key.StartsWith("triduum/", StringComparison.Ordinal));
        Assert.DoesNotContain(provider.RequestedKeys, key => key.StartsWith("lent/", StringComparison.Ordinal)
            && key.Contains("/psalm-antiphon-", StringComparison.Ordinal));
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        Assert.True(reading.UseOfficialText);
        Assert.Null(reading.Reference);

        provider.RequestedKeys.Clear();
        var evening = hours.Compose(date, Hour.EveningPrayer);
        Assert.False(evening.IsFirstVespers);
        Assert.Equal(EveningPrayerPhase.TriduumProper, calendar.ForEvening(date).Phase);
        Assert.Equal(new[] { "Psalm 72:1-11", "Psalm 72:12-19", "Revelation 11:17-18; 12:10b-12a" }, Psalmody(evening));
        Assert.Contains("triduum/w2/thursday/evening/hymn", provider.RequestedKeys);
        Assert.DoesNotContain(provider.RequestedKeys, key => key.StartsWith("lent/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("general", 2027, 3, 28)]
    [InlineData("us", 2027, 3, 28)]
    [InlineData("us-thursday", 2027, 3, 28)]
    [InlineData("general", 2028, 4, 16)]
    [InlineData("us", 2028, 4, 16)]
    [InlineData("us-thursday", 2028, 4, 16)]
    public void Easter_Lauds_uses_Easter_keys_without_losing_its_proper_psalmody(string profile, int year, int month, int day)
    {
        var provider = new NoTextProvider();
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), provider);
        var office = hours.Compose(new(year, month, day), Hour.MorningPrayer);

        Assert.Equal(1, office.PsalterWeek);
        Assert.Equal(new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" }, Psalmody(office));
        Assert.Contains("easter/w1/sunday/morning/hymn", provider.RequestedKeys);
        Assert.DoesNotContain(provider.RequestedKeys, key => key.StartsWith("triduum/", StringComparison.Ordinal));
        Assert.Contains("proper/easter-sunday-of-the-resurrection-of-the-lord/morning/psalm-antiphon-1", provider.RequestedKeys);
    }

    [Fact]
    public void Transferred_Annunciation_still_uses_festal_Lauds()
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), new NoTextProvider());
        var office = hours.Compose(new(2027, 4, 5), Hour.MorningPrayer);

        Assert.Equal(CelebrationRank.Solemnity, office.Day.Celebration!.Rank);
        Assert.Equal(new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" }, Psalmody(office));
    }

    private static IEnumerable<string> Psalmody(Office office) => office.Steps
        .Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
        .Select(step => step.Reference!.Citation);

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public List<string> RequestedKeys { get; } = [];

        public LicensedText? Find(IEnumerable<string> keys)
        {
            RequestedKeys.AddRange(keys);
            return null;
        }
    }
}
