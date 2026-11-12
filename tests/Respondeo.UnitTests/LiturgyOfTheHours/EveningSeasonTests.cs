using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class EveningSeasonTests
{
    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void First_Advent_Vespers_does_not_borrow_an_Ordinary_Time_reading(string profile)
    {
        var hours = Hours(profile);
        var date = new DateOnly(2026, 11, 28);
        var morning = hours.Compose(date, Hour.MorningPrayer);
        var evening = hours.Compose(date, Hour.EveningPrayer);
        var reading = Assert.Single(evening.Steps, step => step.Kind == OfficeStepKind.Reading);

        Assert.Equal(LiturgicalSeason.OrdinaryTime, morning.Day.Season);
        Assert.NotNull(Assert.Single(morning.Steps, step => step.Kind == OfficeStepKind.Reading).Reference);
        Assert.True(evening.IsFirstVespers);
        Assert.Equal(new DateOnly(2026, 11, 29), evening.Day.Date);
        Assert.Null(reading.Reference);
        Assert.Null(reading.Text);
        Assert.True(reading.UseOfficialText);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Compline_on_Easter_Sunday_uses_the_mandatory_Easter_Marian_antiphon(string profile)
    {
        var office = Hours(profile).Compose(new(2027, 3, 28), Hour.NightPrayer);
        var antiphon = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.MarianAntiphon);

        Assert.Equal(new DateOnly(2027, 3, 28), office.Day.Date);
        Assert.False(office.IsFirstVespers);
        Assert.Equal("Regina Caeli", antiphon.Title);
        Assert.Equal(OfficeData.Text("regina-caeli"), antiphon.Text);
        Assert.Contains("must", antiphon.Explanation);
        Assert.DoesNotContain("Any of", antiphon.Explanation);
    }

    [Theory]
    [InlineData(2026, 11, 28, "Alma Redemptoris Mater")]
    [InlineData(2026, 12, 24, "Alma Redemptoris Mater")]
    [InlineData(2027, 3, 27, "Salve Regina")]
    [InlineData(2027, 5, 16, "Regina Caeli")]
    [InlineData(2027, 5, 17, "Salve Regina")]
    public void Compline_season_selection_does_not_roll_over_every_evening(int year, int month, int day, string title)
    {
        var antiphon = Assert.Single(Hours("general").Compose(new(year, month, day), Hour.NightPrayer).Steps,
            step => step.Kind == OfficeStepKind.MarianAntiphon);

        Assert.Equal(title, antiphon.Options is { Count: > 0 } options ? options[0].Title : antiphon.Title);
        Assert.Equal(title == "Regina Caeli", antiphon.Explanation!.Contains("must", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Marian_choices_use_existing_texts_and_are_fixed_throughout_Easter(string profile)
    {
        var hours = Hours(profile);
        foreach (var date in new[] { new DateOnly(2026, 11, 11), new(2026, 12, 15), new(2027, 1, 4), new(2027, 2, 15), new(2027, 5, 17) })
        {
            var office = hours.Compose(date, Hour.NightPrayer);
            var antiphon = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.MarianAntiphon);
            var preferred = OfficeData.Texts.MarianAntiphons.First(rule => rule.Matches(date, office.Day.Season));
            Assert.Equal("Marian Antiphon", antiphon.Title);
            Assert.Null(antiphon.Text);
            Assert.Equal(preferred.Title, antiphon.Options![0].Title);
            Assert.Equal(4, antiphon.Options.Count);
            Assert.Equal(4, antiphon.Options.Select(option => option.Title).Distinct().Count());
            Assert.All(antiphon.Options, option =>
            {
                var rule = OfficeData.Texts.MarianAntiphons.First(rule => rule.Title == option.Title);
                Assert.Equal(OfficeStepKind.MarianAntiphon, option.Kind);
                Assert.Equal(OfficeData.Text(rule.Text), option.Text);
                Assert.False(option.UseOfficialText);
                Assert.False(option.IsLicensedText);
            });
        }
        for (var date = new DateOnly(2027, 3, 28); date <= new DateOnly(2027, 5, 16); date = date.AddDays(1))
        {
            var antiphon = Assert.Single(hours.Compose(date, Hour.NightPrayer).Steps, step => step.Kind == OfficeStepKind.MarianAntiphon);
            Assert.Equal("Regina Caeli", antiphon.Title);
            Assert.Equal(OfficeData.Text("regina-caeli"), antiphon.Text);
            Assert.Null(antiphon.Options);
        }
        Assert.DoesNotContain(hours.Compose(new(2026, 11, 11), Hour.MorningPrayer).Steps, step => step.Kind == OfficeStepKind.MarianAntiphon);
        Assert.DoesNotContain(hours.Compose(new(2026, 11, 11), Hour.EveningPrayer).Steps, step => step.Kind == OfficeStepKind.MarianAntiphon);
        Assert.Empty(hours.Compose(new(2027, 3, 27), Hour.NightPrayer, LiturgicalParticipation.EasterVigil).Steps);
    }

    [Fact]
    public void Easter_Sunday_text_lookup_uses_the_same_Easter_season_as_the_template()
    {
        var provider = new NoTextProvider();
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);

        hours.Compose(new(2027, 3, 28), Hour.NightPrayer);

        Assert.Contains(provider.RequestedKeys, key => key.StartsWith("easter/", StringComparison.Ordinal)
            && key.EndsWith("/night/hymn", StringComparison.Ordinal));
        Assert.DoesNotContain(provider.RequestedKeys, key => key.StartsWith("triduum/", StringComparison.Ordinal));
    }

    private static OrdinaryFormOffice Hours(string profile) =>
        new(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new NoTextProvider());

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
