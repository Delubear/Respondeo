using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class QuickMenuPinSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("the \"(.*)\" guide orb is pinned in the quick menu")]
    public async Task GivenTheGuideOrbIsPinnedInTheQuickMenu(string orbId)
    {
        await Page.EvaluateAsync("id => localStorage.setItem('respondeo.pins', JSON.stringify(['orb:' + id]))", orbId);
        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".quick-menu__toggle");
    }

    [When("I open the quick menu")]
    public async Task WhenIOpenTheQuickMenu()
    {
        await Page.Locator(".quick-menu__toggle").ClickAsync();
        await Page.WaitForSelectorAsync("nav.quick-menu");
    }

    [When("I choose the \"(.*)\" pin")]
    public async Task WhenIChooseThePin(string label)
    {
        await Page.Locator(".quick-menu__section--pinned .quick-menu__item")
            .Filter(new LocatorFilterOptions { HasText = label }).First.ClickAsync();
        await Page.WaitForSelectorAsync(".dialog[open]");
    }
}
