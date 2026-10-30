using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

/// <summary>Validates the embedded data files that drive the Office, so a bad edit fails here rather than at runtime.</summary>
public class OfficeDataTests
{
    public static TheoryData<DayOfWeek> Days => new(Enum.GetValues<DayOfWeek>());

    [Fact]
    public void Psalter_HasFourWeeksOfMorningAndEvening()
    {
        Assert.Equal(4, OfficeData.Psalter.Morning.Count);
        Assert.Equal(4, OfficeData.Psalter.Evening.Count);
    }

    [Theory]
    [MemberData(nameof(Days))]
    public void Psalter_EveryWeekAndDay_HasThreeCitations(DayOfWeek day)
    {
        for (var week = 1; week <= 4; week++)
        {
            Assert.Equal(3, Psalter.MorningPsalmody(week, day).Count);
            Assert.Equal(3, Psalter.EveningPsalmody(week, day).Count);
        }
    }

    [Theory]
    [MemberData(nameof(Days))]
    public void Psalter_MorningHasCanticleInMiddle_EveningEndsWithCanticle(DayOfWeek day)
    {
        for (var week = 1; week <= 4; week++)
        {
            var morning = Psalter.MorningPsalmody(week, day);
            var evening = Psalter.EveningPsalmody(week, day);

            Assert.False(Psalter.IsCanticle(morning[0]));
            Assert.True(Psalter.IsCanticle(morning[1]));
            Assert.False(Psalter.IsCanticle(evening[0]));
            Assert.True(Psalter.IsCanticle(evening[2]));
        }
    }

    [Theory]
    [MemberData(nameof(Days))]
    public void Psalter_EveryNight_HasPsalmsAndReading(DayOfWeek day)
    {
        var (psalms, reading) = Psalter.NightPsalmody(day);

        Assert.NotEmpty(psalms);
        Assert.False(string.IsNullOrWhiteSpace(reading));
    }

    [Fact]
    public void Psalter_EveryPsalmCitation_Parses()
    {
        var citations = OfficeData.Psalter.Morning.Concat(OfficeData.Psalter.Evening)
            .SelectMany(week => week.Values).SelectMany(c => c)
            .Concat(OfficeData.Psalter.Night.Values.SelectMany(n => n.Psalms))
            .Where(c => !Psalter.IsCanticle(c));

        Assert.All(citations, c => Assert.Matches(@"^Psalm (\d{1,3})(:\d+[ab]?(-\d+[ab]?)?(, \d+[ab]?(-\d+[ab]?)?)*)?$", c));
    }

    [Fact]
    public void Texts_AllEmbeddedReferences_Resolve()
    {
        Assert.All(OfficeData.Texts.Texts.Keys, key => Assert.DoesNotContain("{", OfficeData.Text(key)));
        Assert.All(OfficeData.Texts.NightConcludingPrayers.Values, lines => Assert.DoesNotContain("{", OfficeData.ResolveText(lines)));
    }

    [Fact]
    public void Texts_MarianAntiphons_EndWithCatchAllAndReferenceKnownTexts()
    {
        var rules = OfficeData.Texts.MarianAntiphons;

        Assert.All(rules, r => Assert.True(OfficeData.Texts.Texts.ContainsKey(r.Text), $"Unknown text '{r.Text}'."));
        Assert.True(rules[^1] is { Seasons: null, Months: null, Dates: null });
    }

    [Theory]
    [InlineData(Hour.MorningPrayer)]
    [InlineData(Hour.EveningPrayer)]
    [InlineData(Hour.NightPrayer)]
    public void Hours_EveryHourTemplate_IsComplete(Hour hour)
    {
        var template = OfficeData.Hours.Hours[hour];

        Assert.False(string.IsNullOrWhiteSpace(template.Page));
        Assert.Single(template.Steps, s => s.Source == StepSource.Psalmody);
        Assert.All(template.Steps.Where(s => s.Source == StepSource.Fixed), s =>
        {
            Assert.NotNull(s.Text);
            Assert.DoesNotContain("{", OfficeData.ResolveText(s.Text));
            foreach (var o in s.Seasons?.Values.ToList() ?? new List<SeasonOverride>())
            {
                Assert.DoesNotContain("{", OfficeData.ResolveText(o.Text ?? Array.Empty<string>()));
            }
        });
        Assert.All(template.Steps.Where(s => s.Source != StepSource.Psalmody && s.Source != StepSource.MarianAntiphon), s =>
            Assert.False(string.IsNullOrWhiteSpace(s.Title)));
    }

    [Fact]
    public void Hours_TitlesExistForEveryHourAndFirstVespers()
    {
        foreach (var key in new[] { "MorningPrayer", "EveningPrayer", "EveningPrayerFirstVespers", "NightPrayer", "NightPrayerFirstVespers" })
        {
            Assert.Contains("{day}", OfficeData.Hours.Titles[key]);
        }
    }
}
