using Microsoft.Playwright;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests.Steps;

[Binding]
public sealed class BibleSteps(PlaywrightContext context)
{
    private IPage Page => context.Page;

    [When("I open the Bible chapter {string}")]
    public async Task WhenIOpenTheBibleChapter(string path)
    {
        await Page.GotoAsync($"{context.BaseUrl}/bible/{path}");
        await Page.Locator(".bible-chapter, .content-article--empty").First.WaitForAsync();
    }

    [Then("I should see verse {int} of the chapter")]
    public async Task ThenIShouldSeeVerse(int number) =>
        await Assertions.Expect(Page.Locator($".bible-chapter #v{number}")).ToBeVisibleAsync();

    [Then("only verses {string} should be highlighted")]
    public async Task ThenOnlyVersesShouldBeHighlighted(string verses)
    {
        var expected = verses.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => $"v{v.Trim()}").ToArray();
        var highlighted = Page.Locator(".bible-verse--highlight");
        await Assertions.Expect(highlighted).ToHaveCountAsync(expected.Length);

        var actual = new List<string>();
        foreach (var verse in await highlighted.AllAsync())
        {
            actual.Add(await verse.GetAttributeAsync("id") ?? string.Empty);
        }

        Assert.Equal(expected, actual);
        if (expected.Length > 0)
        {
            await Assertions.Expect(highlighted.First).ToBeInViewportAsync();
        }
    }

    [When("I follow the next chapter link")]
    public async Task WhenIFollowTheNextChapterLink() => await Page.Locator(".bible-pager__next").ClickAsync();

    [When("I mark the current chapter article")]
    public async Task WhenIMarkTheCurrentChapterArticle() =>
        await Page.EvaluateAsync("document.querySelector('.bible-chapter').dataset.mark = 'old'");

    [When("I follow the chapter link {string}")]
    public async Task WhenIFollowTheChapterLink(string number) =>
        await Page.Locator(".bible-chapters").GetByRole(AriaRole.Link, new() { Name = number, Exact = true }).ClickAsync();

    [Then("the chapter article should be replaced")]
    public async Task ThenTheChapterArticleShouldBeReplaced()
    {
        await Assertions.Expect(Page.Locator(".bible-chapter[data-mark='old']")).ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator(".bible-chapter").First).ToBeVisibleAsync();
    }

    [Then("the chapter article should be kept")]
    public async Task ThenTheChapterArticleShouldBeKept()
    {
        await Assertions.Expect(Page.Locator(".bible-chapters__link[aria-current='page']")).ToHaveTextAsync("5");
        await Assertions.Expect(Page.Locator(".bible-chapter[data-mark='old']")).ToHaveCountAsync(1);
    }

    [Then("I should see the chapter not found message")]
    public async Task ThenIShouldSeeTheChapterNotFoundMessage() =>
        await Assertions.Expect(Page.GetByText("Chapter not found")).ToBeVisibleAsync();
}
