using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class NavigationSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open the start page")]
    public async Task GivenIOpenTheStartPage()
    {
        // Blazor WebAssembly downloads its runtime after the load event, so wait for the network to settle before asserting the app has rendered its entry-point questions.
        await Page.GotoAsync(context.BaseUrl + "/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".reel__q");
    }

    [When("I choose the first stage card")]
    public async Task WhenIChooseTheFirstStageCard()
    {
        // The home reel renders the journey with Stage 1 at the LEFT (walking rightward) and scrolls to it on init.
        // Stage 1 is the FIRST `.reel__step`, so we wait for the left-most question specifically to become centered (and thus unmasked) before clicking.
        var stageOne = Page.Locator(".reel__step:first-child.is-centered");
        await stageOne.WaitForAsync();

        // Clicking a question at (or near) center always follows its link — the reel only intercepts clicks on steps that are far off-center.
        // Dispatch the click straight to the DOM so Playwright does NOT auto-scroll the reel first: that scroll could un-center the question
        // past the interception threshold and turn the click into a one-stage nudge instead of navigation. The centered anchor navigates deterministically.
        await stageOne.Locator(".reel__q").DispatchEventAsync("click");
        // Confirm we left Home and the destination stage page rendered: `.card` are the branch cards on a stage/node
        // page (NOT the Home reel's `.reel__q`). Waiting here syncs before the next step clicks a branch card.
        await Page.WaitForSelectorAsync(".card");
    }

    [When("I choose the first branch card")]
    public async Task WhenIChooseTheFirstBranchCard()
    {
        await Page.Locator(".card").First.ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb");
    }

    [When("I navigate back to the start page")]
    public async Task WhenINavigateBackToTheStartPage()
    {
        await Page.GotoAsync(context.BaseUrl + "/");
        await Page.WaitForSelectorAsync(".reel__q");
    }

    [Given("I open the \"(.*)\" stage directly")]
    [When("I open the \"(.*)\" stage directly")]
    public async Task GivenIOpenTheStageDirectly(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".card");
    }

    [Given("I open the \"(.*)\" path directly")]
    public async Task GivenIOpenThePathDirectly(string slug)
    {
        await Page.GotoAsync($"{context.BaseUrl}/{slug}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [When("I choose the first masthead stage")]
    public async Task WhenIChooseTheFirstMastheadStage()
    {
        // The first masthead link is Home; the first stage link is the next one.
        // We may be on a node page whose branch cards are still in the DOM,
        // so wait for the client-side navigation to actually land on the stage page (which has no breadcrumb) before proceeding,
        // otherwise a later step could click a stale card mid-transition.
        await Page.Locator(".mastnav__link").Nth(1).ClickAsync();
        await Page.WaitForSelectorAsync(".breadcrumb", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });
        await Page.WaitForSelectorAsync(".card");
    }

    [Then("I should see at least one stage card")]
    public async Task ThenIShouldSeeAtLeastOneStageCard()
    {
        // The home reel renders .reel__q; stage pages render their branch .card list.
        // Either satisfies "at least one entry to move forward from here".
        var count = await Page.Locator(".reel__q, .card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one entry, found {count}.");
    }

    [Then("I should see the not found page")]
    public async Task ThenIShouldSeeTheNotFoundPage()
    {
        await Page.WaitForSelectorAsync(".pillar__eyebrow");
        Assert.Contains("Page Not Found", await Page.Locator(".pillar__eyebrow").First.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Then("the breadcrumb root should not be \"(.*)\"")]
    public async Task ThenTheBreadcrumbRootShouldNotBe(string label)
    {
        await Page.WaitForSelectorAsync(".breadcrumb");
        // The root crumb is the first link in the breadcrumb list.
        var root = await Page.Locator(".breadcrumb__list a").First.InnerTextAsync();
        Assert.NotEqual(label, root.Trim());
    }

    [Then("the page should show a breadcrumb")]
    public async Task ThenThePageShouldShowABreadcrumb()
    {
        Assert.True(await Page.Locator(".breadcrumb").IsVisibleAsync());
    }

    [When("I press the \"walk onward\" control")]
    public async Task WhenIPressTheWalkOnwardControl()
    {
        await ReelHint("walk onward").ClickAsync();
        // The reel scrolls smoothly; wait for the "earlier steps" control to become active.
        await Page.WaitForSelectorAsync(".reel__hint--prev:not(.is-hidden)");
    }

    [Then("the \"(.*)\" control should be visible")]
    public async Task ThenTheControlShouldBeVisible(string control)
    {
        await Assertions.Expect(ReelHint(control)).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-hidden\b"));
    }

    [Then("the \"(.*)\" control should be hidden")]
    public async Task ThenTheControlShouldBeHidden(string control)
    {
        await Assertions.Expect(ReelHint(control)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-hidden\b"));
    }

    // Maps the human-readable control name to its reel hint element.
    private ILocator ReelHint(string control) => control.Trim().ToLowerInvariant() switch
    {
        "walk onward" => Page.Locator(".reel__hint--next"),
        "earlier steps" => Page.Locator(".reel__hint--prev"),
        _ => throw new ArgumentOutOfRangeException(nameof(control), control, "Unknown reel control."),
    };

    [When(@"I scroll the wheel forward over the reel")]
    public async Task WhenIScrollTheWheelForwardOverTheReel()
    {
        var reel = Page.Locator(".reel");
        await reel.HoverAsync();
        // A single forward wheel notch over the reel should advance exactly one card.
        await Page.Mouse.WheelAsync(0, 240);
    }

    [Then(@"the second stage card should be centered")]
    public async Task ThenTheSecondStageCardShouldBeCentred()
    {
        // Stage 2 is the second `.reel__step`; wait for it to gain the centered marker.
        await Page.WaitForSelectorAsync(".reel__step:nth-child(2).is-centered");
    }

    [Given(@"I am viewing on a (.*) pixel wide screen")]
    public async Task GivenIAmViewingOnAPixelWideScreen(int width)
    {
        // Set the viewport before navigating so the responsive rules apply on first paint.
        await Page.SetViewportSizeAsync(width, 900);
    }

    [Then(@"the ""(.*)"" control label should be hidden")]
    public async Task ThenTheControlLabelShouldBeHidden(string control)
    {
        // Below 800px the pill collapses to an icon-only button: the label is display:none.
        var label = ReelHint(control).Locator(".reel__hint-label");
        Assert.False(await label.IsVisibleAsync());
    }

    [Then("the breadcrumb should contain at least (.*) steps")]
    public async Task ThenTheBreadcrumbShouldContainAtLeastSteps(int minimum)
    {
        var count = await CountBreadcrumbStepsAsync();
        Assert.True(count >= minimum, $"Expected at least {minimum} breadcrumb steps, found {count}.");
    }

    [Then("the breadcrumb should contain (.*) step")]
    public async Task ThenTheBreadcrumbShouldContainStep(int expected)
    {
        var count = await CountBreadcrumbStepsAsync();
        Assert.Equal(expected, count);
    }

    // A "step" is a visited node: an ancestor crumb link plus the current node marker.
    // The static "Home" link and the "/" separators are intentionally excluded.
    private async Task<int> CountBreadcrumbStepsAsync()
    {
        var crumbLinks = await Page.Locator(".breadcrumb__link").CountAsync();
        var current = await Page.Locator(".breadcrumb__current").CountAsync();
        return crumbLinks + current;
    }

    [Then("I should see the \"(.*)\" off-ramp")]
    public async Task ThenIShouldSeeTheOffRamp(string lead)
    {
        var aside = Page.Locator(".reel__aside-lead");
        await aside.WaitForAsync();
        Assert.Equal(lead, (await aside.InnerTextAsync()).Trim());
    }

    [Then("I should see an off-ramp pill linking to \"(.*)\"")]
    public async Task ThenIShouldSeeAnOffRampPillLinkingTo(string slug)
    {
        var pill = OffRampPill(slug);
        Assert.True(await pill.IsVisibleAsync(), $"Expected an off-ramp pill linking to '{slug}'.");
    }

    [Then("the off-ramp pills should have the same width")]
    public async Task ThenTheOffRampPillsShouldHaveTheSameWidth()
    {
        var pills = Page.Locator(".reel__pill");
        await pills.First.WaitForAsync();
        var count = await pills.CountAsync();
        Assert.Equal(2, count);

        var first = await pills.Nth(0).BoundingBoxAsync();
        var second = await pills.Nth(1).BoundingBoxAsync();
        Assert.NotNull(first);
        Assert.NotNull(second);
        // Equal-width grid columns should render identical pill widths (allow a sub-pixel rounding tolerance).
        Assert.True(Math.Abs(first!.Width - second!.Width) <= 1,
            $"Expected equal pill widths, found {first.Width} and {second.Width}.");
    }

    [When("I choose the off-ramp pill linking to \"(.*)\"")]
    public async Task WhenIChooseTheOffRampPillLinkingTo(string slug)
    {
        await OffRampPill(slug).ClickAsync();
    }

    [Then("the address should be \"(.*)\"")]
    public async Task ThenTheAddressShouldBe(string path)
    {
        // WaitForURLAsync throws if the client-side navigation does not land on the expected path.
        await Page.WaitForURLAsync($"{context.BaseUrl}{path}");
        Assert.Equal(path, new Uri(Page.Url).AbsolutePath);
    }

    // The off-ramp beneath the reel renders two .reel__pill links; match by the destination href.
    private ILocator OffRampPill(string slug) => Page.Locator($".reel__pill[href='{slug}']");
}
