using Bunit;
using Respondeo.Components;
using Respondeo.Content.Contracts;

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

    private static InquiryNode Node(string id, string title, string summary = "") => new() { Id = id, Title = title, Summary = summary, BodyHtml = string.Empty };

    private static IReadOnlyList<StageReel.ReelStage> ThreeStages() =>
    [
        new("why-god", Node("why-god", "Why God?", "Does God exist?")),
        new("why-jesus", Node("why-jesus", "Why Jesus?", "Who is Jesus?")),
        new("why-the-church", Node("why-the-church", "Why the Church?", "Which church?")),
    ];

    private IRenderedComponent<StageReel> Render(IReadOnlyList<StageReel.ReelStage> stages) => RenderComponent<StageReel>(p => p.Add(c => c.Stages, stages));

    [Fact]
    public void Renders_a_question_for_each_stage()
    {
        var cut = Render(ThreeStages());

        Assert.Equal(3, cut.FindAll("a.reel__q").Count);
    }

    [Fact]
    public void Renders_stages_in_forward_dom_order_so_stage_one_sits_at_the_start()
    {
        var cut = Render(ThreeStages());

        // The reel walks left-to-right: Stage 1 is the FIRST question in the DOM, the final stage sits LAST.
        var questions = cut.FindAll(".reel__q-text").Select(l => l.TextContent).ToList();
        Assert.Contains("God exists", questions[0]);
        Assert.Contains("Jesus", questions[1]);
        Assert.Contains("Church", questions[2]);
    }

    [Fact]
    public void Links_each_question_to_its_stage_slug()
    {
        var cut = Render(ThreeStages());

        var hrefs = cut.FindAll("a.reel__q").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("why-god", hrefs);
        Assert.Contains("why-jesus", hrefs);
        Assert.Contains("why-the-church", hrefs);
    }

    [Fact]
    public void Renders_both_scroll_hint_controls_and_the_dot_rail()
    {
        var cut = Render(ThreeStages());

        Assert.NotNull(cut.Find(".reel__hint--prev"));
        Assert.NotNull(cut.Find(".reel__hint--next"));
        Assert.NotNull(cut.Find(".reel-dots"));
    }

    [Fact]
    public void Marks_the_reel_as_a_focusable_list_of_items()
    {
        var cut = Render(ThreeStages());

        var reel = cut.Find(".reel");
        Assert.Equal("list", reel.GetAttribute("role"));
        Assert.Equal("0", reel.GetAttribute("tabindex"));
        Assert.Equal(3, cut.FindAll(".reel__step[role=listitem]").Count);
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

    [Fact]
    public void Previous_hint_steps_one_stage_back()
    {
        var cut = Render(ThreeStages());

        cut.Find(".reel__hint--prev").Click();

        var step = Assert.Single(JSInterop.Invocations, i => i.Identifier == "scrollByStep");
        Assert.Equal(-1, step.Arguments[1]);
    }

    [Fact]
    public void Next_hint_steps_one_stage_forward()
    {
        var cut = Render(ThreeStages());

        cut.Find(".reel__hint--next").Click();

        var step = Assert.Single(JSInterop.Invocations, i => i.Identifier == "scrollByStep");
        Assert.Equal(1, step.Arguments[1]);
    }
}
