using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Bible.Contracts;
using Respondeo.Components;
using Respondeo.Components.Players;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.Services.Calendar;
using Respondeo.Services.Interop;

namespace Respondeo.UnitTests.Pages;

public class HourPlayerTests : TestContext
{
    public HourPlayerTests()
    {
        ComponentFactories.AddStub<PageShell>();
        ComponentFactories.AddStub<PlayerProgressBar>();
        Services.AddScoped<DialogInterop>();
        Services.AddScoped<NavigationInterop>();
        Services.AddSingleton<OrbLauncher>();
        Services.AddSingleton(Substitute.For<ICalendarPreference>());
        Services.AddSingleton<Func<DateOnly>>(() => new(2026, 11, 11));
        var settings = Substitute.For<IHourPlayerSettingsService>();
        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true });
        Services.AddSingleton(settings);
    }

    [Fact]
    public void MissingPrescribedAntiphon_IsExplicitEvenWhenThePsalmBodyIsPresent()
    {
        var step = new OfficeStep(OfficeStepKind.Psalm, "Psalm 63:2-9")
        {
            Reference = new("Psalm 63:2-9"),
            Text = "Synthetic body without a supplied antiphon",
            UseOfficialText = true,
        };

        var cut = Render(step, Substitute.For<IBibleText>());

        Assert.Equal(step.Text, cut.Find(".hourplayer__text").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__antiphon"));
        Assert.Empty(cut.FindAll(".hourplayer__calendar-warning"));
        Assert.Equal("This psalmody's prescribed antiphon is not available here. Read it from an official source below.",
            cut.Find(".hourplayer__official[role=note]").TextContent);
    }

    [Fact]
    public void ArrangedDanielCanticle_RendersItsEndingAndUnofficialLabelWithoutAnExtraGloryBe()
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("Daniel 3:57-87").Returns("Synthetic first Scripture excerpt");
        bible.GetPassageAsync("Daniel 3:56").Returns("Synthetic final Scripture blessing");
        var step = new OfficeStep(OfficeStepKind.Canticle, "Daniel 3:57-88, 56")
        {
            Reference = new("Daniel 3:57-88, 56"),
            Passage = OfficeData.Texts.PsalmodyPassages["Daniel 3:57-88, 56"],
            UseOfficialText = true,
        };

        var cut = Render(step, bible);

        cut.WaitForAssertion(() => Assert.Contains("Synthetic final Scripture blessing", cut.Find(".hourplayer__text").TextContent));
        var body = cut.Find(".hourplayer__text").TextContent;
        Assert.Contains("Benedicamus Patrem et Filium cum Sancto Spiritu;", body);
        Assert.DoesNotContain("Glory be to the Father", body);
        Assert.Equal(step.Passage.Translation, cut.Find(".hourplayer__translation").TextContent);
        Assert.Single(cut.FindAll(".hourplayer__official[role=note]"));
    }

    [Theory]
    [InlineData(2027, 3, 25, "evening", "Lord's Supper", "209")]
    [InlineData(2027, 3, 26, "evening", "Lord's Passion", "209")]
    [InlineData(2027, 3, 27, "night", "Easter Vigil", "211")]
    [InlineData(2026, 12, 24, "night", "Office of Readings vigil", "215")]
    public void Confirmed_participation_replaces_steps_without_claiming_the_Hour_was_prayed(
        int year, int month, int day, string slug, string label, string paragraph)
    {
        Services.AddSingleton<Func<DateOnly>>(() => new(year, month, day));
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, slug));
        var originalSteps = cut.FindAll(".hourplayer__step").Count;

        Assert.True(originalSteps > 0);
        Assert.Contains(label, cut.Find(".hourplayer__participation").TextContent);
        Assert.False(cut.Find(".hourplayer__participation input").HasAttribute("checked"));
        cut.Find(".hourplayer__head").Click();
        cut.Find(".hourplayer__participation input").Change(true);

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".hourplayer__step")));
        Assert.Contains("not said separately", cut.Find(".hourplayer__omission").TextContent);
        Assert.Contains($"no. {paragraph}", cut.Find(".hourplayer__participation").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        Assert.Empty(cut.FindAll(".hourplayer__hint"));
        Assert.Empty(cut.FindComponents<Stub<PlayerProgressBar>>());
        Assert.DoesNotContain("All steps prayed", cut.Markup);

        cut.Find(".hourplayer__participation input").Change(false);
        cut.WaitForAssertion(() => Assert.Equal(originalSteps, cut.FindAll(".hourplayer__step").Count));
        Assert.Empty(cut.FindAll(".hourplayer__omission"));
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        Assert.Equal("is-current", cut.Find("#hour-current-step").ClassList.Single(c => c == "is-current"));
        Assert.Single(cut.FindComponents<Stub<PlayerProgressBar>>());

        cut.Find(".hourplayer__participation input").Change(true);
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "morning"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".hourplayer__step")));
        Assert.Empty(cut.FindAll(".hourplayer__participation"));
        Assert.Empty(cut.FindAll(".hourplayer__omission"));
    }

    [Fact]
    public void Service_date_preserves_previous_nights_Office_after_midnight_and_calendar_changes()
    {
        var clock = new DateOnly(2027, 3, 28);
        Services.AddSingleton<Func<DateOnly>>(() => clock);
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));

        cut.Find("#hour-service-date").Change("2027-03-27");
        Assert.Contains("Saturday, March 27, 2027", cut.Find(".hourplayer__meta").TextContent);
        Assert.Contains("Easter Vigil", cut.Find(".hourplayer__participation").TextContent);
        cut.Find(".hourplayer__participation input").Change(true);
        Assert.Single(cut.FindAll(".hourplayer__omission"));

        clock = new(2027, 3, 29);
        Services.GetRequiredService<ICalendarPreference>().Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".hourplayer__step")));
        Assert.Equal("2027-03-27", cut.Find("#hour-service-date").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".hourplayer__omission"));
        Assert.Contains("Easter Vigil", cut.Find(".hourplayer__participation").TextContent);

        cut.Find("#hour-service-date").Change("2027-03-28");
        Assert.Empty(cut.FindAll(".hourplayer__participation"));
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        Assert.Contains("Sunday, March 28, 2027", cut.Find(".hourplayer__meta").TextContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("2027-02-29")]
    public void Invalid_service_date_keeps_the_Office_and_reports_an_error(string input)
    {
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));
        var originalTitle = cut.Find("h1").TextContent;

        cut.Find("#hour-service-date").Change(input);

        Assert.Equal(originalTitle, cut.Find("h1").TextContent);
        Assert.Equal("Choose a valid service date.", cut.Find("#hour-service-date-error").TextContent);
        Assert.Equal("true", cut.Find("#hour-service-date").GetAttribute("aria-invalid"));
        cut.Find("#hour-service-date").Change("2027-03-27");
        Assert.Empty(cut.FindAll("#hour-service-date-error"));
        Assert.Contains("Saturday, March 27, 2027", cut.Find(".hourplayer__meta").TextContent);
    }

    [Fact]
    public async Task Earlier_date_passage_load_cannot_overwrite_the_selected_dates_text()
    {
        var engine = Substitute.For<ILiturgyOfTheHours>();
        var bible = Substitute.For<IBibleText>();
        var delayed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        foreach (var day in new[] { 11, 12, 13 })
        {
            var date = new DateOnly(2026, 11, day);
            var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new($"Psalm {day}") };
            engine.Compose(date, Hour.NightPrayer).Returns(new Office(date, Hour.NightPrayer, $"Office {day}",
                new(date, LiturgicalSeason.OrdinaryTime, LiturgicalColor.Green, null), 4, [step]));
        }
        bible.GetPassageAsync("Psalm 11").Returns(Task.FromResult<string?>("Initial passage"));
        bible.GetPassageAsync("Psalm 12").Returns(delayed.Task);
        bible.GetPassageAsync("Psalm 13").Returns(Task.FromResult<string?>("Selected date passage"));
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(bible));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));

        var olderLoad = cut.Find("#hour-service-date").ChangeAsync(new() { Value = "2026-11-12" });
        cut.Find("#hour-service-date").Change("2026-11-13");
        cut.WaitForAssertion(() => Assert.Equal("Selected date passage", cut.Find(".hourplayer__text").TextContent));
        delayed.SetResult("Stale date passage");
        await olderLoad;

        Assert.Equal("Office 13", cut.Find("h1").TextContent);
        Assert.Equal("Selected date passage", cut.Find(".hourplayer__text").TextContent);
        Assert.DoesNotContain("Stale date passage", cut.Markup);
    }

    [Fact]
    public async Task Browsing_another_service_date_does_not_advance_todays_Invitatory_cycle()
    {
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var settings = Services.GetRequiredService<IHourPlayerSettingsService>();
        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = InvitatoryMode.Cycle });
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));

        cut.Find("#hour-service-date").Change("2027-03-27");
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "morning"));

        Assert.Equal("2027-03-27", cut.Find("#hour-service-date").GetAttribute("value"));
        await settings.DidNotReceive().UpdateAsync(Arg.Any<Func<HourPlayerSettings, HourPlayerSettings>>());
    }

    [Theory]
    [InlineData(2027, 3, 29, "SundayII", "SundayI", "Deuteronomy 6:4-7", "Revelation 22:4-5")]
    [InlineData(2027, 4, 3, "SundayI", "SundayII", "Revelation 22:4-5", "Deuteronomy 6:4-7")]
    public void Octave_Compline_form_choice_recomposes_and_resets_without_leaking_to_another_context(
        int year, int month, int day, string defaultForm, string chosenForm, string chosenReading, string defaultReading)
    {
        Services.AddSingleton<Func<DateOnly>>(() => new(year, month, day));
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));
        var title = cut.Find("h1").TextContent;
        var metadata = cut.Find(".hourplayer__meta").TextContent;

        Assert.Equal(defaultForm, cut.Find("#hour-compline-form").GetAttribute("value"));
        Assert.Equal(new[] { "SundayI", "SundayII" }, cut.FindAll("#hour-compline-form option").Select(option => option.GetAttribute("value")));
        cut.Find(".hourplayer__head").Click();
        Assert.NotEmpty(cut.FindAll(".hourplayer__step.is-done"));
        cut.Find("#hour-compline-form").Change(chosenForm);

        Assert.Equal(chosenForm, cut.Find("#hour-compline-form").GetAttribute("value"));
        Assert.Contains(chosenReading, cut.Markup);
        Assert.DoesNotContain(defaultReading, cut.Markup);
        Assert.Equal(title, cut.Find("h1").TextContent);
        Assert.Equal(metadata, cut.Find(".hourplayer__meta").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__step.is-done"));
        Assert.Empty(cut.FindAll(".hourplayer__done"));

        Services.GetRequiredService<ICalendarPreference>().Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.Equal(defaultForm, cut.Find("#hour-compline-form").GetAttribute("value")));
        cut.Find("#hour-compline-form").Change(chosenForm);
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "morning"));
        Assert.Empty(cut.FindAll("#hour-compline-form"));
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "night"));
        Assert.Equal(defaultForm, cut.Find("#hour-compline-form").GetAttribute("value"));

        cut.Find("#hour-compline-form").Change(chosenForm);
        cut.Find("#hour-service-date").Change("2027-03-30");
        Assert.Equal("SundayII", cut.Find("#hour-compline-form").GetAttribute("value"));
        cut.Find("#hour-service-date").Change("2027-03-27");
        Assert.Empty(cut.FindAll("#hour-compline-form"));
        Assert.Contains("Easter Vigil", cut.Find(".hourplayer__participation").TextContent);
    }

    [Theory]
    [InlineData("SundayI", "Psalm 4", "Psalm 134")]
    [InlineData("SundayII", "Psalm 91", "Psalm 91")]
    public void Weekday_Compline_psalm_choice_keeps_reading_and_prayer_and_resets_with_context(string choice, string firstPsalm, string lastPsalm)
    {
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));
        var office = Services.GetRequiredService<ILiturgyOfTheHours>().Compose(new(2026, 11, 11), Hour.NightPrayer);
        var reading = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation;
        var prayer = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Prayer).Text!;

        Assert.Equal("Weekday", cut.Find("#hour-compline-psalmody").GetAttribute("value"));
        Assert.Empty(cut.FindAll("#hour-compline-form"));
        cut.Find(".hourplayer__head").Click();
        Assert.NotEmpty(cut.FindAll(".hourplayer__step.is-done"));
        cut.Find("#hour-compline-psalmody").Change(choice);

        Assert.Equal(choice, cut.Find("#hour-compline-psalmody").GetAttribute("value"));
        Assert.Contains(firstPsalm, cut.Markup);
        Assert.Contains(lastPsalm, cut.Markup);
        Assert.Contains(reading, cut.Markup);
        Assert.Contains(prayer, cut.Find(".hourplayer").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__step.is-done"));
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        cut.Find("#hour-compline-psalmody").Change("Weekday");
        Assert.Contains("Psalm 31:1-6", cut.Markup);
        Assert.Contains("Psalm 130", cut.Markup);

        cut.Find("#hour-compline-psalmody").Change(choice);
        Services.GetRequiredService<ICalendarPreference>().Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.Equal("Weekday", cut.Find("#hour-compline-psalmody").GetAttribute("value")));
        cut.Find("#hour-compline-psalmody").Change(choice);
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "morning"));
        Assert.Empty(cut.FindAll("#hour-compline-psalmody"));
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "night"));
        Assert.Equal("Weekday", cut.Find("#hour-compline-psalmody").GetAttribute("value"));
        cut.Find("#hour-compline-psalmody").Change(choice);
        cut.Find("#hour-service-date").Change("2027-04-06");
        Assert.Equal("Weekday", cut.Find("#hour-compline-psalmody").GetAttribute("value"));
        cut.Find("#hour-service-date").Change("2027-03-29");
        Assert.Empty(cut.FindAll("#hour-compline-psalmody"));
        Assert.Single(cut.FindAll("#hour-compline-form"));
        cut.Find("#hour-service-date").Change("2027-03-27");
        Assert.Empty(cut.FindAll("#hour-compline-psalmody"));
        cut.Find(".hourplayer__participation input").Change(true);
        Assert.Empty(cut.FindAll("#hour-compline-psalmody"));
        Assert.Single(cut.FindAll(".hourplayer__omission"));
    }

    [Theory]
    [InlineData(2026, 11, 11)]
    [InlineData(2027, 2, 15)]
    [InlineData(2027, 3, 28)]
    public async Task Lauds_can_omit_Invitatory_psalm_without_changing_its_opening_verse_or_other_Hours(int year, int month, int day)
    {
        Services.AddSingleton<Func<DateOnly>>(() => new(year, month, day));
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var settings = Services.GetRequiredService<IHourPlayerSettingsService>();
        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = InvitatoryMode.OmitPsalm });
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "morning"));
        var stepCount = cut.FindAll(".hourplayer__head").Count;

        Assert.Contains("Lord, open my lips.", cut.Find(".hourplayer__text").TextContent);
        Assert.DoesNotContain("O God, come to my assistance", cut.Find(".hourplayer__text").TextContent);
        Assert.Contains("GILH 35", cut.Markup);
        Assert.Empty(cut.FindAll(".hourplayer__option"));
        Assert.Empty(cut.FindAll(".hourplayer__picker"));
        Assert.DoesNotContain("Sample antiphon", cut.Find(".hourplayer__content").TextContent);
        Assert.DoesNotContain("was already prayed", cut.Find(".hourplayer__content").TextContent);
        await settings.DidNotReceive().UpdateAsync(Arg.Any<Func<HourPlayerSettings, HourPlayerSettings>>());

        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = InvitatoryMode.ShowAll });
        settings.Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".hourplayer__option")));
        Assert.Equal(4, cut.FindAll(".hourplayer__picker-btn").Count);
        Assert.Contains("Lord, open my lips.", cut.Find(".hourplayer__text").TextContent);
        Assert.Equal(stepCount, cut.FindAll(".hourplayer__head").Count);

        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = InvitatoryMode.None });
        settings.Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.Contains("O God, come to my assistance", cut.Find(".hourplayer__text").TextContent));
        Assert.Empty(cut.FindAll(".hourplayer__option"));
        Assert.DoesNotContain("Lord, open my lips.", cut.Find(".hourplayer__text").TextContent);

        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = InvitatoryMode.OmitPsalm });
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "evening"));
        Assert.Contains("O God, come to my assistance", cut.Find(".hourplayer__text").TextContent);
        Assert.DoesNotContain("GILH 35", cut.Markup);
        cut.SetParametersAndRender(p => p.Add(c => c.Slug, "night"));
        Assert.Contains("O God, come to my assistance", cut.Find(".hourplayer__text").TextContent);
        Assert.DoesNotContain("GILH 35", cut.Markup);
    }

    [Theory]
    [InlineData(InvitatoryMode.ShowAll)]
    [InlineData(InvitatoryMode.Specific)]
    [InlineData(InvitatoryMode.Random)]
    [InlineData(InvitatoryMode.Cycle)]
    [InlineData(InvitatoryMode.None)]
    [InlineData(InvitatoryMode.OmitPsalm)]
    public async Task Marian_choice_is_independent_of_Invitatory_preferences_and_resets_on_date_changes(InvitatoryMode mode)
    {
        Services.AddRespondeoLiturgy();
        Services.AddRespondeoHours();
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));
        var settings = Services.GetRequiredService<IHourPlayerSettingsService>();
        settings.Current.Returns(new HourPlayerSettings { ExpandAll = true, InvitatoryMode = mode, InvitatoryPsalm = "Psalm 95" });
        var cut = RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "night"));
        var picker = cut.Find("[aria-label='Choose the Marian antiphon']");

        Assert.Equal(4, picker.QuerySelectorAll("button").Length);
        Assert.Equal("Salve Regina", picker.QuerySelector("[aria-pressed=true]")!.TextContent);
        Assert.Equal(OfficeData.Text("salve-regina"), cut.Find(".hourplayer__option .hourplayer__text").TextContent);
        Assert.DoesNotContain("GILH 35", cut.Markup);
        await settings.DidNotReceive().UpdateAsync(Arg.Any<Func<HourPlayerSettings, HourPlayerSettings>>());

        var stepCount = cut.FindAll(".hourplayer__head").Count;
        for (var index = 0; index < stepCount; index++)
        {
            cut.FindAll(".hourplayer__head").ElementAt(index).Click();
        }
        Assert.Single(cut.FindAll(".hourplayer__done"));
        cut.Find("[aria-label='Choose the Marian antiphon'] [aria-pressed=true]").Click();
        Assert.Single(cut.FindAll(".hourplayer__done"));
        cut.FindAll("[aria-label='Choose the Marian antiphon'] button").Single(button => button.TextContent == "Ave Regina Caelorum").Click();
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        Assert.Equal(OfficeData.Text("ave-regina-caelorum"), cut.Find(".hourplayer__option .hourplayer__text").TextContent);
        Assert.Equal("Ave Regina Caelorum", cut.Find("[aria-label='Choose the Marian antiphon'] [aria-pressed=true]").TextContent);
        cut.FindAll(".hourplayer__head").Last().Click();
        Assert.Single(cut.FindAll(".hourplayer__done"));

        Services.GetRequiredService<ICalendarPreference>().Changed += Raise.Event<Action>();
        cut.WaitForAssertion(() => Assert.Equal("Salve Regina", cut.Find(".hourplayer__picker [aria-pressed=true]").TextContent));
        Assert.Empty(cut.FindAll(".hourplayer__done"));
        cut.Find("#hour-service-date").Change("2026-12-15");
        Assert.Equal("Alma Redemptoris Mater", cut.Find(".hourplayer__picker [aria-pressed=true]").TextContent);
        cut.Find("#hour-service-date").Change("2027-03-28");
        Assert.Empty(cut.FindAll(".hourplayer__picker"));
        Assert.Empty(cut.FindAll(".hourplayer__option"));
        Assert.Contains("Regina Caeli", cut.Markup);
        cut.Find("#hour-service-date").Change("2027-05-17");
        Assert.Equal("Salve Regina", cut.Find(".hourplayer__picker [aria-pressed=true]").TextContent);
        cut.Find("#hour-service-date").Change("2027-03-27");
        cut.Find(".hourplayer__participation input").Change(true);
        Assert.Empty(cut.FindAll(".hourplayer__picker"));
        Assert.Single(cut.FindAll(".hourplayer__omission"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArrangedReading_RendersItsExcerptLabelOrAnExplicitMissingTextNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("Hebrews 13:7-8").Returns("Synthetic first two verses");
        }
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading")
        {
            Reference = new("Hebrews 13:7-9a"),
            Passage = OfficeData.Texts.ReadingPassages["Hebrews 13:7-9a"],
        };

        var cut = Render(step, bible);

        Assert.Contains("Hebrews 13:7-9a", cut.Markup);
        if (available)
        {
            cut.WaitForAssertion(() => Assert.Contains("Synthetic first two verses", cut.Find(".hourplayer__text").TextContent));
            Assert.Contains("With various & strange doctrines be not led away.", cut.Find(".hourplayer__text").TextContent);
            Assert.Contains("verse 9 ends after its first sentence", cut.Find(".hourplayer__translation").TextContent);
            Assert.DoesNotContain(HourPassageLoader.GloryBe, cut.Markup);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.DoesNotContain("With various", cut.Markup);
        }
    }

    [Theory]
    [InlineData("1 Peter 5:8-9a", true)]
    [InlineData("1 Peter 5:8-9a", false)]
    [InlineData("Jeremiah 14:9a", true)]
    public void ComplineExcerpt_RendersOnlyItsAssembledBodyAndTranslationOrMissingNotice(string citation, bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("1 Peter 5:8").Returns("Synthetic first verse");
        }
        var passage = OfficeData.Texts.ReadingPassages[citation];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new(citation), Passage = passage };

        var cut = Render(step, bible);

        Assert.Contains(citation, cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            var closingClause = passage.Parts.Last().Text!;
            var expected = citation.StartsWith("1 Peter", StringComparison.Ordinal) ? "Synthetic first verse\n\n" + closingClause : closingClause;
            cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void JobExcerpt_ShowsItsExactBodyAndLabelOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("Job 1:21").Returns("Synthetic first Job verse");
        }
        var passage = OfficeData.Texts.ReadingPassages["Job 1:21; 2:10b"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("Job 1:21; 2:10b"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("Job 1:21; 2:10b", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string question = "If we have received good things of the hand of God, evil things why should we not receive?";
            cut.WaitForAssertion(() => Assert.Equal("Synthetic first Job verse\n\n" + question, cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeterExcerpt_ShowsItsExactBodyAndLabelOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("2 Peter 3:13-14").Returns("Synthetic first two Peter verses");
        }
        var passage = OfficeData.Texts.ReadingPassages["2 Peter 3:13-15a"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("2 Peter 3:13-15a"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("2 Peter 3:13-15a", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string clause = "And the longanimity of our Lord, do ye account salvation.";
            cut.WaitForAssertion(() => Assert.Equal("Synthetic first two Peter verses\n\n" + clause, cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EzekielExcerpt_ShowsItsProclamationOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("Ezekiel 37:13-14").Returns("Synthetic remaining Ezekiel verses");
        }
        var passage = OfficeData.Texts.ReadingPassages["Ezekiel 37:12b-14"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("Ezekiel 37:12b-14"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("Ezekiel 37:12b-14", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string opening = "Thus saith our Lord God: Behold I will open your graves, and will bring you out of your sepulchres o my people: "
                + "and will bring you into the land of Israel.";
            cut.WaitForAssertion(() => Assert.Equal(opening + "\n\nSynthetic remaining Ezekiel verses", cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VespersPeterExcerpt_ShowsItsHumilityOpeningOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("1 Peter 5:6-7").Returns("Synthetic remaining Peter verses");
        }
        var passage = OfficeData.Texts.ReadingPassages["1 Peter 5:5b-7"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("1 Peter 5:5b-7"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("1 Peter 5:5b-7", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string opening = "And do ye all insinuate humility one to another, because God resisteth the proud, and to the humble he giveth grace.";
            cut.WaitForAssertion(() => Assert.Equal(opening + "\n\nSynthetic remaining Peter verses", cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CorinthiansExcerpt_ShowsItsExactEndingOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("1 Corinthians 2:7-9").Returns("Synthetic first Corinthians verses");
        }
        var passage = OfficeData.Texts.ReadingPassages["1 Corinthians 2:7-10a"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("1 Corinthians 2:7-10a"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("1 Corinthians 2:7-10a", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string body = "Synthetic first Corinthians verses\n\nBut to us God hath revealed by his Spirit.";
            cut.WaitForAssertion(() => Assert.Equal(body, cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Theory]
    [MemberData(nameof(Respondeo.UnitTests.LiturgyOfTheHours.HourPassageLoaderTests.UnresolvedPartialReadings),
        MemberType = typeof(Respondeo.UnitTests.LiturgyOfTheHours.HourPassageLoaderTests))]
    public void UnresolvedPartialReading_ShowsWholeVersesWithTheAssignedReference(string citation)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync(citation).Returns("Synthetic whole-verse body");
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new(citation) };

        var cut = Render(step, bible);

        Assert.Equal(citation, cut.Find(".hourplayer__ref").TextContent);
        cut.WaitForAssertion(() => Assert.Equal("Synthetic whole-verse body", cut.Find(".hourplayer__text").TextContent));
        Assert.Equal("Douay-Rheims translation", cut.Find(".hourplayer__translation").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__official"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ColossiansExcerpt_ShowsItsExactOpeningOrMissingSourceNotice(bool available)
    {
        var bible = Substitute.For<IBibleText>();
        if (available)
        {
            bible.GetPassageAsync("Colossians 1:10-11").Returns("Synthetic remaining Colossians verses");
        }
        var passage = OfficeData.Texts.ReadingPassages["Colossians 1:9b-11"];
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading") { Reference = new("Colossians 1:9b-11"), Passage = passage };

        var cut = Render(step, bible);

        Assert.Equal("Colossians 1:9b-11", cut.Find(".hourplayer__ref").TextContent);
        if (available)
        {
            const string opening = "That you may be filled with the knowledge of his will, in all wisdom, and spiritual understanding:";
            cut.WaitForAssertion(() => Assert.Equal(opening + "\n\nSynthetic remaining Colossians verses", cut.Find(".hourplayer__text").TextContent));
            Assert.Equal(passage.Translation, cut.Find(".hourplayer__translation").TextContent);
            Assert.Empty(cut.FindAll(".hourplayer__official"));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__text"));
            Assert.Equal("Read today's text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        }
    }

    [Fact]
    public void UnarrangedPartialReading_WithLicensedBodyShowsTheBodyWithoutAMissingNotice()
    {
        var step = new OfficeStep(OfficeStepKind.Reading, "Reading")
        {
            Reference = new("Romans 5:20b-21"),
            Text = "Synthetic authorized proper excerpt",
            IsLicensedText = true,
        };

        var cut = Render(step, Substitute.For<IBibleText>());

        Assert.Equal(step.Text, cut.Find(".hourplayer__text").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__official"));
    }

    [Theory]
    [MemberData(nameof(Respondeo.UnitTests.LiturgyOfTheHours.HourPassageLoaderTests.UnresolvedPartialPsalmody),
        MemberType = typeof(Respondeo.UnitTests.LiturgyOfTheHours.HourPassageLoaderTests))]
    public void UnresolvedPartialPsalmody_ShowsWholeVersesWithItsAntiphonAndEnding(string citation)
    {
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync(citation).Returns("Synthetic whole canticle");
        var step = new OfficeStep(OfficeStepKind.Canticle, $"Canticle: {citation}") { Reference = new(citation), Antiphon = "Synthetic prescribed antiphon" };

        var cut = Render(step, bible);

        Assert.Equal($"Canticle: {citation}", cut.Find(".hourplayer__step-title").TextContent);
        cut.WaitForAssertion(() => Assert.Equal("Synthetic whole canticle\n\n" + HourPassageLoader.GloryBe, cut.Find(".hourplayer__text").TextContent));
        Assert.Equal("Douay-Rheims translation", cut.Find(".hourplayer__translation").TextContent);
        Assert.Equal(2, cut.FindAll(".hourplayer__antiphon").Count);
        Assert.Empty(cut.FindAll(".hourplayer__official"));
    }

    [Fact]
    public void Unresolved_equal_precedence_allows_selecting_the_Office_celebration()
    {
        var date = new DateOnly(2057, 6, 29);
        Services.AddSingleton<Func<DateOnly>>(() => date);
        var selected = new LiturgicalCelebration("First celebration", CelebrationRank.Solemnity, LiturgicalColor.White)
        {
            CalendarKey = "first-celebration",
        };
        var contender = new LiturgicalCelebration("Second celebration", CelebrationRank.Solemnity, LiturgicalColor.White)
        {
            CalendarKey = "second-celebration",
        };
        var day = new LiturgicalDay(date, LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, selected)
        {
            Occurrence = new(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, [])
            {
                ContendingCelebrations = [selected, contender],
            },
        };
        var office = new Office(date, Hour.MorningPrayer, "Morning Prayer", day, 4, []);
        string? composedCelebrationKey = null;
        var engine = Substitute.For<ILiturgyOfTheHours>();
        engine.Compose(Arg.Any<DateOnly>(), Arg.Any<Hour>(), Arg.Any<OfficeForm>(), Arg.Any<LiturgicalParticipation>(),
            Arg.Any<ComplineSundayForm>(), Arg.Any<ComplinePsalmodyChoice>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(call =>
            {
                composedCelebrationKey = call.ArgAt<string?>(8);
                return office;
            });
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));

        var cut = RenderComponent<HourPlayer>(parameters => parameters.Add(component => component.Slug, "morning"));
        var warning = cut.Find(".hourplayer__calendar-warning").TextContent;

        Assert.Contains("do not determine", warning);
        Assert.Contains("does not resolve or change", warning);
        var candidates = cut.FindAll(".hourplayer__calendar-candidates li").Select(item => item.TextContent).ToList();
        Assert.Equal(2, candidates.Count);
        Assert.Contains(day.Occurrence!.ContendingCelebrations[0].Name, candidates[0]);
        Assert.Contains(day.Occurrence.ContendingCelebrations[1].Name, candidates[1]);
        cut.Find("#hour-calendar-celebration").Change("second-celebration");
        Assert.Equal("second-celebration", composedCelebrationKey);
        cut.Find("#hour-service-date").Change("2057-06-30");
        Assert.Null(composedCelebrationKey);
        cut.SetParametersAndRender(parameters => parameters.Add(component => component.Slug, "evening"));
        Assert.Null(composedCelebrationKey);
        Assert.Empty(day.Occurrence.SuppressedCelebrations);
        Assert.Equal(2, day.Occurrence.ContendingCelebrations.Count);
    }

    [Fact]
    public void Restricted_memorial_picker_passes_the_stable_calendar_key_to_Hours()
    {
        var date = new DateOnly(2026, 11, 11);
        const string calendarKey = "saints-perpetua-and-felicity-martyrs";
        var memorial = new LiturgicalCelebration("Saints Perpetua and Felicity, Martyrs",
            CelebrationRank.Memorial, LiturgicalColor.Violet) { CalendarKey = calendarKey };
        var day = new LiturgicalDay(date, LiturgicalSeason.Lent, LiturgicalColor.Violet, null)
        {
            MemorialCommemorations = [memorial],
        };
        var office = new Office(date, Hour.MorningPrayer, "Morning Prayer", day, 4, []);
        var engine = Substitute.For<ILiturgyOfTheHours>();
        engine.Compose(Arg.Any<DateOnly>(), Arg.Any<Hour>(), Arg.Any<OfficeForm>(), Arg.Any<LiturgicalParticipation>(),
            Arg.Any<ComplineSundayForm>(), Arg.Any<ComplinePsalmodyChoice>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(call => office with { SelectedMemorialCommemorationCalendarKey = call.ArgAt<string?>(7) });
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));

        var cut = RenderComponent<HourPlayer>(parameters => parameters.Add(component => component.Slug, "morning"));

        Assert.Equal(2, cut.FindAll("#hour-memorial-commemoration option").Count);
        cut.Find("#hour-memorial-commemoration").Change(calendarKey);
        Assert.Equal(calendarKey, cut.Find("#hour-memorial-commemoration").GetAttribute("value"));
        engine.Received().Compose(date, Hour.MorningPrayer, memorialCommemorationCalendarKey: calendarKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProviderPreference_ControlsSampleBodiesLabelsAndAcknowledgments(bool enabled)
    {
        var date = new DateOnly(2026, 11, 11);
        var office = new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), new EmbeddedHoursTextProvider())
            .Compose(date, Hour.MorningPrayer, preferOfficialText: enabled);
        var engine = Substitute.For<ILiturgyOfTheHours>();
        engine.Compose(date, Hour.MorningPrayer).Returns(office);
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));

        var cut = RenderComponent<HourPlayer>(parameters => parameters.Add(component => component.Slug, "morning"));

        if (enabled)
        {
            Assert.Contains("not approved", cut.Find(".hourplayer__sample").TextContent);
            Assert.Contains(cut.FindAll(".hourplayer__text"), element => element.TextContent.Contains("Sample hymn", StringComparison.Ordinal));
            Assert.NotEmpty(cut.FindAll(".hourplayer__provider-sample"));
            Assert.Equal(2, cut.FindAll(".hourplayer__ack").Count);
            Assert.All(cut.FindAll(".hourplayer__ack"), element => Assert.Equal(office.Acknowledgment, element.TextContent));
        }
        else
        {
            Assert.Empty(cut.FindAll(".hourplayer__sample"));
            Assert.Empty(cut.FindAll(".hourplayer__provider-sample"));
            Assert.Empty(cut.FindAll(".hourplayer__ack"));
            Assert.DoesNotContain("Sample hymn", cut.Markup);
            Assert.NotEmpty(cut.FindAll(".hourplayer__official"));
        }
    }

    [Fact]
    public void SampleContentOnlyInAnOption_StillDisplaysItsAcknowledgment()
    {
        var date = new DateOnly(2026, 11, 11);
        var option = new OfficeStep(OfficeStepKind.Hymn, "Synthetic option") { Text = "Invented sample body", IsSampleText = true };
        var office = new Office(date, Hour.MorningPrayer, "Synthetic office",
            new(date, LiturgicalSeason.OrdinaryTime, LiturgicalColor.Green, null), 4,
            [new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option] }])
        {
            Acknowledgment = "Invented sample only; not approved.",
            IsSampleContent = true,
        };
        var engine = Substitute.For<ILiturgyOfTheHours>();
        engine.Compose(date, Hour.MorningPrayer).Returns(office);
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(Substitute.For<IBibleText>()));

        var cut = RenderComponent<HourPlayer>(parameters => parameters.Add(component => component.Slug, "morning"));

        Assert.Equal(2, cut.FindAll(".hourplayer__ack").Count);
        Assert.All(cut.FindAll(".hourplayer__ack"), element => Assert.Equal(office.Acknowledgment, element.TextContent));
        Assert.Equal("Includes sample provider content; not approved liturgical text.", cut.Find(".hourplayer__provider-sample").TextContent);
        Assert.Equal(option.Text, cut.Find(".hourplayer__text").TextContent);
    }

    private IRenderedComponent<HourPlayer> Render(OfficeStep step, IBibleText bible)
    {
        var date = new DateOnly(2026, 11, 11);
        var office = new Office(date, Hour.MorningPrayer, "Synthetic test office",
            new(date, LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, null), 4, [step]);
        var engine = Substitute.For<ILiturgyOfTheHours>();
        engine.Compose(date, Hour.MorningPrayer).Returns(office);
        Services.AddSingleton(engine);
        Services.AddSingleton(new HourPassageLoader(bible));
        return RenderComponent<HourPlayer>(p => p.Add(c => c.Slug, "morning"));
    }
}
