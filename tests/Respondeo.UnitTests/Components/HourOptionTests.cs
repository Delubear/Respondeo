using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class HourOptionTests : TestContext
{
    private readonly IHourPlayerSettingsService _settings = Substitute.For<IHourPlayerSettingsService>();

    public HourOptionTests()
    {
        _settings.Current.Returns(new HourPlayerSettings());
        Services.AddSingleton(_settings);
    }

    private static readonly RenderFragment<string> PlainText = t => b => b.AddContent(0, t);

    private IRenderedComponent<HourOption> Render(OfficeStep option, IReadOnlyList<string>? stanzas = null, string? passage = null) =>
        RenderComponent<HourOption>(p => p
            .Add(c => c.Option, option)
            .Add(c => c.RenderText, PlainText)
            .Add(c => c.Stanzas, stanzas)
            .Add(c => c.Passage, passage));

    private static OfficeStep Step(string title = "Psalm 95") => new(default, title);

    [Fact]
    public void Renders_title_and_vulgate_citation()
    {
        var cut = Render(Step() with { Reference = new PsalmodyReference("Ps 95", "Ps 94") });

        Assert.Contains("Psalm 95", cut.Find(".hourplayer__option-title").TextContent);
        Assert.Equal("(Vulgate: Ps 94)", cut.Find(".hourplayer__ref").TextContent);
    }

    [Fact]
    public void Omits_vulgate_citation_when_numbering_is_identical()
    {
        var cut = Render(Step() with { Reference = new PsalmodyReference("Ps 100") });

        Assert.Empty(cut.FindAll(".hourplayer__ref"));
    }

    [Fact]
    public void Renders_explanation_as_note()
    {
        var cut = Render(Step() with { Explanation = "Why this is prayed." });

        var note = cut.Find(".hourplayer__explain");
        Assert.Equal("note", note.GetAttribute("role"));
        Assert.Equal("Why this is prayed.", note.TextContent);
    }

    [Fact]
    public void Text_takes_precedence_over_stanzas_and_passage()
    {
        var cut = Render(Step() with { Text = "Inline text", Antiphon = "Ant text" }, ["Stanza"], "Passage");

        var text = Assert.Single(cut.FindAll(".hourplayer__text"));
        Assert.Equal("Inline text", text.TextContent);
        Assert.Single(cut.FindAll(".hourplayer__antiphon"));
        Assert.Empty(cut.FindAll(".hourplayer__translation"));
    }

    [Fact]
    public void Stanzas_repeat_antiphon_after_each_stanza_and_doxology()
    {
        var cut = Render(Step() with { Antiphon = "Come, let us worship" }, ["One", "Two"]);

        var texts = cut.FindAll(".hourplayer__text").Select(e => e.TextContent).ToList();
        Assert.Equal(["One", "Two", HourPassageLoader.GloryBe], texts);
        Assert.Equal(4, cut.FindAll(".hourplayer__antiphon").Count);
        Assert.Equal("Douay-Rheims translation", cut.Find(".hourplayer__translation").TextContent);
    }

    [Fact]
    public void Stanzas_show_antiphon_only_at_start_when_setting_is_on()
    {
        _settings.Current.Returns(new HourPlayerSettings { AntiphonAtStartOnly = true });

        var cut = Render(Step() with { Antiphon = "Come, let us worship" }, ["One", "Two"]);

        Assert.Single(cut.FindAll(".hourplayer__antiphon"));
        Assert.Equal(3, cut.FindAll(".hourplayer__text").Count);
    }

    [Fact]
    public void Renders_passage_with_translation_note()
    {
        var cut = Render(Step(), passage: "Passage text");

        Assert.Equal("Passage text", cut.Find(".hourplayer__text").TextContent);
        Assert.Equal("Douay-Rheims translation", cut.Find(".hourplayer__translation").TextContent);
    }

    [Fact]
    public void Arranged_passage_displays_its_unofficial_translation_label()
    {
        var option = Step() with { Passage = new([new(Text: "Synthetic text")], "Latin with an unofficial English translation") };

        var cut = Render(option, passage: "Assembled text");

        Assert.Equal("Assembled text", cut.Find(".hourplayer__text").TextContent);
        Assert.Equal("Latin with an unofficial English translation", cut.Find(".hourplayer__translation").TextContent);
    }

    [Fact]
    public void Points_to_official_source_when_no_text_is_available()
    {
        var cut = Render(Step() with { UseOfficialText = true });

        Assert.Equal("Read the text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__text"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unresolved_partial_reference_or_stanza_shows_an_official_source_notice(bool stanza)
    {
        var option = Step() with
        {
            Reference = new(stanza ? "Psalm 95" : "Isaiah 66:10-14a"),
            Stanzas = stanza ? [new("Psalm 95:1-7a")] : null,
        };

        var cut = Render(option);

        Assert.Equal("Read the text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__text"));
        Assert.Empty(cut.FindAll(".hourplayer__translation"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Loaded_partial_option_renders_whole_verses_without_a_missing_notice(bool stanza)
    {
        var bible = Substitute.For<Respondeo.Bible.Contracts.IBibleText>();
        const string citation = "Isaiah 66:10-14a";
        bible.GetPassageAsync(citation).Returns("Synthetic whole boundary verses");
        var option = new OfficeStep(OfficeStepKind.Canticle, citation)
        {
            Reference = new(citation),
            Stanzas = stanza ? [new(citation)] : null,
        };
        var date = new DateOnly(2026, 11, 11);
        var office = new Office(date, Hour.MorningPrayer, "Synthetic office",
            new(date, Respondeo.LiturgicalCalendar.LiturgicalSeason.OrdinaryTime, Respondeo.LiturgicalCalendar.LiturgicalColor.Green, null), 4,
            [new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option] }]);
        var loaded = await new HourPassageLoader(bible).LoadAsync(office);

        var cut = Render(option, loaded.OptionStanzas.GetValueOrDefault((0, 0)), loaded.OptionPassages.GetValueOrDefault((0, 0)));

        Assert.Equal(citation, cut.Find(".hourplayer__option-title").TextContent.Trim());
        Assert.Equal(stanza ? new[] { "Synthetic whole boundary verses", HourPassageLoader.GloryBe }
            : new[] { "Synthetic whole boundary verses\n\n" + HourPassageLoader.GloryBe },
            cut.FindAll(".hourplayer__text").Select(element => element.TextContent));
        Assert.Equal("Douay-Rheims translation", cut.Find(".hourplayer__translation").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__official"));
    }

    [Fact]
    public void Missing_arranged_option_shows_an_official_source_notice()
    {
        var cut = Render(Step() with { Passage = new([new(Citation: "Psalm 95")], "Synthetic source") });

        Assert.Equal("Read the text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__text"));
    }

    [Fact]
    public void Inline_partial_option_keeps_its_body_without_a_missing_notice()
    {
        var cut = Render(Step() with { Reference = new("Isaiah 66:10-14a"), Text = "Synthetic authorized canticle" });

        Assert.Equal("Synthetic authorized canticle", cut.Find(".hourplayer__text").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__official"));
    }

    [Fact]
    public void SampleAntiphon_IsLabeledSeparatelyFromTheBibleFallback()
    {
        var cut = Render(Step() with { Antiphon = "Invented sample antiphon", IsSampleText = true }, passage: "Synthetic Bible body");

        Assert.Equal("Synthetic Bible body", cut.Find(".hourplayer__text").TextContent);
        Assert.Equal("Includes sample provider content; not approved liturgical text.", cut.Find(".hourplayer__provider-sample").TextContent);
        Assert.Contains(cut.FindAll(".hourplayer__translation"), element => element.TextContent == "Douay-Rheims translation");
    }

    [Fact]
    public void MissingProviderAntiphon_IsExplicitEvenWithABibleBody()
    {
        var option = new OfficeStep(OfficeStepKind.Psalm, "Psalm 95") { Reference = new("Psalm 95"), UseOfficialText = true };

        var cut = Render(option, passage: "Synthetic Bible body");

        Assert.Equal("Synthetic Bible body", cut.Find(".hourplayer__text").TextContent);
        Assert.Equal("This psalmody's prescribed antiphon is not available here. Read it from an official source below.",
            cut.Find(".hourplayer__official[role=note]").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__antiphon"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_required_stanza_shows_a_notice_without_partial_text_or_doxology(bool missingFirst)
    {
        var bible = Substitute.For<Respondeo.Bible.Contracts.IBibleText>();
        bible.GetPassageAsync("Psalm 95:1-5").Returns(missingFirst ? null : "Synthetic first stanza");
        bible.GetPassageAsync("Psalm 95:6-11").Returns(missingFirst ? "Synthetic second stanza" : null);
        bible.GetPassageAsync("Psalm 95").Returns("Wrong full psalm");
        var option = new OfficeStep(OfficeStepKind.Psalm, "Psalm 95")
        {
            Reference = new("Psalm 95"),
            Stanzas = [new("Psalm 95:1-5"), new("Psalm 95:6-11")],
            Antiphon = "Synthetic antiphon",
        };
        var date = new DateOnly(2026, 11, 11);
        var office = new Office(date, Hour.MorningPrayer, "Synthetic Office",
            new(date, Respondeo.LiturgicalCalendar.LiturgicalSeason.OrdinaryTime, Respondeo.LiturgicalCalendar.LiturgicalColor.Green, null),
            4, [new OfficeStep(OfficeStepKind.Rubric, "Options") { Options = [option] }]);
        var loaded = await new HourPassageLoader(bible).LoadAsync(office);

        var cut = Render(option, loaded.OptionStanzas.GetValueOrDefault((0, 0)), loaded.OptionPassages.GetValueOrDefault((0, 0)));

        Assert.Equal("Read the text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__text"));
        Assert.Empty(cut.FindAll(".hourplayer__translation"));
        Assert.Equal("Ant. Synthetic antiphon", cut.Find(".hourplayer__antiphon").TextContent);
    }

    [Fact]
    public void Renders_no_body_when_nothing_applies()
    {
        var cut = Render(Step());

        Assert.Empty(cut.FindAll(".hourplayer__text"));
        Assert.Empty(cut.FindAll(".hourplayer__official"));
        Assert.Empty(cut.FindAll(".hourplayer__translation"));
    }
}
