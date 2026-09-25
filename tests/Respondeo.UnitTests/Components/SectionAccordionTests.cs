using System.Net;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.Components;
using Respondeo.UnitTests.TestSupport;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Components;

public class SectionAccordionTests : TestContext
{
    private const string Manifest = "{\"files\":[\"one.md\",\"two.md\"]}";
    private const string OneMd = "---\nid: one\ntitle: Section One\nsummary: The first summary\n---\nBody one";
    private const string TwoMd = "---\nid: two\ntitle: Section Two\n---\nBody two";

    private readonly FakeNavigationManager _nav;

    public SectionAccordionTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content.Markdown/content/one.md"] = OneMd,
            ["_content/Respondeo.Content.Markdown/content/two.md"] = TwoMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser(ContentRendering.Renderer)));
        _nav = Services.GetRequiredService<FakeNavigationManager>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<SectionAccordion> Render() =>
        RenderComponent<SectionAccordion>(p => p
            .Add(c => c.NodeId, "parent")
            .Add(c => c.SectionIds, new[] { "one", "two" }));

    // The accordion updates the address bar silently via the respondeoUrl.replace JS helper,
    // so URL assertions read the last argument passed to that interop call.
    private string LastUrlReplace()
    {
        var invocation = JSInterop.Invocations["respondeoUrl.replace"].LastOrDefault();
        return invocation.Arguments.Count > 0 ? invocation.Arguments[0]?.ToString() ?? string.Empty : string.Empty;
    }

    [Fact]
    public void Renders_a_collapsed_trigger_for_each_section()
    {
        var cut = Render();

        var triggers = cut.FindAll("button.accordion__trigger");
        Assert.Equal(2, triggers.Count);
        Assert.All(triggers, t => Assert.Equal("false", t.GetAttribute("aria-expanded")));
        Assert.Empty(cut.FindAll(".accordion__panel"));
    }

    [Fact]
    public void Renders_the_section_summary_when_present()
    {
        var cut = Render();

        var summaries = cut.FindAll(".accordion__summary").ToList();
        // Only the first section defines a summary; the second omits it.
        Assert.Single(summaries);
        Assert.Equal("The first summary", summaries[0].TextContent);
    }

    [Fact]
    public void Opening_a_section_shows_its_panel_and_updates_the_url()
    {
        var cut = Render();

        cut.FindAll("button.accordion__trigger").ToList()[0].Click();

        var panel = cut.Find(".accordion__panel");
        Assert.Contains("Body one", panel.TextContent);
        Assert.Contains("section=one", LastUrlReplace());
    }

    [Fact]
    public void Opening_a_second_section_closes_the_first()
    {
        var cut = Render();

        cut.FindAll("button.accordion__trigger").ToList()[0].Click();
        cut.FindAll("button.accordion__trigger").ToList()[1].Click();

        var panels = cut.FindAll(".accordion__panel");
        Assert.Single(panels);
        Assert.Contains("Body two", panels.ToList()[0].TextContent);
        Assert.Contains("section=two", LastUrlReplace());
    }

    [Fact]
    public void Clicking_an_open_section_closes_it_and_clears_the_url()
    {
        var cut = Render();

        var triggers = cut.FindAll("button.accordion__trigger").ToList();
        triggers[0].Click();
        cut.FindAll("button.accordion__trigger").ToList()[0].Click();

        Assert.Empty(cut.FindAll(".accordion__panel"));
        Assert.DoesNotContain("section=", LastUrlReplace());
    }

    [Fact]
    public void Deep_link_query_opens_the_matching_section()
    {
        _nav.NavigateTo("node/parent?section=two");

        var cut = Render();

        var panel = cut.Find(".accordion__panel");
        Assert.Contains("Body two", panel.TextContent);
        var openTrigger = cut.FindAll("button.accordion__trigger").ToList()[1];
        Assert.Equal("true", openTrigger.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Unknown_section_query_leaves_all_closed()
    {
        _nav.NavigateTo("node/parent?section=missing");

        var cut = Render();

        Assert.Empty(cut.FindAll(".accordion__panel"));
    }
}
