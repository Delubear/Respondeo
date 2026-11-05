using Microsoft.Extensions.DependencyInjection;
using Respondeo.Bible;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class SanctoralTests
{
    private static readonly Hour[] AllHours = Enum.GetValues<Hour>();

    private static IEnumerable<DateOnly> Days(int fromYear, int toYear)
    {
        for (var date = new DateOnly(fromYear, 1, 1); date.Year <= toYear; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static IServiceProvider Services() =>
        new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours().BuildServiceProvider().CreateScope().ServiceProvider;

    private static IEnumerable<string> AllCitations()
    {
        var data = OfficeData.Sanctoral;
        foreach (var readings in data.Commons.Values.Concat(data.Celebrations.Values))
        {
            if (readings.Morning is not null)
            {
                yield return readings.Morning;
            }

            if (readings.Evening is not null)
            {
                yield return readings.Evening;
            }
        }
    }

    // Data integrity

    [Fact]
    public void Data_Loads_WithCommonsAndCelebrations()
    {
        Assert.NotEmpty(OfficeData.Sanctoral.Commons);
        Assert.NotEmpty(OfficeData.Sanctoral.Celebrations);
    }

    [Fact]
    public void Commons_EachHaveMorningAndEvening()
    {
        Assert.All(OfficeData.Sanctoral.Commons, pair =>
        {
            Assert.False(string.IsNullOrWhiteSpace(pair.Value.Morning), $"{pair.Key} has no morning reading.");
            Assert.False(string.IsNullOrWhiteSpace(pair.Value.Evening), $"{pair.Key} has no evening reading.");
            Assert.Null(pair.Value.Common);
        });
    }

    [Fact]
    public void Celebrations_ReferenceExistingCommons()
    {
        var commons = OfficeData.Sanctoral.Commons;
        Assert.All(OfficeData.Sanctoral.Celebrations.Where(c => c.Value.Common is not null), pair =>
            Assert.True(commons.ContainsKey(pair.Value.Common!), $"{pair.Key} references unknown common '{pair.Value.Common}'."));
    }

    // Within the Christmas octave, Evening Prayer is of Christmas, so these celebrations have a Morning reading only.
    private static readonly HashSet<string> MorningOnly = ["saint-stephen-the-first-martyr", "the-holy-innocents-martyrs"];

    [Fact]
    public void Celebrations_ResolveBothMorningAndEvening()
    {
        Assert.All(OfficeData.Sanctoral.Celebrations.Keys, id =>
        {
            Assert.True(Sanctoral.ShortReading(Hour.MorningPrayer, id) is not null, $"{id} has no morning reading.");
            if (!MorningOnly.Contains(id))
            {
                Assert.True(Sanctoral.ShortReading(Hour.EveningPrayer, id) is not null, $"{id} has no evening reading.");
            }
        });
    }

    [Fact]
    public void Commons_AreAllUsed()
    {
        var used = OfficeData.Sanctoral.Celebrations.Values.Select(c => c.Common).OfType<string>().ToHashSet();
        Assert.All(OfficeData.Sanctoral.Commons.Keys, key => Assert.Contains(key, used));
    }

    [Fact]
    public void Celebrations_KeysAreCanonicalIds()
    {
        Assert.All(OfficeData.Sanctoral.Celebrations.Keys, id => Assert.Equal(id, OrdinaryFormOffice.CelebrationId(id)));
    }

    [Fact]
    public void Celebrations_KeysMatchCalendarCelebrations()
    {
        var ids = new HashSet<string>();
        foreach (var calendar in new[] { new RomanCalendar(), new RomanCalendar(new LiturgicalCalendarOptions(CalendarRegion.UnitedStates)) })
        foreach (var date in Days(2025, 2028))
        {
            var day = calendar.ForDate(date);
            if (day.Celebration is { } celebration)
            {
                ids.Add(OrdinaryFormOffice.CelebrationId(celebration.Name));
            }

            foreach (var memorial in day.OptionalMemorials)
            {
                ids.Add(OrdinaryFormOffice.CelebrationId(memorial.Name));
            }
        }

        var orphans = OfficeData.Sanctoral.Celebrations.Keys.Where(id => !ids.Contains(id)).ToList();
        Assert.True(orphans.Count == 0, "Keys not matching any calendar celebration: " + string.Join(", ", orphans));
    }

    [Fact]
    public void Citations_AllParse()
    {
        var failures = AllCitations().Distinct().Where(c => BibleCitation.Parse(c, TestBible.Drb) is null).ToList();
        Assert.True(failures.Count == 0, "Unparseable citations: " + string.Join("; ", failures));
    }

    // Behavior

    [Theory]
    [InlineData("Saint Agnes, Virgin and Martyr", Hour.MorningPrayer, "2 Corinthians 1:3-5")]
    [InlineData("Saint Agnes, Virgin and Martyr", Hour.EveningPrayer, "1 Peter 4:13-14")]
    public void ShortReading_FallsBackToCommon(string name, Hour hour, string expected) =>
        Assert.Equal(expected, Sanctoral.ShortReading(hour, name));

    [Theory]
    [InlineData(Hour.NightPrayer)]
    public void ShortReading_OtherHours_AreNull(Hour hour) =>
        Assert.All(OfficeData.Sanctoral.Celebrations.Keys, id => Assert.Null(Sanctoral.ShortReading(hour, id)));

    [Fact]
    public void ShortReading_UnknownCelebration_IsNull()
    {
        Assert.Null(Sanctoral.ShortReading(Hour.MorningPrayer, "Saint Nobody, Fictional"));
        Assert.Null(Sanctoral.ShortReading(Hour.EveningPrayer, "Saint Nobody, Fictional"));
    }

    [Fact]
    public void ShortReading_OwnReadingWinsOverCommon()
    {
        foreach (var (id, readings) in OfficeData.Sanctoral.Celebrations)
        {
            if (readings.Morning is not null)
            {
                Assert.Equal(readings.Morning, Sanctoral.ShortReading(Hour.MorningPrayer, id));
            }

            if (readings.Evening is not null)
            {
                Assert.Equal(readings.Evening, Sanctoral.ShortReading(Hour.EveningPrayer, id));
            }
        }
    }

    // Composed-office contracts

    [Fact]
    public void Compose_EveryDayAndHour_ProducesSteps()
    {
        var hours = Services().GetRequiredService<ILiturgyOfTheHours>();
        foreach (var date in Days(2026, 2026))
        {
            foreach (var hour in AllHours)
            {
                var office = hours.Compose(date, hour);
                Assert.True(office.Steps.Count > 0, $"{date} {hour} has no steps.");
            }
        }
    }

    [Fact]
    public void Compose_MorningAndEvening_HaveExactlyOneReading()
    {
        var hours = Services().GetRequiredService<ILiturgyOfTheHours>();
        foreach (var date in Days(2026, 2026))
        {
            foreach (var hour in new[] { Hour.MorningPrayer, Hour.EveningPrayer })
            {
                var count = hours.Compose(date, hour).Steps.Count(s => s.Kind == OfficeStepKind.Reading);
                Assert.True(count == 1, $"{date} {hour} has {count} reading steps.");
            }
        }
    }

    [Fact]
    public void Compose_SanctoralDays_ShowSanctoralReading()
    {
        var services = Services();
        var calendar = services.GetRequiredService<ILiturgicalCalendar>();
        var hours = services.GetRequiredService<ILiturgyOfTheHours>();
        var checkedDays = 0;
        foreach (var date in Days(2026, 2026))
        {
            if (calendar.ForDate(date).Celebration is not { } celebration
                || !OfficeData.Sanctoral.Celebrations.ContainsKey(OrdinaryFormOffice.CelebrationId(celebration.Name)))
            {
                continue;
            }

            foreach (var hour in new[] { Hour.MorningPrayer, Hour.EveningPrayer })
            {
                // Evening Prayer before a Sunday or solemnity is its First Vespers, which supersedes the day's celebration.
                var office = hours.Compose(date, hour);
                if (office.IsFirstVespers || Sanctoral.ShortReading(hour, celebration.Name) is not { } expected)
                {
                    continue;
                }

                var reading = office.Steps.Single(s => s.Kind == OfficeStepKind.Reading);
                Assert.True(expected == reading.Reference?.Citation, $"{date} {hour} ({celebration.Name}): expected {expected}, got {reading.Reference?.Citation}.");
                Assert.False(reading.UseOfficialText, $"{date} {hour} should not defer to the official text.");
            }

            checkedDays++;
        }

        Assert.True(checkedDays > 0);
    }

    [Fact]
    public void Compose_AllReadingCitations_Parse()
    {
        var hours = Services().GetRequiredService<ILiturgyOfTheHours>();
        var failures = new HashSet<string>();
        foreach (var date in Days(2026, 2026))
        {
            foreach (var hour in AllHours)
            {
                foreach (var step in hours.Compose(date, hour).Steps)
                {
                    if (step.Reference?.Citation is { } citation && BibleCitation.Parse(citation, TestBible.Drb) is null)
                    {
                        failures.Add($"{date} {hour}: {citation}");
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, "Unparseable citations: " + string.Join("; ", failures.Take(20)));
    }
}
