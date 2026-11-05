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
    public void Points_to_official_source_when_no_text_is_available()
    {
        var cut = Render(Step() with { UseOfficialText = true });

        Assert.Equal("Read the text from an official source below.", cut.Find(".hourplayer__official").TextContent);
        Assert.Empty(cut.FindAll(".hourplayer__text"));
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
