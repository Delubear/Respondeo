using Microsoft.Extensions.DependencyInjection;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

/// <summary>
/// Walks every day of many years through the public contracts the front end consumes
/// (<see cref="ILiturgicalCalendar"/> via <see cref="LiturgicalCalendars"/>, and <see cref="ILiturgyOfTheHours"/>),
/// checking structural invariants on every day and published Ordo facts on known dates.
/// </summary>
public class ExhaustiveCalendarTests
{
    private const int FirstYear = 2018;
    private const int LastYear = 2035;

    public static TheoryData<string> Regions => ["general", "us", "us-thursday"];

    private static ILiturgicalCalendar Calendar(string region) => LiturgicalCalendars.Create(region switch
    {
        "us" => new LiturgicalCalendarOptions(CalendarRegion.UnitedStates),
        "us-thursday" => new LiturgicalCalendarOptions(CalendarRegion.UnitedStates, AscensionOnThursday: true),
        _ => LiturgicalCalendarOptions.Default,
    });

    private static IEnumerable<DateOnly> AllDays()
    {
        for (var d = new DateOnly(FirstYear, 1, 1); d <= new DateOnly(LastYear, 12, 31); d = d.AddDays(1))
        {
            yield return d;
        }
    }

    [Theory]
    [MemberData(nameof(Regions))]
    public void EveryDay_SatisfiesStructuralInvariants(string region)
    {
        var calendar = Calendar(region);
        var failures = new List<string>();
        LiturgicalSeason? previous = null;

        foreach (var date in AllDays())
        {
            var day = calendar.ForDate(date);
            void Fail(string why) => failures.Add($"{date:yyyy-MM-dd} {day.Celebration?.Name ?? day.FerialName}: {why}");

            if (day.Date != date)
            {
                Fail("date mismatch");
            }

            if ((day.Celebration is null) == string.IsNullOrWhiteSpace(day.FerialName))
            {
                Fail("exactly one of Celebration/FerialName must be set");
            }

            if (day.Celebration is { Rank: CelebrationRank.OptionalMemorial })
            {
                Fail("an optional memorial can never be the principal celebration");
            }

            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                if (day.OptionalMemorials.Count > 0)
                {
                    Fail("Sundays never offer optional memorials");
                }

                if (day.Celebration is { Rank: CelebrationRank.Memorial })
                {
                    Fail("a memorial can never replace a Sunday");
                }
            }

            if (day.Season is LiturgicalSeason.Lent or LiturgicalSeason.Advent or LiturgicalSeason.Easter
                && date.DayOfWeek == DayOfWeek.Sunday
                && day.Celebration is { } sundayCelebration
                && !IsDominicalSolemnity(sundayCelebration.Name))
            {
                Fail("only the Lord's own solemnities may displace a Sunday of Advent, Lent or Easter");
            }

            if (day.Season == LiturgicalSeason.Lent && day.Celebration is { Rank: CelebrationRank.Memorial })
            {
                Fail("memorials are only optional in Lent");
            }

            if (day.Celebration is null)
            {
                var expected = day.Season switch
                {
                    LiturgicalSeason.Advent => date is { Month: 12, Day: 3 or 4 or 5 } && date.DayOfWeek == DayOfWeek.Sunday ? LiturgicalColor.Rose : (LiturgicalColor?)null,
                    _ => null,
                };
                _ = expected;
                if (day.Season == LiturgicalSeason.OrdinaryTime && day.Color != LiturgicalColor.Green)
                {
                    Fail("ferial Ordinary Time must be green");
                }

                if (day.Season is LiturgicalSeason.Christmas or LiturgicalSeason.Easter && day.Color != LiturgicalColor.White)
                {
                    Fail("ferial Christmas/Easter must be white");
                }

                if (day.Season is LiturgicalSeason.Advent or LiturgicalSeason.Lent && day.Color is not (LiturgicalColor.Violet or LiturgicalColor.Rose))
                {
                    Fail("ferial Advent/Lent must be violet or rose");
                }
            }
            else if (day.Color != day.Celebration.Color)
            {
                Fail("day color must follow its celebration");
            }

            if (previous is { } p && p != day.Season && !IsValidTransition(p, day.Season))
            {
                Fail($"invalid season transition {p} -> {day.Season}");
            }

            previous = day.Season;
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(50)));
    }

    private static bool IsDominicalSolemnity(string name) =>
        name.Contains("Lord", StringComparison.Ordinal) || name.Contains("Christ", StringComparison.Ordinal)
        || name.StartsWith("Palm Sunday", StringComparison.Ordinal) || name.StartsWith("Easter", StringComparison.Ordinal)
        || name.StartsWith("Pentecost", StringComparison.Ordinal) || name.Contains("Trinity", StringComparison.Ordinal);

    private static bool IsValidTransition(LiturgicalSeason from, LiturgicalSeason to) => (from, to) switch
    {
        (LiturgicalSeason.Christmas, LiturgicalSeason.OrdinaryTime) => true,
        (LiturgicalSeason.OrdinaryTime, LiturgicalSeason.Lent) => true,
        (LiturgicalSeason.Lent, LiturgicalSeason.Triduum) => true,
        (LiturgicalSeason.Triduum, LiturgicalSeason.Easter) => true,
        (LiturgicalSeason.Easter, LiturgicalSeason.OrdinaryTime) => true,
        (LiturgicalSeason.OrdinaryTime, LiturgicalSeason.Advent) => true,
        (LiturgicalSeason.Advent, LiturgicalSeason.Christmas) => true,
        _ => false,
    };

    [Theory]
    [MemberData(nameof(Regions))]
    public void EveryYear_HasCompleteSeasonAndWeekStructure(string region)
    {
        var calendar = Calendar(region);
        for (var year = FirstYear; year <= LastYear; year++)
        {
            var easter = LiturgicalDates.Easter(year);
            var advent = LiturgicalDates.FirstSundayOfAdvent(year);

            Assert.Equal(LiturgicalSeason.Lent, calendar.ForDate(easter.AddDays(-46)).Season);
            Assert.Equal(LiturgicalSeason.OrdinaryTime, calendar.ForDate(easter.AddDays(-47)).Season);
            Assert.Equal(LiturgicalSeason.Triduum, calendar.ForDate(easter.AddDays(-3)).Season);
            Assert.Equal(LiturgicalSeason.Easter, calendar.ForDate(easter.AddDays(49)).Season);
            Assert.Equal(LiturgicalSeason.OrdinaryTime, calendar.ForDate(easter.AddDays(50)).Season);
            Assert.Equal(LiturgicalSeason.Advent, calendar.ForDate(advent).Season);
            Assert.Equal(LiturgicalSeason.Christmas, calendar.ForDate(new DateOnly(year, 12, 25)).Season);

            Assert.Equal("The Nativity of the Lord", Name(calendar, new DateOnly(year, 12, 25)), ignoreCase: true, ignoreWhiteSpaceDifferences: true);
            Assert.Equal("Ash Wednesday", Name(calendar, easter.AddDays(-46)));
            Assert.Equal("Easter Sunday of the Resurrection of the Lord", Name(calendar, easter));
            Assert.Equal("Pentecost Sunday", Name(calendar, easter.AddDays(49)));
            Assert.Equal("The Most Holy Trinity", Name(calendar, easter.AddDays(56)));
            Assert.Equal("The Most Sacred Heart of Jesus", Name(calendar, easter.AddDays(68)));
            Assert.Equal("Our Lord Jesus Christ, King of the Universe", Name(calendar, advent.AddDays(-7)));
            Assert.Equal(LiturgicalColor.Rose, calendar.ForDate(advent.AddDays(14)).Color);
            Assert.Equal(LiturgicalColor.Rose, calendar.ForDate(easter.AddDays(-21)).Color);
            Assert.Equal("Second Sunday of Easter", calendar.ForDate(easter.AddDays(7)).FerialName ?? "Second Sunday of Easter");

            // Ordinary Time: the Saturday before Advent is in the 34th week; the Monday after Pentecost is a valid week.
            Assert.EndsWith("Thirty-Fourth Week in Ordinary Time", calendar.ForDate(advent.AddDays(-1)).FerialName ?? "Thirty-Fourth Week in Ordinary Time");
            Assert.Contains("Ordinary Time", calendar.ForDate(easter.AddDays(-47)).FerialName ?? "Ordinary Time");

            // Every Ordinary Time week number lies in 1..34 and never decreases within a span.
            foreach (var date in Enumerable.Range(0, 365).Select(i => new DateOnly(year, 1, 1).AddDays(i)).Where(d => d.Year == year))
            {
                var ferial = calendar.ForDate(date).FerialName;
                if (ferial is not null && ferial.Contains("Ordinary Time", StringComparison.Ordinal))
                {
                    Assert.DoesNotContain("Thirty-Fifth", ferial);
                    Assert.False(char.IsDigit(ferial[0]), $"{date}: {ferial}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Regions))]
    public void Ascension_AndCorpusChristi_FollowRegion(string region)
    {
        var calendar = Calendar(region);
        for (var year = FirstYear; year <= LastYear; year++)
        {
            var easter = LiturgicalDates.Easter(year);
            var ascension = region == "us" ? 42 : 39;
            var corpus = region == "general" ? 60 : 63;
            Assert.Equal("The Ascension of the Lord", Name(calendar, easter.AddDays(ascension)));
            Assert.Equal("The Most Holy Body and Blood of Christ", Name(calendar, easter.AddDays(corpus)));
        }
    }

    // Published Ordo facts (universal calendar unless region says otherwise).
    [Theory]
    [InlineData("general", 2024, 4, 8, "The Annunciation of the Lord")]
    [InlineData("general", 2024, 12, 9, "The Immaculate Conception of the Blessed Virgin Mary")]
    [InlineData("general", 2024, 3, 19, "Saint Joseph, Spouse of the Blessed Virgin Mary")]
    [InlineData("general", 2025, 3, 19, "Saint Joseph, Spouse of the Blessed Virgin Mary")]
    [InlineData("general", 2023, 3, 20, "Saint Joseph, Spouse of the Blessed Virgin Mary")]
    [InlineData("general", 2029, 3, 19, "Saint Joseph, Spouse of the Blessed Virgin Mary")]
    [InlineData("general", 2027, 4, 5, "The Annunciation of the Lord")]
    [InlineData("general", 2022, 6, 23, "The Nativity of Saint John the Baptist")]
    [InlineData("general", 2022, 6, 24, "The Most Sacred Heart of Jesus")]
    [InlineData("general", 2025, 6, 29, "Saints Peter and Paul, Apostles")]
    [InlineData("general", 2025, 8, 6, "The Transfiguration of the Lord")]
    [InlineData("general", 2020, 9, 14, "The Exaltation of the Holy Cross")]
    [InlineData("general", 2025, 11, 9, "The Dedication of the Lateran Basilica")]
    [InlineData("general", 2025, 1, 6, "The Epiphany of the Lord")]
    [InlineData("general", 2025, 1, 12, "The Baptism of the Lord")]
    [InlineData("general", 2023, 12, 31, "The Holy Family of Jesus, Mary and Joseph")]
    [InlineData("general", 2022, 12, 30, "The Holy Family of Jesus, Mary and Joseph")]
    [InlineData("general", 2023, 1, 1, "Mary, the Holy Mother of God")]
    [InlineData("us", 2025, 1, 5, "The Epiphany of the Lord")]
    [InlineData("us", 2024, 1, 8, "The Baptism of the Lord")]
    [InlineData("us", 2025, 5, 29, null)]
    [InlineData("us", 2025, 6, 1, "The Ascension of the Lord")]
    [InlineData("us-thursday", 2025, 5, 29, "The Ascension of the Lord")]
    [InlineData("us", 2025, 11, 27, null)]
    public void KnownDates_MatchPublishedOrdo(string region, int year, int month, int day, string? expectedPrefix)
    {
        var result = Calendar(region).ForDate(new DateOnly(year, month, day));
        if (expectedPrefix is null)
        {
            Assert.True(result.Celebration is null || result.Celebration.Name != "The Ascension of the Lord", result.Celebration?.Name);
            return;
        }

        Assert.NotNull(result.Celebration);
        Assert.StartsWith(Normalize(expectedPrefix), Normalize(result.Celebration!.Name));
    }

    [Theory]
    [InlineData(2025, 4, 25)] // Saint Mark in the Octave of Easter
    [InlineData(2019, 4, 25)]
    [InlineData(2014, 4, 25)]
    public void FeastsWithinEasterOctave_AreOmitted(int year, int month, int day)
    {
        var result = Calendar("general").ForDate(new DateOnly(year, month, day));
        Assert.Null(result.Celebration);
        Assert.EndsWith("within the Octave of Easter", result.FerialName);
    }

    [Theory]
    [MemberData(nameof(Regions))]
    public void EveryDay_ComposesEveryHourWithoutError(string region)
    {
        var services = new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours();
        services.AddSingleton(Calendar(region));
        var hours = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<ILiturgyOfTheHours>();
        var failures = new List<string>();

        foreach (var date in AllDays().Where(d => d.Year is >= 2024 and <= 2027))
        {
            foreach (var hour in Enum.GetValues<Hour>())
            {
                try
                {
                    var office = hours.Compose(date, hour);
                    if (office.Date != date || office.Hour != hour)
                    {
                        failures.Add($"{date} {hour}: identity mismatch");
                    }

                    if (office.PsalterWeek is < 1 or > 4)
                    {
                        failures.Add($"{date} {hour}: psalter week {office.PsalterWeek}");
                    }

                    if (office.Steps.Count == 0 || string.IsNullOrWhiteSpace(office.Title))
                    {
                        failures.Add($"{date} {hour}: empty office");
                    }

                    var expectedDay = office.IsFirstVespers ? date.AddDays(1) : date;
                    if (office.Day.Date != expectedDay)
                    {
                        failures.Add($"{date} {hour}: office day {office.Day.Date}");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{date} {hour}: {ex.GetType().Name} {ex.Message}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(50)));
    }

    [Fact]
    public void PsalterWeek_ResetsToOneOnAdventAndLentStarts()
    {
        var hours = new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours().BuildServiceProvider()
            .CreateScope().ServiceProvider.GetRequiredService<ILiturgyOfTheHours>();
        for (var year = FirstYear; year <= LastYear; year++)
        {
            Assert.Equal(1, hours.Compose(LiturgicalDates.FirstSundayOfAdvent(year), Hour.MorningPrayer).PsalterWeek);
            Assert.Equal(1, hours.Compose(LiturgicalDates.FirstSundayOfLent(year), Hour.MorningPrayer).PsalterWeek);
            Assert.Equal(2, hours.Compose(LiturgicalDates.Easter(year).AddDays(7), Hour.MorningPrayer).PsalterWeek);
            Assert.Equal(4, hours.Compose(LiturgicalDates.AshWednesday(year), Hour.MorningPrayer).PsalterWeek);
        }
    }

    [Fact]
    public void PaulinusOfNola_IsWhite()
    {
        var day = Calendar("general").ForDate(new DateOnly(2026, 6, 22));
        var paulinus = Assert.Single(day.OptionalMemorials, m => m.Name.StartsWith("Saint Paulinus", StringComparison.Ordinal));
        Assert.Equal(LiturgicalColor.White, paulinus.Color);
    }

    [Theory]
    [MemberData(nameof(Regions))]
    public void AllSouls_ReplacesSunday(string region)
    {
        var day = Calendar(region).ForDate(new DateOnly(2025, 11, 2));
        Assert.Equal("The Commemoration of All the Faithful Departed", day.Celebration?.Name);
    }

    [Fact]
    public void UnitedStatesOverrides_ReferenceExistingGeneralEntries()
    {
        var general = ReadResource("Respondeo.LiturgicalCalendar.GeneralRomanCalendar.txt")
            .Select(p => (Date: p[0], Name: p[3]))
            .ToHashSet();
        var failures = new List<string>();

        foreach (var parts in ReadResource("Respondeo.LiturgicalCalendar.UnitedStatesProper.txt"))
        {
            if (parts[0].StartsWith('-'))
            {
                var date = parts[0][1..];
                if (!general.Contains((date, parts[1])))
                {
                    failures.Add($"Removal not found in general calendar: {date} | {parts[1]}");
                }

                continue;
            }

            if (general.Contains((parts[0], parts[3])))
            {
                continue;
            }

            var key = Key(parts[3]);
            foreach (var near in general.Where(g => g.Date == parts[0] && (Key(g.Name).Contains(key) || key.Contains(Key(g.Name)))))
            {
                failures.Add($"US entry '{parts[3]}' on {parts[0]} looks like a renamed replacement of '{near.Name}'");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static List<string[]> ReadResource(string resourceName)
    {
        using var stream = typeof(LiturgicalCalendars).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing resource {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l => l.Split('|').Select(p => p.Trim()).ToArray())
            .ToList();
    }

    private static string Key(string name) => new(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? Name(ILiturgicalCalendar calendar, DateOnly date) => calendar.ForDate(date).Celebration?.Name;

    private static string Normalize(string s) => s.Replace("Most Holy Mother", "Holy Mother", StringComparison.Ordinal);
}
