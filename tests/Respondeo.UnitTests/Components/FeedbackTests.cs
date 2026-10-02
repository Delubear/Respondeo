using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.Components;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class FeedbackTests : TestContext
{
    private readonly FeatureFlags _features = new();

    public FeedbackTests()
    {
        // Feedback renders a Dialog, which drives the native <dialog> through DialogInterop/JS.
        Services.AddScoped<DialogInterop>();
        // The anonymous form listens for Tally's submit message via TallyInterop/JS.
        Services.AddScoped<TallyInterop>();
        // ExternalFeedback defaults to true, so the choice dialog is the default behavior under test.
        Services.AddSingleton(_features);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Feedback> Render(Action<ComponentParameterCollectionBuilder<Feedback>>? configure = null) =>
        RenderComponent<Feedback>(p => configure?.Invoke(p));

    [Fact]
    public void Trigger_uses_the_default_link_text()
    {
        var cut = Render();

        Assert.Equal("Report an issue or share feedback", cut.Find("button.feedback__trigger").TextContent);
    }

    [Fact]
    public void Trigger_uses_the_supplied_link_text()
    {
        var cut = Render(p => p.Add(c => c.LinkText, "let us know"));

        Assert.Equal("let us know", cut.Find("button.feedback__trigger").TextContent);
    }

    [Fact]
    public void Offers_exactly_two_feedback_choices()
    {
        var cut = Render();

        Assert.Equal(2, cut.FindAll(".feedback__choice").Count);
    }

    [Fact]
    public void One_choice_opens_a_github_issue()
    {
        var cut = Render();

        var github = cut.Find("a.feedback__choice");
        Assert.Equal("https://github.com/Delubear/Respondeo/issues/new/choose", github.GetAttribute("href"));
        Assert.Equal("_blank", github.GetAttribute("target"));
        Assert.Contains("noopener", github.GetAttribute("rel"));
    }

    [Fact]
    public void The_choice_screen_does_not_embed_the_form_until_chosen()
    {
        var cut = Render();

        Assert.Empty(cut.FindAll("iframe.feedback__frame"));
    }

    [Fact]
    public void Choosing_anonymous_feedback_embeds_the_hosted_form_inline()
    {
        var cut = Render();

        // The anonymous route is a button that swaps the dialog body to the embedded form.
        cut.Find("button.feedback__choice").Click();

        var frame = cut.Find("iframe.feedback__frame");
        Assert.Contains("tally.so", frame.GetAttribute("src"));
    }

    [Fact]
    public void The_embedded_form_can_return_to_the_choices()
    {
        var cut = Render();

        cut.Find("button.feedback__choice").Click();
        Assert.Single(cut.FindAll("iframe.feedback__frame"));

        cut.Find("button.feedback__back").Click();
        Assert.Empty(cut.FindAll("iframe.feedback__frame"));
        Assert.Equal(2, cut.FindAll(".feedback__choice").Count);
    }

    [Fact]
    public void The_embedded_form_offers_to_go_back_until_it_is_submitted()
    {
        var cut = Render();

        cut.Find("button.feedback__choice").Click();

        Assert.Contains("Back to options", cut.Find("button.feedback__back").TextContent);
    }

    [Fact]
    public async Task Submitting_the_form_swaps_the_back_control_for_a_close_one()
    {
        var cut = Render();

        cut.Find("button.feedback__choice").Click();

        // Tally relays the submission to the component's [JSInvokable] OnSubmitted via JS.
        await cut.InvokeAsync(() => cut.Instance.OnSubmitted());

        // The quiet "Back to options" text link gives way to a real footer Close button.
        Assert.Empty(cut.FindAll("button.feedback__back"));
        Assert.Equal("Close", cut.Find("button.feedback__close").TextContent);
    }

    [Fact]
    public async Task Clicking_the_trigger_opens_the_dialog()
    {
        var cut = Render();

        await cut.InvokeAsync(() => cut.Find("button.feedback__trigger").Click());

        Assert.Single(JSInterop.Invocations["respondeoDialog.show"]);
    }

    [Fact]
    public async Task Reopening_the_dialog_returns_to_the_choices()
    {
        var cut = Render();

        // Navigate into the form, close, then reopen: should be back on the choice screen.
        cut.Find("button.feedback__choice").Click();
        Assert.Single(cut.FindAll("iframe.feedback__frame"));

        await cut.InvokeAsync(() => cut.Find("button.feedback__trigger").Click());

        Assert.Empty(cut.FindAll("iframe.feedback__frame"));
        Assert.Equal(2, cut.FindAll(".feedback__choice").Count);
    }

    [Fact]
    public void With_external_feedback_off_the_trigger_links_straight_to_github()
    {
        _features.ExternalFeedback = false;

        var cut = Render();

        // No dialog-opening button: the trigger is a direct GitHub anchor instead.
        Assert.Empty(cut.FindAll("button.feedback__trigger"));
        var trigger = cut.Find("a.feedback__trigger");
        Assert.Equal("https://github.com/Delubear/Respondeo/issues/new/choose", trigger.GetAttribute("href"));
        Assert.Equal("_blank", trigger.GetAttribute("target"));
        Assert.Contains("noopener", trigger.GetAttribute("rel"));
    }

    [Fact]
    public void With_external_feedback_off_there_is_no_choice_dialog()
    {
        _features.ExternalFeedback = false;

        var cut = Render();

        Assert.Empty(cut.FindAll(".feedback__choice"));
        Assert.Empty(cut.FindAll("iframe.feedback__frame"));
    }
}
