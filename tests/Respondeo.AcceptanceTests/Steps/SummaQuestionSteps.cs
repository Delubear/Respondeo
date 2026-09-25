using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class SummaQuestionSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [Given("I open question \"(.*)\"")]
    public async Task GivenIOpenQuestion(string id)
    {
        await Page.GotoAsync($"{context.BaseUrl}/summa/{id}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        // The articles render only after the question content loads over HTTP.
        await Page.WaitForSelectorAsync("details.summa-article");
    }

    [Given("I open question \"(.*)\" at article (\\d+)")]
    public async Task GivenIOpenQuestionAtArticle(string id, int articleNumber)
    {
        await Page.GotoAsync(
            $"{context.BaseUrl}/summa/{id}#article-{articleNumber}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("details.summa-article");
    }

    [When("I open article (\\d+) from the table of contents")]
    public async Task WhenIOpenArticleFromTheTableOfContents(int articleNumber)
    {
        await Page.Locator($".summa-question__toc-item >> nth={articleNumber - 1} >> a").ClickAsync();
        await Assertions.Expect(ArticlePanel(articleNumber)).ToHaveAttributeAsync("open", string.Empty);
    }

    [When("I toggle article (\\d+)")]
    public async Task WhenIToggleArticle(int articleNumber)
    {
        await ArticlePanel(articleNumber).Locator("summary.summa-article__summary").ClickAsync();
    }

    [Then("article (\\d+) should be expanded")]
    public async Task ThenArticleShouldBeExpanded(int articleNumber)
    {
        await Assertions.Expect(ArticlePanel(articleNumber)).ToHaveAttributeAsync("open", string.Empty);
    }

    [Then("article (\\d+) should be collapsed")]
    public async Task ThenArticleShouldBeCollapsed(int articleNumber)
    {
        await Assertions.Expect(ArticlePanel(articleNumber)).Not.ToHaveAttributeAsync("open", string.Empty);
    }

    [Then("the address bar should contain \"(.*)\"")]
    public async Task ThenTheAddressBarShouldContain(string fragment)
    {
        await Page.WaitForFunctionAsync($"() => location.href.includes({AsJsString(fragment)})");
    }

    [Then("the address bar should end with \"(.*)\"")]
    public async Task ThenTheAddressBarShouldEndWith(string suffix)
    {
        await Page.WaitForFunctionAsync($"() => location.href.endsWith({AsJsString(suffix)})");
    }

    private ILocator ArticlePanel(int articleNumber) => Page.Locator($"details.summa-article#article-{articleNumber}");

    private static string AsJsString(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
