using NSubstitute;
using Respondeo.Bible;
using Respondeo.Bible.Contracts;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class HourPassageLoaderTests
{
    [Fact]
    public async Task DanielCanticle_UsesBundledScriptureWithItsLiturgicalEndingAndNoGloryBe()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var daniel = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "daniel.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/daniel.json"] = daniel,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var engine = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), Substitute.For<IHoursTextProvider>());
        var office = engine.Compose(new(2026, 11, 11), Hour.MorningPrayer);
        var canticle = Assert.Single(office.Steps, s => s.Kind == OfficeStepKind.Canticle);

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [canticle] });

        var body = Assert.Single(result.Passages).Value;
        var scripture = await bible.GetPassageAsync("Daniel 3:57-87");
        var finalBlessing = await bible.GetPassageAsync("Daniel 3:56");
        Assert.NotNull(scripture);
        Assert.NotNull(finalBlessing);
        Assert.StartsWith(scripture, body);
        Assert.Contains("Ananias, Azarias, and Misael, bless ye our Lord: praise and superexalt him for ever.", body);
        Assert.Contains("Benedicamus Patrem et Filium cum Sancto Spiritu;", body);
        Assert.Contains("(Unofficial translation:", body);
        Assert.EndsWith(finalBlessing, body);
        Assert.DoesNotContain("Because he hath delivered", body);
        Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
        Assert.True(body.IndexOf("Benedicamus", StringComparison.Ordinal) < body.IndexOf(finalBlessing, StringComparison.Ordinal));
        Assert.Contains("Not the approved English liturgical translation", canticle.Passage?.Translation);
    }

    [Theory]
    [InlineData(OfficeStepKind.Psalm, true)]
    [InlineData(OfficeStepKind.Canticle, true)]
    [InlineData(OfficeStepKind.Reading, false)]
    public async Task OrdinaryPassage_PreservesPsalmDoxologiesAndUndecoratedReadings(OfficeStepKind kind, bool appendGloryBe)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Psalm 149").Returns("Synthetic body");
        var step = new OfficeStep(kind, "Synthetic step") { Reference = new("Psalm 149") };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Equal(appendGloryBe ? "Synthetic body\n\n" + HourPassageLoader.GloryBe : "Synthetic body", Assert.Single(result.Passages).Value);
    }

    [Fact]
    public async Task MissingPart_DoesNotReturnATruncatedCanticleOrRawCitationFallback()
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Daniel 3:57-87").Returns("Synthetic first part");
        bible.GetPassageAsync("Daniel 3:57-88, 56").Returns("Wrong unarranged fallback");
        var step = DanielStep();

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("Daniel 3:57-88, 56");
    }

    [Fact]
    public async Task InlineLicensedBody_IsNeverReplacedOrGivenAnotherDoxology()
    {
        var bible = Substitute.For<IBibleText>();
        var step = DanielStep() with { Text = "Synthetic complete licensed body" };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Empty(result.Passages);
        await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        Assert.Equal("Synthetic complete licensed body", step.Text);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Daniel 3:56", "Ambiguous fixed text")]
    public async Task InvalidPassagePart_IsRejected(string? citation, string? text)
    {
        var bible = Substitute.For<IBibleText>();
        var step = DanielStep() with { Passage = new([new(citation, text)], "Synthetic translation") };

        await Assert.ThrowsAsync<InvalidDataException>(() => new HourPassageLoader(bible).LoadAsync(Office(step)));
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task HebrewsReading_StopsAtTheFirstSentenceOfVerseNine(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var hebrews = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "hebrews.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/hebrews.json"] = hebrews,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), Substitute.For<IHoursTextProvider>());
        foreach (var date in new[] { new DateOnly(2026, 11, 4), new(2026, 11, 11) })
        {
            var office = hours.Compose(date, Hour.MorningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal("Hebrews 13:7-9a", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages[reading.Reference!.Citation], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            var firstVerses = await bible.GetPassageAsync("Hebrews 13:7-8");
            Assert.NotNull(firstVerses);
            const string lastSentence = "With various & strange doctrines be not led away.";
            Assert.Equal(firstVerses + "\n\n" + lastSentence, body);
            Assert.StartsWith(lastSentence + " For it is best", await bible.GetPassageAsync("Hebrews 13:9"));
            Assert.DoesNotContain("For it is best", body);
            Assert.DoesNotContain("not with meats", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArrangedReading_DoesNotUseRawPartialCitationOrAlterAnInlineBody(bool hasInlineBody)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Hebrews 13:7-9a").Returns("Wrong full verse fallback");
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading")
        {
            Reference = new("Hebrews 13:7-9a"),
            Passage = OfficeData.Texts.ReadingPassages["Hebrews 13:7-9a"],
            Text = hasInlineBody ? "Synthetic complete licensed reading" : null,
            IsLicensedText = hasInlineBody,
        };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("Hebrews 13:7-9a");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete licensed reading", step.Text);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("Hebrews 13:7-8");
        }
    }

    [Theory]
    [InlineData(17, "1 Peter 5:8-9a", "1-peter", "1 Peter 5:8", "Whom resist ye, strong in faith.", "knowing that")]
    [InlineData(20, "Jeremiah 14:9a", "jeremie", null, "But thou o Lord art in us, and thy name is invocated upon us, forsake us not.", "Why wilt thou")]
    public async Task ComplineReadings_UseExactBundledClausesEvenWithSundayPsalmSubstitution(
        int day, string citation, string bookSlug, string? firstCitation, string closingClause, string omittedClause)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", bookSlug + ".json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            [$"_content/Respondeo.Bible/douay-rheims/{bookSlug}.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var fullVerse = await bible.GetPassageAsync(bookSlug == "jeremie" ? "Jeremiah 14:9" : "1 Peter 5:9");
        Assert.NotNull(fullVerse);
        Assert.Contains(omittedClause, fullVerse);
        Assert.Contains(closingClause.TrimEnd('.'), fullVerse);
        var expected = firstCitation is null ? closingClause : await bible.GetPassageAsync(firstCitation) + "\n\n" + closingClause;
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), Substitute.For<IHoursTextProvider>());
            foreach (var choice in Enum.GetValues<ComplinePsalmodyChoice>())
            {
                var office = hours.Compose(new(2026, 11, day), Hour.NightPrayer, complinePsalmody: choice);
                var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
                Assert.Equal(citation, reading.Reference?.Citation);
                Assert.Equal(OfficeData.Texts.ReadingPassages[citation], reading.Passage);
                var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
                var body = Assert.Single(result.Passages).Value;
                Assert.Equal(expected, body);
                Assert.DoesNotContain(omittedClause, body);
                Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
                Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
            }
        }
    }

    [Theory]
    [InlineData(17)]
    [InlineData(20)]
    public async Task ComplineReading_InlineProviderBodyTakesPrecedenceOverTheExcerpt(int day)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
            call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                ? new LicensedText("Synthetic complete approved reading") : null);
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2026, 11, day), Hour.NightPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.Equal("Synthetic complete approved reading", reading.Text);
        Assert.True(reading.IsLicensedText);
        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
    }

    [Fact]
    public async Task MissingTuesdayExcerpt_DoesNotReturnOnlyTheClosingClauseOrTheFullVerse()
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("1 Peter 5:8-9a").Returns("Wrong full verse fallback");
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), Substitute.For<IHoursTextProvider>());
        var office = hours.Compose(new(2026, 11, 17), Hour.NightPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.Empty(result.Passages);
        await bible.Received(1).GetPassageAsync("1 Peter 5:8");
        await bible.DidNotReceive().GetPassageAsync("1 Peter 5:8-9a");
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekThreeWednesday_JobReadingOmitsTheRebukeAndNarrator(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var job = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "job.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/job.json"] = job,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("Job 2:10");
        var firstVerse = await bible.GetPassageAsync("Job 1:21");
        Assert.NotNull(fullVerse);
        Assert.NotNull(firstVerse);
        const string question = "If we have received good things of the hand of God, evil things why should we not receive?";
        Assert.Contains(char.ToLowerInvariant(question[0]) + question[1..], fullVerse);
        Assert.Contains("Thou hast spoken like one of the foolish women", fullVerse);
        Assert.Contains("Job sinned not with his lips", fullVerse);
        foreach (var date in new[] { new DateOnly(2027, 1, 27), new(2027, 5, 19) })
        {
            var office = hours.Compose(date, Hour.MorningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(3, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("Job 1:21; 2:10b", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["Job 1:21; 2:10b"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(firstVerse + "\n\n" + question, body);
            Assert.DoesNotContain("foolish women", body);
            Assert.DoesNotContain("Job sinned not", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
        var followingWeek = hours.Compose(new(2027, 2, 3), Hour.MorningPrayer);
        var otherReading = Assert.Single(followingWeek.Steps, step => step.Kind == OfficeStepKind.Reading);
        Assert.Equal(4, followingWeek.PsalterWeek);
        Assert.Equal("Deuteronomy 4:39-40", otherReading.Reference?.Citation);
        Assert.Null(otherReading.Passage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JobReading_MissingFirstVerseCannotFallBackAndInlineBodyRemainsUnchanged(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Job reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2027, 1, 27), Hour.MorningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Job 1:21; 2:10b").Returns("Wrong unabridged fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("Job 1:21; 2:10b");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Job reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("Job 1:21");
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekFourSaturday_PeterReadingStopsBeforeTheReferenceToPaul(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "2-peter.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/2-peter.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("2 Peter 3:15");
        var firstVerses = await bible.GetPassageAsync("2 Peter 3:13-14");
        Assert.NotNull(fullVerse);
        Assert.NotNull(firstVerses);
        const string closingClause = "And the longanimity of our Lord, do ye account salvation.";
        Assert.StartsWith(char.ToLowerInvariant(closingClause[0]) + closingClause[1..^1] + ",", fullVerse);
        Assert.Contains("our most dear brother Paul", fullVerse);
        foreach (var date in new[] { new DateOnly(2026, 11, 14), new(2027, 6, 26), new(2027, 7, 24) })
        {
            var office = hours.Compose(date, Hour.MorningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(4, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("2 Peter 3:13-15a", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["2 Peter 3:13-15a"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(firstVerses + "\n\n" + closingClause, body);
            Assert.DoesNotContain("brother Paul", body);
            Assert.DoesNotContain("hath written to you", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PeterReading_MissingFirstVersesCannotFallBackAndInlineBodyRemainsUnchanged(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Peter reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2026, 11, 14), Hour.MorningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("2 Peter 3:13-15a").Returns("Wrong unabridged fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("2 Peter 3:13-15a");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Peter reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("2 Peter 3:13-14");
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekThreeSunday_EzekielReadingBeginsAtTheProclamation(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "ezechiel.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/ezechiel.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("Ezekiel 37:12");
        var remainingVerses = await bible.GetPassageAsync("Ezekiel 37:13-14");
        Assert.NotNull(fullVerse);
        Assert.NotNull(remainingVerses);
        const string opening = "Thus saith our Lord God: Behold I will open your graves, and will bring you out of your sepulchres o my people: "
            + "and will bring you into the land of Israel.";
        Assert.Equal("Therefore prophesy, and thou shalt say to them: " + opening, fullVerse);
        foreach (var date in new[] { new DateOnly(2027, 1, 24), new(2027, 6, 13), new(2027, 7, 11) })
        {
            var office = hours.Compose(date, Hour.MorningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(3, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("Ezekiel 37:12b-14", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["Ezekiel 37:12b-14"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(opening + "\n\n" + remainingVerses, body);
            Assert.DoesNotContain("Therefore prophesy", body);
            Assert.DoesNotContain("thou shalt say to them", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EzekielReading_MissingVersesCannotReturnAnOpeningOnlyAndInlineBodyTakesPrecedence(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Ezekiel reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2027, 1, 24), Hour.MorningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Ezekiel 37:12b-14").Returns("Wrong unabridged fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("Ezekiel 37:12b-14");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Ezekiel reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("Ezekiel 37:13-14");
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekTwoWednesdayVespers_PeterReadingBeginsWithMutualHumility(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "1-peter.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/1-peter.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("1 Peter 5:5");
        var remainingVerses = await bible.GetPassageAsync("1 Peter 5:6-7");
        Assert.NotNull(fullVerse);
        Assert.NotNull(remainingVerses);
        const string opening = "And do ye all insinuate humility one to another, because God resisteth the proud, and to the humble he giveth grace.";
        Assert.Equal("In like manner ye young men be subject to the seniors. " + opening, fullVerse);
        foreach (var date in new[] { new DateOnly(2027, 1, 20), new(2027, 6, 9), new(2027, 7, 7) })
        {
            var office = hours.Compose(date, Hour.EveningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(2, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("1 Peter 5:5b-7", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["1 Peter 5:5b-7"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(opening + "\n\n" + remainingVerses, body);
            Assert.DoesNotContain("young men", body);
            Assert.DoesNotContain("seniors", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VespersPeterReading_MissingVersesCannotReturnAnOpeningOnlyAndInlineBodyTakesPrecedence(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Vespers Peter reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2027, 1, 20), Hour.EveningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("1 Peter 5:5b-7").Returns("Wrong unabridged fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("1 Peter 5:5b-7");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Vespers Peter reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("1 Peter 5:6-7");
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekTwoFridayVespers_CorinthiansReadingStopsAtTheRevelationThroughTheSpirit(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "1-corinthians.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/1-corinthians.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("1 Corinthians 2:10");
        var firstVerses = await bible.GetPassageAsync("1 Corinthians 2:7-9");
        Assert.NotNull(firstVerses);
        const string closing = "But to us God hath revealed by his Spirit.";
        Assert.Equal(closing + " For the Spirit searcheth all things, yea the profundities of God.", fullVerse);
        foreach (var date in new[] { new DateOnly(2027, 1, 22), new(2027, 7, 9), new(2027, 10, 29) })
        {
            var office = hours.Compose(date, Hour.EveningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(2, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("1 Corinthians 2:7-10a", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["1 Corinthians 2:7-10a"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(firstVerses + "\n\n" + closing, body);
            Assert.DoesNotContain("Spirit searcheth", body);
            Assert.DoesNotContain("profundities", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
            Assert.Contains("Not the approved English liturgical translation", reading.Passage!.Translation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CorinthiansReading_MissingVersesCannotFallBackAndInlineBodyTakesPrecedence(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Corinthians reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2027, 1, 22), Hour.EveningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("1 Corinthians 2:7-10a").Returns("Wrong unabridged fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("1 Corinthians 2:7-10a");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Corinthians reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("1 Corinthians 2:7-9");
        }
    }

    private static readonly string[] UnresolvedReadings =
    [
        "1 Corinthians 15:1-2a, 3-4", "1 Corinthians 15:3-5, 8a", "1 Corinthians 7:32b, 34a",
        "1 John 3:1a, 2", "2 Corinthians 6:16b; 7:1", "2 Timothy 2:8a, 11-13",
        "Acts 26:16b-18", "Acts 5:12a, 14-16", "Acts 6:2b-5a",
        "Colossians 1:2b-6a", "Colossians 1:3-6a", "Ephesians 5:25b-27", "Genesis 28:12-13a",
        "Hebrews 2:9b-10", "Isaiah 61:1-2a", "Joel 2:27-3:1a", "Micah 5:2-4a",
        "Revelation 1:4b-5", "Romans 5:20b-21", "Romans 8:3b-4",
    ];

    public static IEnumerable<object[]> UnresolvedPartialReadings() => UnresolvedReadings.Select(citation => new object[] { citation });

    [Fact]
    public void PartialReadingInventory_EveryEmbeddedCitationIsArrangedOrExplicitlyPending()
    {
        var psalter = OfficeData.Psalter;
        var readings = psalter.MorningReadings!.Concat(psalter.EveningReadings!).SelectMany(week => week.Values)
            .Concat(psalter.Night.Values.Select(night => night.Reading))
            .Concat(OfficeData.Sanctoral.Commons.Values.Concat(OfficeData.Sanctoral.Celebrations.Values)
                .SelectMany(proper => new[] { proper.Morning, proper.Evening }).OfType<string>());
        var partial = readings.Where(citation => new PsalmodyReference(citation).HasPartialVerses).Distinct().ToArray();
        var arrangements = OfficeData.Texts.ReadingPassages;

        Assert.Equal(29, partial.Length);
        Assert.Equal(9, arrangements.Count);
        Assert.Equal(UnresolvedReadings.Order(StringComparer.Ordinal), partial.Except(arrangements.Keys).Order(StringComparer.Ordinal));
        Assert.Empty(arrangements.Keys.Except(partial));
        foreach (var passage in arrangements.Values)
        {
            Assert.All(passage.Parts.Where(part => part.Citation is not null),
                part => Assert.False(new PsalmodyReference(part.Citation!).HasPartialVerses));
        }
    }

    [Theory]
    [InlineData("1 John 3:1a, 2", true)]
    [InlineData("Job 1:21; 2:10b", true)]
    [InlineData("John 1:1c", true)]
    [InlineData("John 1:1A", true)]
    [InlineData("Isaiah 61:1- 2a", true)]
    [InlineData("Colossians 1:2b-6a", true)]
    [InlineData("Acts 1:1-2", false)]
    [InlineData("1 Chronicles 1:1", false)]
    [InlineData("Psalm 149", false)]
    public void PartialVerseDetection_DoesNotInferTextOrMistakeBookNames(string citation, bool expected) =>
        Assert.Equal(expected, new PsalmodyReference(citation).HasPartialVerses);

    [Fact]
    public async Task PartialReference_RealBibleAdapterReturnsAllBoundaryVersesWithoutChangingTheAssignment()
    {
        const string book = """
            {"chapters":[{"chapter":3,"verses":[
                {"verse":12,"text":"Excluded preceding verse"},
                {"verse":13,"text":"Complete verse thirteen"},
                {"verse":14,"text":"Complete verse fourteen"},
                {"verse":15,"text":"Complete verse fifteen, including its second clause"},
                {"verse":16,"text":"Excluded following verse"}]}]}
            """;
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            [$"_content/Respondeo.Bible/douay-rheims/{TestBible.Drb.Manifest["2-peter"].File}"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("2 Peter 3:13-15a") };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Equal(string.Join(Environment.NewLine, "Complete verse thirteen", "Complete verse fourteen",
            "Complete verse fifteen, including its second clause"), Assert.Single(result.Passages).Value);
        Assert.Equal("2 Peter 3:13-15a", step.Reference.Citation);
    }

    [Theory]
    [MemberData(nameof(UnresolvedPartialReadings))]
    public async Task UnresolvedPartialReading_LoadsWholeVersesAndPreservesReferenceAndInlineText(string citation)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync(citation).Returns("Synthetic whole-verse body");
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new(citation) };

        var fallback = await new HourPassageLoader(bible).LoadAsync(Office(step));
        var inline = step with { Text = "Synthetic authorized excerpt", IsLicensedText = true };
        var supplied = await new HourPassageLoader(bible).LoadAsync(Office(inline));

        Assert.Equal("Synthetic whole-verse body", Assert.Single(fallback.Passages).Value);
        Assert.Equal(citation, step.Reference.Citation);
        Assert.Empty(supplied.Passages);
        Assert.Equal("Synthetic authorized excerpt", inline.Text);
        await bible.Received(1).GetPassageAsync(citation);
    }

    [Theory]
    [InlineData(OfficeStepKind.Reading)]
    [InlineData(OfficeStepKind.Canticle)]
    public async Task ArrangedPassage_AllowsWholeVerseResolutionOfPartialCitations(OfficeStepKind kind)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("John 1:1a").Returns("Synthetic complete verse");
        var step = new OfficeStep(kind, "Arrangement")
        {
            Passage = new([new(Citation: "John 1:1a"), new(Text: "Synthetic ending")], "Synthetic translation"),
        };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step));

        Assert.Equal("Synthetic complete verse\n\nSynthetic ending", Assert.Single(result.Passages).Value);
        Assert.Equal("John 1:1a", step.Passage.Parts[0].Citation);
        await bible.Received(1).GetPassageAsync("John 1:1a");
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void ForwardOffices_AllowBibleFallbackForEveryUnarrangedPartialReading(string profile)
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), Substitute.For<IHoursTextProvider>());
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var arranged = new HashSet<string>(StringComparer.Ordinal);
        for (var date = new DateOnly(2026, 11, 11); date <= new DateOnly(2028, 12, 31); date = date.AddDays(1))
        {
            foreach (var hour in Enum.GetValues<Hour>())
            {
                var office = hours.Compose(date, hour);
                foreach (var reading in office.Steps.Where(step => step.Kind == OfficeStepKind.Reading && step.Reference is not null))
                {
                    var citation = reading.Reference!.Citation;
                    if (!reading.Reference.HasPartialVerses)
                    {
                        Assert.False(reading.UseOfficialText);
                        continue;
                    }
                    if (OfficeData.Texts.ReadingPassages.TryGetValue(citation, out var passage))
                    {
                        Assert.Equal(passage, reading.Passage);
                        Assert.False(reading.UseOfficialText);
                        arranged.Add(citation);
                    }
                    else
                    {
                        Assert.Null(reading.Passage);
                        Assert.False(reading.UseOfficialText, $"{profile} {date:yyyy-MM-dd} {hour}: {citation}");
                        Assert.Contains(citation, UnresolvedReadings);
                        unresolved.Add(citation);
                    }
                }
            }
        }
        Assert.Equal(9, arranged.Count);
        Assert.Contains("2 Timothy 2:8a, 11-13", unresolved);
        Assert.Contains("1 Corinthians 7:32b, 34a", unresolved);
        Assert.Contains("Colossians 1:2b-6a", unresolved);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task WeekOneMondayVespers_ColossiansReadingOmitsTheOpeningReport(string profile)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Respondeo.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var book = File.ReadAllText(Path.Combine(root.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "colossians.json"));
        using var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Bible/douay-rheims/colossians.json"] = book,
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        var translations = Substitute.For<IBibleTranslations>();
        translations.GetDefaultAsync().Returns(TestBible.Drb);
        var bible = new BibleText(http, translations);
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, Substitute.For<IHoursTextProvider>());
        var fullVerse = await bible.GetPassageAsync("Colossians 1:9");
        var remaining = await bible.GetPassageAsync("Colossians 1:10-11");
        Assert.NotNull(fullVerse);
        Assert.NotNull(remaining);
        const string opening = "That you may be filled with the knowledge of his will, in all wisdom, and spiritual understanding:";
        Assert.Equal("Therefore we also from the day that we heard it, cease not praying for you and desiring, "
            + char.ToLowerInvariant(opening[0]) + opening[1..], fullVerse);
        foreach (var date in new[] { new DateOnly(2027, 1, 11), new(2027, 2, 8), new(2027, 8, 23) })
        {
            var office = hours.Compose(date, Hour.EveningPrayer);
            var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
            Assert.Equal(1, calendar.ForWeek(date).PsalterWeek);
            Assert.Equal("Colossians 1:9b-11", reading.Reference?.Citation);
            Assert.Equal(OfficeData.Texts.ReadingPassages["Colossians 1:9b-11"], reading.Passage);
            var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });
            var body = Assert.Single(result.Passages).Value;
            Assert.Equal(opening + "\n\n" + remaining, body);
            Assert.DoesNotContain("cease not praying", body);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, body);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ColossiansReading_MissingVersesCannotReturnAnOpeningOnlyAndLicensedBodyTakesPrecedence(bool hasInlineBody)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        if (hasInlineBody)
        {
            provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
                call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                    ? new LicensedText("Synthetic complete Colossians reading") : null);
        }
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);
        var office = hours.Compose(new(2027, 1, 11), Hour.EveningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Colossians 1:9b-11").Returns("Wrong whole-verse fallback");

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.NotNull(reading.Passage);
        Assert.Empty(result.Passages);
        await bible.DidNotReceive().GetPassageAsync("Colossians 1:9b-11");
        if (hasInlineBody)
        {
            Assert.Equal("Synthetic complete Colossians reading", reading.Text);
            Assert.True(reading.IsLicensedText);
            await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
        }
        else
        {
            await bible.Received(1).GetPassageAsync("Colossians 1:10-11");
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public async Task UnarrangedProperReading_StillAcceptsItsLicensedBody(string profile)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call =>
            call.Arg<IEnumerable<string>>().Any(key => key.EndsWith("/reading", StringComparison.Ordinal))
                ? new LicensedText("Synthetic authorized proper reading") : null);
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), provider);
        var office = hours.Compose(new(2027, 12, 8), Hour.MorningPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();

        var result = await new HourPassageLoader(bible).LoadAsync(office with { Steps = [reading] });

        Assert.Equal("Romans 5:20b-21", reading.Reference?.Citation);
        Assert.Null(reading.Passage);
        Assert.Equal("Synthetic authorized proper reading", reading.Text);
        Assert.True(reading.IsLicensedText);
        Assert.False(reading.UseOfficialText);
        Assert.Empty(result.Passages);
        await bible.DidNotReceiveWithAnyArgs().GetPassageAsync(default!);
    }

    public static IEnumerable<object[]> UnresolvedPartialPsalmody() =>
    [
        new object[] { "Judith 16:2-3a, 13-15" },
        new object[] { "Exodus 15:1-4a, 8-13, 17-18" },
        new object[] { "Habakkuk 3:2-4, 13a, 15-19" },
        new object[] { "Isaiah 66:10-14a" },
        new object[] { "Revelation 11:17-18; 12:10b-12a" },
    ];

    [Fact]
    public void PartialPsalmodyInventory_ListsEveryEmbeddedUnarrangedSuffixCitation()
    {
        var psalter = OfficeData.Psalter;
        var references = psalter.Morning.Concat(psalter.Evening).SelectMany(week => week.Values).SelectMany(set => set)
            .Concat(psalter.Night.Values.SelectMany(night => night.Psalms))
            .Concat(OfficeData.Sanctoral.Commons.Values.Concat(OfficeData.Sanctoral.Celebrations.Values)
                .SelectMany(proper => (proper.MorningPsalmody ?? []).Concat(proper.EveningPsalmody ?? [])
                    .Concat(proper.FirstEveningCanticle is { } canticle ? new[] { canticle } : [])));
        var partial = references.Where(citation => new PsalmodyReference(citation).HasPartialVerses).Distinct().Order(StringComparer.Ordinal);

        Assert.Equal(UnresolvedPartialPsalmody().Select(row => (string)row[0]).Order(StringComparer.Ordinal), partial);
        Assert.Empty(partial.Intersect(OfficeData.Texts.PsalmodyPassages.Keys));
    }

    [Theory]
    [MemberData(nameof(UnresolvedPartialPsalmody))]
    public async Task PartialPsalmody_LoadsWholeVersesForStepsAndOptionsAndPreservesInlineBodies(string citation)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync(citation).Returns("Synthetic whole-verse canticle");
        var step = new OfficeStep(OfficeStepKind.Canticle, "Canticle") { Reference = new(citation) };
        var inline = step with { Text = "Synthetic authorized canticle", IsLicensedText = true };
        var parent = new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [step, inline] };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(step, inline, parent));

        Assert.Equal("Synthetic whole-verse canticle\n\n" + HourPassageLoader.GloryBe, Assert.Single(result.Passages).Value);
        Assert.Equal(result.Passages[0], Assert.Single(result.OptionPassages).Value);
        Assert.Equal((2, 0), Assert.Single(result.OptionPassages).Key);
        Assert.Equal(citation, step.Reference.Citation);
        Assert.Empty(result.OptionStanzas);
        Assert.Equal("Synthetic authorized canticle", inline.Text);
        await bible.Received(2).GetPassageAsync(citation);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void ForwardPsalmody_PreservesPartialCitationsAndRequiresOfficialSources(string profile)
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), Substitute.For<IHoursTextProvider>());
        var expected = UnresolvedPartialPsalmody().Select(row => (string)row[0]).ToHashSet(StringComparer.Ordinal);
        var observed = new HashSet<string>(StringComparer.Ordinal);
        for (var date = new DateOnly(2026, 11, 11); date <= new DateOnly(2028, 12, 31); date = date.AddDays(1))
        {
            foreach (var hour in Enum.GetValues<Hour>())
            {
                foreach (var step in hours.Compose(date, hour).Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle))
                {
                    if (step.Reference?.HasPartialVerses != true)
                    {
                        continue;
                    }
                    Assert.Contains(step.Reference.Citation, expected);
                    Assert.Null(step.Passage);
                    Assert.True(step.UseOfficialText);
                    observed.Add(step.Reference.Citation);
                }
            }
        }
        Assert.Equal(expected.Order(StringComparer.Ordinal), observed.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PartialStanza_NeverFallsBackToTheFullPsalmAndAllowsCompleteBodies(bool inline, bool arranged)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Psalm 95").Returns("Wrong full psalm");
        bible.GetPassageAsync("Psalm 95:1-7a").Returns("Synthetic whole stanza");
        var option = new OfficeStep(OfficeStepKind.Psalm, "Psalm 95")
        {
            Reference = new("Psalm 95"),
            Stanzas = [new("Psalm 95:1-7a")],
            Text = inline ? "Synthetic authorized body" : null,
            Passage = arranged ? new([new(Text: "Synthetic complete arrangement")], "Synthetic source") : null,
        };
        var parent = new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option] };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(parent));

        Assert.Empty(result.Passages);
        Assert.Equal("Psalm 95:1-7a", option.Stanzas[0].Citation);
        if (arranged)
        {
            Assert.Empty(result.OptionStanzas);
            Assert.Equal("Synthetic complete arrangement", Assert.Single(result.OptionPassages).Value);
        }
        else if (inline)
        {
            Assert.Empty(result.OptionStanzas);
            Assert.Empty(result.OptionPassages);
            Assert.Equal("Synthetic authorized body", option.Text);
        }
        else
        {
            Assert.Equal(new[] { "Synthetic whole stanza" }, Assert.Single(result.OptionStanzas).Value);
            Assert.Empty(result.OptionPassages);
            await bible.Received(1).GetPassageAsync("Psalm 95:1-7a");
        }
        await bible.DidNotReceive().GetPassageAsync("Psalm 95");
    }

    [Fact]
    public async Task WholeVerseOptionStanzas_KeepTheirExistingBodies()
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Psalm 95:1-5").Returns("Synthetic first stanza");
        bible.GetPassageAsync("Psalm 95:6-11").Returns("Synthetic second stanza");
        var option = new OfficeStep(OfficeStepKind.Psalm, "Psalm 95")
        {
            Reference = new("Psalm 95"),
            Stanzas = [new("Psalm 95:1-5"), new("Psalm 95:6-11")],
        };
        var result = await new HourPassageLoader(bible).LoadAsync(Office(new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option] }));

        Assert.Equal(new[] { "Synthetic first stanza", "Synthetic second stanza" }, Assert.Single(result.OptionStanzas).Value);
        Assert.Empty(result.OptionPassages);
        await bible.DidNotReceive().GetPassageAsync("Psalm 95");
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, null)]
    [InlineData(0, "")]
    [InlineData(1, "")]
    [InlineData(0, " \r\n\t")]
    [InlineData(1, " \r\n\t")]
    public async Task MissingRequiredStanza_DiscardsTheSetWithoutLoadingTheParentPsalm(int missingIndex, string? missingText)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Psalm 95:1-5").Returns(missingIndex == 0 ? missingText : "Synthetic first stanza");
        bible.GetPassageAsync("Psalm 95:6-11").Returns(missingIndex == 1 ? missingText : "Synthetic second stanza");
        bible.GetPassageAsync("Psalm 95").Returns("Wrong full-psalm fallback");
        var option = new OfficeStep(OfficeStepKind.Psalm, "Psalm 95")
        {
            Reference = new("Psalm 95"),
            Stanzas = [new("Psalm 95:1-5"), new("Psalm 95:6-11")],
        };
        var control = new OfficeStep(OfficeStepKind.Psalm, "Independent option") { Reference = new("Psalm 100") };
        bible.GetPassageAsync("Psalm 100").Returns("Synthetic independent psalm");
        var parent = new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option, control] };

        var result = await new HourPassageLoader(bible).LoadAsync(Office(parent));

        Assert.Empty(result.OptionStanzas);
        Assert.Empty(result.Passages);
        var loaded = Assert.Single(result.OptionPassages);
        Assert.Equal((0, 1), loaded.Key);
        Assert.Equal("Synthetic independent psalm\n\n" + HourPassageLoader.GloryBe, loaded.Value);
        await bible.DidNotReceive().GetPassageAsync("Psalm 95");
        await bible.Received(1).GetPassageAsync("Psalm 95:1-5");
        if (missingIndex == 0)
        {
            await bible.DidNotReceive().GetPassageAsync("Psalm 95:6-11");
        }
        else
        {
            await bible.Received(1).GetPassageAsync("Psalm 95:6-11");
        }
    }

    private static OfficeStep DanielStep() => new(OfficeStepKind.Canticle, "Daniel canticle")
    {
        Reference = new("Daniel 3:57-88, 56"),
        Passage = OfficeData.Texts.PsalmodyPassages["Daniel 3:57-88, 56"],
        UseOfficialText = true,
    };

    private static Office Office(params OfficeStep[] steps) => new(
        new(2026, 11, 11), Hour.MorningPrayer, "Synthetic office",
        new(new(2026, 11, 11), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, null), 4, steps);
}
