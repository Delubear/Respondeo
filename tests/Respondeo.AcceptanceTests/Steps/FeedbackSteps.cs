using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class FeedbackSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [When("I open the feedback dialog")]
    public async Task WhenIOpenTheFeedbackDialog()
    {
        // The feedback trigger lives in the footer; it opens the shared <dialog> via respondeoDialog.show.
        await Page.Locator(".feedback__trigger").First.ClickAsync();
        await Page.WaitForSelectorAsync(".feedback__choice");
    }

    [Then("I should see a feedback choice to open a GitHub issue")]
    public async Task ThenIShouldSeeAGitHubIssueChoice()
    {
        var github = Page.Locator("a.feedback__choice[href*='github.com']");
        Assert.True(await github.IsVisibleAsync(), "Expected a GitHub issue feedback choice.");
    }

    [Then("I should see an anonymous feedback choice that needs no account")]
    public async Task ThenIShouldSeeAnAnonymousFeedbackChoice()
    {
        var count = await Page.Locator(".feedback__choice").CountAsync();
        Assert.Equal(2, count);
        // The anonymous route is a button (it swaps the dialog body to an embedded form), not a link.
        var anonymous = Page.Locator("button.feedback__choice");
        Assert.True(await anonymous.IsVisibleAsync(), "Expected an anonymous (non-GitHub) feedback choice.");
    }

    [Then("the anonymous feedback choice should not link to GitHub")]
    public async Task ThenTheAnonymousFeedbackChoiceShouldNotLinkToGitHub()
    {
        // Choosing anonymous embeds the form in place rather than navigating to GitHub.
        await Page.Locator("button.feedback__choice").ClickAsync();
        var frame = Page.Locator("iframe.feedback__frame");
        await frame.WaitForAsync();
        var src = await frame.GetAttributeAsync("src");
        Assert.False(string.IsNullOrWhiteSpace(src), "Embedded feedback form should have a source.");
        Assert.DoesNotContain("github.com", src!, StringComparison.OrdinalIgnoreCase);
    }
}
