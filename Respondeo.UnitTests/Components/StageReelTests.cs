using Bunit;
using Respondeo.Components;
using Respondeo.Content.Abstractions;

namespace Respondeo.UnitTests.Components;

public class StageReelTests : TestContext
{
    public StageReelTests()
    {
        // StageReel imports ./js/reel.js on first render and calls init; dispose on teardown.
        var reelModule = JSInterop.SetupModule("./js/reel.js");
        reelModule.SetupVoid("init", _ => true);
        reelModule.SetupVoid("dispose", _ => true);
        reelModule.SetupVoid("scrollByStep", _ => true);
    }

    private static ContentNode Node(string id, string title, string summary = "") =>
        new() { Id = id, Title = title, Summary = summary, BodyHtml = string.Empty };

    private static IReadOnlyList<StageReel.ReelStage> ThreeStages() =>
    [
        new("why-god", Node("why-god", "Why God?", "Does God exist?")),
        new("why-jesus", Node("why-jesus", "Why Jesus?", "Who is Jesus?")),
        new("why-the-church", Node("why-the-church", "Why the Church?", "Which church?")),
    ];

    private IRenderedComponent<StageReel> Render(IReadOnlyList<StageReel.ReelStage> stages) =>
        RenderComponent<StageReel>(p => p.Add(c => c.Stages, stages));

    [Fact]
    public void Renders_a_card_for_each_stage()
    {
        var cut = Render(ThreeStages());

        Assert.Equal(3, cut.FindAll("a.stage-card").Count);
    }

    [Fact]
    public void Renders_stages_in_reverse_dom_order_so_stage_one_sits_at_the_bottom()
    {
        var cut = Render(ThreeStages());

        // The reel climbs upward: Stage 1 is the LAST card in the DOM, the final stage sits FIRST.
        var labels = cut.FindAll(".stage-card__label").Select(l => l.TextContent).ToList();
        Assert.Contains("Why the Church?", labels[0]);
        Assert.Contains("Why Jesus?", labels[1]);
        Assert.Contains("Why God?", labels[2]);
    }

    [Fact]
    public void Numbers_the_eyebrows_in_natural_order_despite_reversed_dom()
    {
        var cut = Render(ThreeStages());

        var eyebrows = cut.FindAll(".stage-card__eyebrow").Select(e => e.TextContent.Trim()).ToList();
        // DOM is top-to-bottom reversed, so the numbering counts down as you read the markup.
        Assert.Equal("Stage 3 of 3", eyebrows[0]);
        Assert.Equal("Stage 2 of 3", eyebrows[1]);
        Assert.Equal("Stage 1 of 3", eyebrows[2]);
    }

    [Fact]
    public void Links_each_card_to_its_stage_slug()
    {
        var cut = Render(ThreeStages());

        var hrefs = cut.FindAll("a.stage-card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("why-god", hrefs);
        Assert.Contains("why-jesus", hrefs);
        Assert.Contains("why-the-church", hrefs);
    }

    [Fact]
    public void Renders_both_scroll_hint_controls_and_the_dot_rail()
    {
        var cut = Render(ThreeStages());

        Assert.NotNull(cut.Find(".reel__hint--up"));
        Assert.NotNull(cut.Find(".reel__hint--down"));
        Assert.NotNull(cut.Find(".reel-dots"));
    }

    [Fact]
    public void Marks_the_reel_as_a_focusable_listbox_of_options()
    {
        var cut = Render(ThreeStages());

        var reel = cut.Find(".reel");
        Assert.Equal("listbox", reel.GetAttribute("role"));
        Assert.Equal("0", reel.GetAttribute("tabindex"));
        Assert.Equal(3, cut.FindAll(".reel__step[role=option]").Count);
    }

    [Fact]
    public void Initializes_the_reel_js_module_on_render()
    {
        Render(ThreeStages());

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "init");
    }

    [Fact]
    public void Disposes_the_reel_js_module_on_teardown()
    {
        var cut = Render(ThreeStages());

        DisposeComponents();

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "dispose");
    }
}
