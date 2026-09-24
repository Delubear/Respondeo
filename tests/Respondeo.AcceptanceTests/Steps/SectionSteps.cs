using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class SectionSteps(PlaywrightContext context)
{
    private const string PageId = "aquinas-five-ways";
    private const string SecondSectionId = "five-ways-essence-existence";

    private IPage Page => context.Page;

    [Given("I open the Five Ways page")]
    public async Task GivenIOpenTheFiveWaysPage()
    {
        await Page.GotoAsync($"{context.BaseUrl}/node/{PageId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".accordion__trigger");
    }

    [Given("I open the Five Ways page with the second section in the URL")]
    public async Task GivenIOpenTheFiveWaysPageWithTheSecondSectionInTheUrl()
    {
        await Page.GotoAsync($"{context.BaseUrl}/node/{PageId}?section={SecondSectionId}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync(".accordion__trigger");
    }

    [When("I open the first section")]
    public async Task WhenIOpenTheFirstSection()
    {
        await Page.Locator(".accordion__trigger").Nth(0).ClickAsync();
        await Page.WaitForSelectorAsync(".accordion__panel");
    }

    [When("I open the second section")]
    public async Task WhenIOpenTheSecondSection()
    {
        await Page.Locator(".accordion__trigger").Nth(1).ClickAsync();
    }

    [Then("the first section should be expanded")]
    public async Task ThenTheFirstSectionShouldBeExpanded()
    {
        var expanded = await Page.Locator(".accordion__trigger").Nth(0).GetAttributeAsync("aria-expanded");
        Assert.Equal("true", expanded);
    }

    [Then("the second section should be expanded")]
    public async Task ThenTheSecondSectionShouldBeExpanded()
    {
        var expanded = await Page.Locator(".accordion__trigger").Nth(1).GetAttributeAsync("aria-expanded");
        Assert.Equal("true", expanded);
    }

    [Then("only one section should be expanded")]
    public async Task ThenOnlyOneSectionShouldBeExpanded()
    {
        var expandedCount = await Page.Locator(".accordion__trigger[aria-expanded='true']").CountAsync();
        Assert.Equal(1, expandedCount);
    }

    [Then("the URL should contain the first section")]
    public async Task ThenTheUrlShouldContainTheFirstSection()
    {
        Assert.Contains("section=", Page.Url);
    }
}
