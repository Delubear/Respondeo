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
        // Blazor WebAssembly downloads its runtime after the load event, so wait for the network to settle before asserting the app has rendered its entry-point cards.
        await Page.GotoAsync(context.BaseUrl + "/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".stage-card");
    }

    [When("I choose the first stage card")]
    public async Task WhenIChooseTheFirstStageCard()
    {
        // The home reel renders the journey with Stage 1 at the LEFT (walking rightward) and scrolls to it on init.
        // Stage 1 is the FIRST `.reel__step`, so we wait for the left-most card specifically to become centred (and thus unmasked) before clicking.
        var stageOne = Page.Locator(".reel__step:first-child.is-centered");
        await stageOne.WaitForAsync();

        // Clicking a card at (or near) centre always follows its link — the reel only intercepts clicks on cards that are far off-centre.
        // Dispatch the click straight to the DOM so Playwright does NOT auto-scroll the reel first: that scroll could un-centre the card
        // past the interception threshold and turn the click into a one-stage nudge instead of navigation. The centred anchor navigates deterministically.
        await stageOne.Locator(".stage-card").DispatchEventAsync("click");
        // Confirm we left Home and the destination stage page rendered: `.card` are the branch cards on a stage/node
        // page (NOT the Home reel's `.stage-card`). Waiting here syncs before the next step clicks a branch card.
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
        await Page.WaitForSelectorAsync(".stage-card");
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
        // The home reel renders .stage-card; stage pages render their branch .card list.
        // Either satisfies "at least one card to move forward from here".
        var count = await Page.Locator(".stage-card, .card").CountAsync();
        Assert.True(count >= 1, $"Expected at least one card, found {count}.");
    }

    [Then("I should see the not found page")]
    public async Task ThenIShouldSeeTheNotFoundPage()
    {
        await Page.WaitForSelectorAsync("h3");
        Assert.Contains("Not Found", await Page.Locator("h3").First.InnerTextAsync());
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

    [Then(@"the second stage card should be centred")]
    public async Task ThenTheSecondStageCardShouldBeCentred()
    {
        // Stage 2 is the second `.reel__step`; wait for it to gain the centred marker.
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
}
