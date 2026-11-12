using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class EasterOctavePsalmodyTests
{
    [Theory]
    [InlineData("general", 2027, 3, 28)]
    [InlineData("us", 2027, 3, 28)]
    [InlineData("us-thursday", 2027, 3, 28)]
    [InlineData("general", 2028, 4, 16)]
    [InlineData("us", 2028, 4, 16)]
    [InlineData("us-thursday", 2028, 4, 16)]
    public void Octave_psalmody_overrides_the_background_cycle_through_the_Second_Sunday(string profile, int year, int month, int day)
    {
        var provider = new TextProvider();
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), provider);
        var easter = new DateOnly(year, month, day);
        foreach (var hour in new[] { Hour.MorningPrayer, Hour.EveningPrayer })
        {
            for (var offset = 0; offset <= 7; offset++)
            {
                provider.Requests.Clear();
                var office = hours.Compose(easter.AddDays(offset), hour);
                var selectedOffset = hour == Hour.EveningPrayer && offset == 6 ? 7 : offset;
                var hourKey = hour == Hour.MorningPrayer ? "morning" : offset == 6 ? "evening-i" : "evening";
                var id = selectedOffset == 0 ? "easter-sunday-of-the-resurrection-of-the-lord" : $"easter-octave-day-{selectedOffset + 1}";
                var psalmody = office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).ToArray();
                var expected = hour == Hour.MorningPrayer
                    ? new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" }
                    : new[] { "Psalm 110:1-5, 7", "Psalm 114", "Revelation 19:1-7" };

                Assert.Equal(expected, psalmody.Select(step => step.Reference!.Citation));
                Assert.Equal(selectedOffset == 7 ? 2 : 1, office.PsalterWeek);
                Assert.Equal(hour == Hour.EveningPrayer && offset == 6, office.IsFirstVespers);
                Assert.Equal(easter.AddDays(selectedOffset), office.Day.Date);
                Assert.Contains($"proper/{id}/{hourKey}/psalm-antiphon-1", provider.Requests);
                Assert.DoesNotContain(provider.Requests, key => key.StartsWith("easter/", StringComparison.Ordinal)
                    && key.Contains("/psalm-antiphon-", StringComparison.Ordinal));
                Assert.All(psalmody, step =>
                {
                    Assert.Null(step.Antiphon);
                    Assert.Null(step.Text);
                    Assert.True(step.UseOfficialText);
                });
            }
        }

        foreach (var hour in new[] { Hour.MorningPrayer, Hour.EveningPrayer })
        {
            provider.Requests.Clear();
            var office = hours.Compose(easter.AddDays(10), hour);
            var expected = hour == Hour.MorningPrayer ? Psalter.MorningPsalmody(2, DayOfWeek.Wednesday) : Psalter.EveningPsalmody(2, DayOfWeek.Wednesday);
            Assert.Equal(expected, office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).Select(step => step.Reference!.Citation));
            Assert.DoesNotContain(provider.Requests, key => key.StartsWith("proper/easter-octave-", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(2027, 3, 28)]
    [InlineData(2028, 4, 16)]
    public void Calendar_day_and_evening_share_the_civil_octave_ordinal(int year, int month, int day)
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var easter = new DateOnly(year, month, day);
        for (var offset = -1; offset <= 8; offset++)
        {
            int? expected = offset is >= 0 and <= 7 ? offset + 1 : null;
            var date = easter.AddDays(offset);
            Assert.Equal(expected, calendar.ForDate(date).EasterOctaveDay);
            Assert.Equal(expected, calendar.ForEvening(date).EasterOctaveDay);
        }

        var saturday = calendar.ForEvening(easter.AddDays(6));
        Assert.Equal(7, saturday.EasterOctaveDay);
        Assert.Equal(8, saturday.OfficeDay.EasterOctaveDay);
    }

    [Theory]
    [InlineData(Hour.MorningPrayer, 1, "morning")]
    [InlineData(Hour.EveningPrayer, 1, "evening")]
    [InlineData(Hour.EveningPrayer, 6, "evening-i")]
    public void Exact_octave_antiphons_bind_without_borrowing_weekday_or_generic_text(Hour hour, int offset, string hourKey)
    {
        var selectedDay = offset == 6 ? 8 : 2;
        var provider = new TextProvider(new()
        {
            [$"proper/easter-octave-day-{selectedDay}/{hourKey}/psalm-antiphon-1"] = new("", "Synthetic proper antiphon"),
            ["easter/w1/monday/morning/psalm-antiphon-2"] = new("", "Wrong weekday antiphon"),
            ["psalm-antiphon-2"] = new("", "Wrong generic antiphon"),
        });
        var office = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider)
            .Compose(new DateOnly(2027, 3, 28).AddDays(offset), hour);
        var psalmody = office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).ToArray();

        Assert.Equal("Synthetic proper antiphon", psalmody[0].Antiphon);
        Assert.True(psalmody[0].IsLicensedText);
        Assert.False(psalmody[0].UseOfficialText);
        Assert.Null(psalmody[1].Antiphon);
        Assert.True(psalmody[1].UseOfficialText);
    }

    private sealed class TextProvider(Dictionary<string, LicensedText>? texts = null) : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public List<string> Requests { get; } = [];

        public LicensedText? Find(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                Requests.Add(key);
                if (texts is not null && texts.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
