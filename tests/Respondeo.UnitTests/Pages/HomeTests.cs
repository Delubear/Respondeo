using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Shared;
using Respondeo.UnitTests.TestSupport;
using Respondeo.Content.Inquiry;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class HomeTests : TestContext
{
    private const string Manifest = "{\"files\":[\"why-god/why-god.md\",\"why-jesus/why-jesus.md\"]}";

    // Two stage landing nodes whose ids match SiteNavigation slugs.
    private const string WhyGodMd = "---\nid: why-god\ntitle: Why God?\nsummary: Start here\n---\nWelcome";
    private const string WhyJesusMd = "---\nid: why-jesus\ntitle: Why Jesus?\n---\nMore";

    public HomeTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content/inquiry/manifest.json"] = Manifest,
            ["_content/Respondeo.Content/inquiry/why-god/why-god.md"] = WhyGodMd,
            ["_content/Respondeo.Content/inquiry/why-jesus/why-jesus.md"] = WhyJesusMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IContentService>(new InquiryService(http, new InquiryParser(ContentRendering.Renderer)));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(Substitute.For<IVisitedNodes>());

        // Home imports the reel JS module in OnAfterRenderAsync and calls init on it;
        // let bUnit handle the import and the module's method invocations.
        var reelModule = JSInterop.SetupModule("./js/reel.js");
        reelModule.SetupVoid("init", _ => true);
        reelModule.SetupVoid("dispose", _ => true);
    }

    [Fact]
    public void Renders_a_question_for_each_stage()
    {
        var cut = RenderComponent<Home>();

        var questions = cut.FindAll("a.reel__q");
        Assert.Equal(2, questions.Count);
    }

    [Fact]
    public void Stage_questions_link_to_their_stage_page()
    {
        var cut = RenderComponent<Home>();

        var hrefs = cut.FindAll("a.reel__q").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("why-god", hrefs);
        Assert.Contains("why-jesus", hrefs);
    }

    [Fact]
    public void Clears_the_breadcrumb_trail_on_load()
    {
        var trail = Services.GetRequiredService<IBreadcrumbTrail>();

        RenderComponent<Home>();

        trail.Received().ClearAsync();
    }

    [Fact]
    public void Always_renders_the_hero_heading()
    {
        var cut = RenderComponent<Home>();

        Assert.Equal("One question leads to the next", cut.Find("h1.pillar__title").TextContent);
    }
}
