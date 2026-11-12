using System.Text.Json;
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

    [Theory]
    [InlineData("Mary, the Holy Mother of God")]
    [InlineData("The Most Holy Name of Jesus")]
    [InlineData("Saint Joseph the Worker")]
    [InlineData("The Nativity of the Lord")]
    public void Celebration_HasMorningAndEveningReadings(string name)
    {
        Assert.NotNull(Sanctoral.ShortReading(Hour.MorningPrayer, name));
        Assert.NotNull(Sanctoral.ShortReading(Hour.EveningPrayer, name));
    }

    [Theory]
    [InlineData("Saint Stephen, the First Martyr", "1 John 1:1-3")]
    [InlineData("The Holy Innocents, Martyrs", "1 John 1:1-3")]
    [InlineData("Saint Martin of Tours, Bishop", "1 Peter 5:1-4")]
    public void Celebration_HasExpectedEveningReading(string name, string citation) =>
        Assert.Equal(citation, Sanctoral.ShortReading(Hour.EveningPrayer, name));

    // Data integrity

    [Fact]
    public void Data_Loads_WithCommonsAndCelebrations()
    {
        Assert.NotEmpty(OfficeData.Sanctoral.Commons);
        Assert.NotEmpty(OfficeData.Sanctoral.Celebrations);
    }

    [Fact]
    public void Data_HasNoDuplicateKeys()
    {
        using var stream = typeof(OfficeData).Assembly.GetManifestResourceStream(OfficeData.SanctoralResource)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        var reader = new Utf8JsonReader(buffer.ToArray(), new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip });
        var scopes = new Stack<HashSet<string>>();
        var duplicates = new List<string>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    scopes.Push([]);
                    break;
                case JsonTokenType.EndObject:
                    scopes.Pop();
                    break;
                case JsonTokenType.PropertyName:
                    var name = reader.GetString()!;
                    if (!scopes.Peek().Add(name))
                    {
                        duplicates.Add(name);
                    }

                    break;
            }
        }

        Assert.Empty(duplicates);
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

            foreach (var memorial in day.OptionalMemorials.Concat(day.MemorialCommemorations))
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
    [InlineData("Our Lady of Guadalupe", Hour.MorningPrayer, "Isaiah 61:10")]
    [InlineData("Our Lady of Guadalupe", Hour.EveningPrayer, "Galatians 4:4-5")]
    public void ShortReading_UsesProperCitations(string name, Hour hour, string expected) =>
        Assert.Equal(expected, Sanctoral.ShortReading(hour, name));

    // Every fixed feast and solemnity has a short reading at both hours and festal Evening Prayer psalmody.
    [Theory]
    [InlineData("The Presentation of the Lord")]
    [InlineData("The Chair of Saint Peter the Apostle")]
    [InlineData("Saint Joseph, Spouse of the Blessed Virgin Mary")]
    [InlineData("The Annunciation of the Lord")]
    [InlineData("The Visitation of the Blessed Virgin Mary")]
    [InlineData("The Nativity of Saint John the Baptist")]
    [InlineData("Saints Peter and Paul, Apostles")]
    [InlineData("The Transfiguration of the Lord")]
    [InlineData("The Assumption of the Blessed Virgin Mary")]
    [InlineData("The Nativity of the Blessed Virgin Mary")]
    [InlineData("The Exaltation of the Holy Cross")]
    [InlineData("Saints Michael, Gabriel and Raphael, Archangels")]
    [InlineData("All Saints")]
    [InlineData("The Dedication of the Lateran Basilica")]
    [InlineData("The Immaculate Conception of the Blessed Virgin Mary")]
    [InlineData("Saint Stephen, the First Martyr")]
    [InlineData("Saint John, Apostle and Evangelist")]
    [InlineData("The Holy Innocents, Martyrs")]
    public void FeastsAndSolemnities_HaveReadingsAndPsalmody(string name)
    {
        Assert.NotNull(Sanctoral.ShortReading(Hour.MorningPrayer, name));
        Assert.NotNull(Sanctoral.ShortReading(Hour.EveningPrayer, name));
        Assert.Equal(3, Sanctoral.EveningPsalmody(name)?.Count);
    }

    // United States observances without a proper office: the office of the day is prayed.
    [Theory]
    [InlineData("Day of Prayer for the Legal Protection of Unborn Children", Hour.MorningPrayer)]
    [InlineData("Day of Prayer for the Legal Protection of Unborn Children", Hour.EveningPrayer)]
    [InlineData("Independence Day", Hour.MorningPrayer)]
    [InlineData("Independence Day", Hour.EveningPrayer)]
    public void ShortReading_ObservancesWithoutProperOffice_AreNull(string name, Hour hour) =>
        Assert.Null(Sanctoral.ShortReading(hour, name));

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
                Assert.False(reading.UseOfficialText);
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

    // Festal psalmody

    private static string[] Psalmody(DateOnly date, Hour hour, CalendarRegion region = CalendarRegion.GeneralRoman)
    {
        var services = new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours();
        services.AddSingleton(LiturgicalCalendars.Create(new LiturgicalCalendarOptions(region)));
        return services.BuildServiceProvider().CreateScope().ServiceProvider
            .GetRequiredService<ILiturgyOfTheHours>().Compose(date, hour).Steps
            .Where(s => s.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
            .Select(s => s.Reference!.Citation.Replace("Canticle: ", string.Empty))
            .ToArray();
    }

    [Fact]
    public void Commons_EachHaveThreeEveningPsalms()
    {
        Assert.All(OfficeData.Sanctoral.Commons, pair => Assert.Equal(3, pair.Value.EveningPsalmody?.Length ?? 0));
    }

    // Dec 12 is a Feast in the United States; 2027 is omitted because it falls on an Advent Sunday,
    // and 2026 for Evening Prayer because that Saturday evening is First Vespers of the Sunday.
    [Theory]
    [InlineData(2025)]
    [InlineData(2028)]
    [InlineData(2029)]
    public void Guadalupe_EveningPrayer_UsesCommonOfTheBlessedVirginMary(int year)
    {
        Assert.Equal(["Psalm 122", "Psalm 127", "Ephesians 1:3-10"], Psalmody(new DateOnly(year, 12, 12), Hour.EveningPrayer, CalendarRegion.UnitedStates));
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2028)]
    public void Guadalupe_MorningPrayer_UsesSundayWeekOne(int year)
    {
        Assert.Equal(["Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149"], Psalmody(new DateOnly(year, 12, 12), Hour.MorningPrayer, CalendarRegion.UnitedStates));
    }

    [Theory]
    [InlineData(2025, CalendarRegion.GeneralRoman)]
    [InlineData(2026, CalendarRegion.GeneralRoman)]
    [InlineData(2027, CalendarRegion.GeneralRoman)]
    [InlineData(2026, CalendarRegion.UnitedStates)]
    public void MartinOfTours_MorningPrayer_UsesProperPsalmody(int year, CalendarRegion region)
    {
        Assert.Equal(["Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149"], Psalmody(new DateOnly(year, 11, 11), Hour.MorningPrayer, region));
    }

    [Fact]
    public void MartinOfTours_MorningReading_IncludesTheFirstPartOfVerseNine()
    {
        Assert.Equal("Hebrews 13:7-9a", Sanctoral.ShortReading(Hour.MorningPrayer, "Saint Martin of Tours, Bishop"));
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void MartinOfTours_OnSunday_DoesNotReplaceSundayPsalmody(CalendarRegion region)
    {
        Assert.Equal(["Psalm 118", "Daniel 3:52-57", "Psalm 150"], Psalmody(new DateOnly(2029, 11, 11), Hour.MorningPrayer, region));
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void MemorialWithoutProperMorningPsalmody_KeepsWeekdayPsalter(CalendarRegion region)
    {
        Assert.Equal(["Psalm 101", "Daniel 3:26-27, 29, 34-41", "Psalm 144:1-10"], Psalmody(new DateOnly(2026, 11, 10), Hour.MorningPrayer, region));
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public void MartinOfTours_EveningPrayer_UsesProperPsalmody(int year)
    {
        Assert.Equal(["Psalm 15", "Psalm 112", "Revelation 15:3-4"], Psalmody(new DateOnly(year, 11, 11), Hour.EveningPrayer));
    }

    [Fact]
    public void MartinOfTours_OnSaturday_YieldsToSundayFirstVespers()
    {
        Assert.Equal(["Psalm 122", "Psalm 130", "Philippians 2:6-11"], Psalmody(new DateOnly(2028, 11, 11), Hour.EveningPrayer));
    }

    [Fact]
    public void ApostleFeast_EveningPrayer_UsesCommonOfApostles()
    {
        Assert.Equal(["Psalm 116:10-19", "Psalm 126", "Ephesians 1:3-10"], Psalmody(new DateOnly(2026, 11, 30), Hour.EveningPrayer));
    }

    [Theory]
    [InlineData(2025, 5, 29, CalendarRegion.GeneralRoman)]
    [InlineData(2025, 6, 1, CalendarRegion.UnitedStates)]
    public void Ascension_EveningPrayer_UsesProperPsalmody(int year, int month, int day, CalendarRegion region)
    {
        Assert.Contains("Psalm 47", Psalmody(new DateOnly(year, month, day), Hour.EveningPrayer, region));
    }

    [Fact]
    public void Pentecost_EveningPrayer_UsesProperPsalmody()
    {
        Assert.Contains("Psalm 114", Psalmody(new DateOnly(2025, 6, 8), Hour.EveningPrayer));
    }

    [Fact]
    public void Memorial_EveningPrayer_KeepsPsalter()
    {
        // Saint Thomas Aquinas (Jan 28, 2026, a Wednesday memorial) takes the psalter, not the Common of Doctors.
        Assert.DoesNotContain("Psalm 112", Psalmody(new DateOnly(2026, 1, 28), Hour.EveningPrayer));
    }

    [Theory]
    [InlineData(2, Hour.EveningPrayer, "Psalm 72:1-11", "Psalm 72:12-19", "Revelation 11:17-18; 12:10b-12a")]
    [InlineData(3, Hour.MorningPrayer, "Psalm 51", "Habakkuk 3:2-4, 13a, 15-19", "Psalm 147:12-20")]
    [InlineData(3, Hour.EveningPrayer, "Psalm 116:10-19", "Psalm 143:1-11", "Philippians 2:6-11")]
    [InlineData(4, Hour.MorningPrayer, "Psalm 64", "Isaiah 38:10-14, 17-20", "Psalm 150")]
    [InlineData(4, Hour.EveningPrayer, "Psalm 116:10-19", "Psalm 143:1-11", "Philippians 2:6-11")]
    [InlineData(5, Hour.MorningPrayer, "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149")]
    [InlineData(5, Hour.EveningPrayer, "Psalm 110:1-5, 7", "Psalm 114", "Revelation 19:1-7")]
    public void PaschalTriduum_UsesProperPsalmody(int aprilDay, Hour hour, string first, string second, string third)
    {
        Assert.Equal([first, second, third], Psalmody(new DateOnly(2026, 4, aprilDay), hour));
    }

	// Moveable celebrations

	[Theory]
	[InlineData("The Epiphany of the Lord")]
	[InlineData("The Baptism of the Lord")]
	[InlineData("The Holy Family of Jesus, Mary and Joseph")]
	[InlineData("The Ascension of the Lord")]
	[InlineData("Pentecost Sunday")]
	[InlineData("The Most Holy Trinity")]
	[InlineData("The Most Holy Body and Blood of Christ")]
	[InlineData("The Most Sacred Heart of Jesus")]
	[InlineData("Our Lord Jesus Christ, King of the Universe")]
	public void MoveableFeastsAndSolemnities_HaveReadingsAndPsalmody(string name)
	{
		Assert.NotNull(Sanctoral.ShortReading(Hour.MorningPrayer, name));
		Assert.NotNull(Sanctoral.ShortReading(Hour.EveningPrayer, name));
		Assert.Equal(3, Sanctoral.EveningPsalmody(name)?.Count);
	}

	[Theory]
	[InlineData("The Blessed Virgin Mary, Mother of the Church")]
	[InlineData("The Immaculate Heart of the Blessed Virgin Mary")]
	public void MoveableMarianMemorials_UseCommonOfTheBlessedVirginMary(string name)
	{
		Assert.Equal(OfficeData.Sanctoral.Commons["blessed-virgin-mary"].Morning, Sanctoral.ShortReading(Hour.MorningPrayer, name));
		Assert.Equal(OfficeData.Sanctoral.Commons["blessed-virgin-mary"].Evening, Sanctoral.ShortReading(Hour.EveningPrayer, name));
	}

	// Citation checks

	private static IEnumerable<(string Key, string Citation)> AllSanctoralCitations()
	{
		var data = OfficeData.Sanctoral;
		foreach (var (key, r) in data.Commons.Select(p => ("commons/" + p.Key, p.Value)).Concat(data.Celebrations.Select(p => ("celebrations/" + p.Key, p.Value))))
		{
			foreach (var citation in new[] { r.Morning, r.Evening, r.FirstEveningCanticle }.Concat(r.MorningPsalmody ?? []).Concat(r.EveningPsalmody ?? []))
			{
				if (citation is not null)
				{
					yield return (key, citation);
				}
			}
		}
	}

	[Fact]
	public void SanctoralData_AllCitationsParse()
	{
		var failures = AllSanctoralCitations().Where(c => BibleCitation.Parse(c.Citation, TestBible.Drb) is null).Select(c => $"{c.Key}: {c.Citation}").ToList();
		Assert.True(failures.Count == 0, "Unparseable citations: " + string.Join("; ", failures));
	}

	[Fact]
	public void SanctoralData_CommonReferencesExist()
	{
		var data = OfficeData.Sanctoral;
		var missing = data.Celebrations.Where(p => p.Value.Common is { } c && !data.Commons.ContainsKey(c)).Select(p => p.Key).ToList();
		Assert.Empty(missing);
	}

	[Fact]
	public void SanctoralData_EveryCelebrationResolvesReadings()
	{
		var data = OfficeData.Sanctoral;
		var empty = data.Celebrations
			.Where(p => (p.Value.Morning ?? (p.Value.Common is { } c ? data.Commons[c].Morning : null)) is null)
			.Select(p => p.Key)
			.ToList();
		Assert.Empty(empty);
	}

	[Fact]
	public void SanctoralData_PsalmodyHasThreeEntries()
	{
		var bad = OfficeData.Sanctoral.Celebrations
			.Where(p => p.Value.MorningPsalmody is { Length: not 3 } || p.Value.EveningPsalmody is { Length: not 3 })
			.Select(p => p.Key).ToList();
		Assert.Empty(bad);
	}
}
