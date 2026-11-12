using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class LentenSundayPsalmodyTests
{
    [Theory]
    [InlineData("general", 2027, 3, 28)]
    [InlineData("us", 2027, 3, 28)]
    [InlineData("us-thursday", 2027, 3, 28)]
    [InlineData("general", 2028, 4, 16)]
    [InlineData("us", 2028, 4, 16)]
    [InlineData("us-thursday", 2028, 4, 16)]
    public void All_six_Lenten_Sundays_use_Peter_at_second_Vespers_and_keep_their_cycle_at_first_Vespers(
        string profile, int year, int month, int day)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, new NoTextProvider());
        var easter = new DateOnly(year, month, day);
        var secondPsalms = new[] { "Psalm 114", "Psalm 115", "Psalm 111", "Psalm 112", "Psalm 114", "Psalm 115" };
        var firstPsalms = new[]
        {
            new[] { "Psalm 141:1-9", "Psalm 142", "Philippians 2:6-11" },
            new[] { "Psalm 119:105-112", "Psalm 16", "Philippians 2:6-11" },
            new[] { "Psalm 113", "Psalm 116:10-19", "Philippians 2:6-11" },
            new[] { "Psalm 122", "Psalm 130", "Philippians 2:6-11" },
        };
        for (var index = 0; index < 6; index++)
        {
            var sunday = easter.AddDays(-42 + index * 7);
            var week = index % 4 + 1;
            var second = hours.Compose(sunday, Hour.EveningPrayer);
            var first = hours.Compose(sunday.AddDays(-1), Hour.EveningPrayer);

            Assert.Equal(new[] { "Psalm 110:1-5, 7", secondPsalms[index], "1 Peter 2:21-24" }, Psalmody(second));
            Assert.False(second.IsFirstVespers);
            Assert.Equal(week, second.PsalterWeek);
            Assert.Equal(sunday, second.Day.Date);
            Assert.Equal(firstPsalms[week - 1], Psalmody(first));
            Assert.True(first.IsFirstVespers);
            Assert.Equal(week, first.PsalterWeek);
            Assert.Equal(sunday, first.Day.Date);
        }

        foreach (var date in new[] { easter.AddDays(-49), easter, easter.AddDays(14) })
        {
            Assert.Equal("Revelation 19:1-7", Psalmody(hours.Compose(date, Hour.EveningPrayer)).Last());
        }

        Assert.Equal("Revelation 11:17-18; 12:10b-12a", Psalmody(hours.Compose(easter.AddDays(-3), Hour.EveningPrayer)).Last());
    }

    [Theory]
    [InlineData(2, 14, "lent/w1/sunday/evening/psalm-antiphon-3")]
    [InlineData(3, 21, "proper/palm-sunday-of-the-passion-of-the-lord/evening/psalm-antiphon-3")]
    public void Lenten_canticle_keeps_its_seasonal_or_Palm_Sunday_antiphon_binding(int month, int day, string key)
    {
        var provider = new NoTextProvider { TextKey = key };
        var office = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider)
            .Compose(new DateOnly(2027, month, day), Hour.EveningPrayer);
        var canticle = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Canticle);

        Assert.Equal("1 Peter 2:21-24", canticle.Reference!.Citation);
        Assert.Equal("Synthetic bound canticle", canticle.Text);
        Assert.Equal("Synthetic antiphon", canticle.Antiphon);
        Assert.True(canticle.IsLicensedText);
        Assert.False(canticle.UseOfficialText);
    }

    private static IEnumerable<string> Psalmody(Office office) => office.Steps
        .Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
        .Select(step => step.Reference!.Citation);

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public string? TextKey { get; init; }

        public LicensedText? Find(IEnumerable<string> keys) => TextKey is not null && keys.Contains(TextKey)
            ? new("Synthetic bound canticle", "Synthetic antiphon", "1 Peter 2:21-24")
            : null;
    }
}
