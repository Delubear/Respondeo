using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class ArticleDetailTests : TestContext
{
    private IArticleService _articles = default!;

    public ArticleDetailTests()
    {
        // Breadcrumb imports ./js/breadcrumb.js on first render; tolerate JS calls.
        JSInterop.Mode = JSRuntimeMode.Loose;
        _articles = Substitute.For<IArticleService>();
        Services.AddSingleton(_articles);
        var prayers = Substitute.For<IPrayerService>();
        prayers.QuotePrayerReferencesAsync(Arg.Any<string>()).Returns(c => c.Arg<string>());
        Services.AddSingleton(prayers);
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    private static Article SampleArticle() => new()
    {
        Id = "the-sacraments",
        Title = "The Sacraments",
        Summary = "An overview of the seven sacraments.",
        BodyHtml = "<p>Grace made visible.</p>",
        Sources =
        [
            new ArticleSource { Label = "Catechism", Url = "https://example.org/ccc" },
        ],
    };

    [Fact]
    public void Renders_the_article_title_and_sections()
    {
        _articles.GetArticleAsync("the-sacraments").Returns(SampleArticle());

        var cut = RenderComponent<ArticleDetail>(p => p.Add(c => c.Id, "the-sacraments"));

        Assert.Equal("The Sacraments", cut.Find("h1.pillar__title").TextContent.Trim());
        Assert.Contains("Grace made visible.", cut.Find(".content-body").TextContent);
    }

    [Fact]
    public void Fades_the_article_in_once_loaded()
    {
        _articles.GetArticleAsync("the-sacraments").Returns(SampleArticle());

        var cut = RenderComponent<ArticleDetail>(p => p.Add(c => c.Id, "the-sacraments"));

        Assert.Contains("fade-enter", cut.Find("article.content-article").ClassList);
    }

    [Fact]
    public void Shows_no_faded_content_while_loading()
    {
        var pending = new TaskCompletionSource<Article?>();
        _articles.GetArticleAsync("the-sacraments").Returns(pending.Task);

        var cut = RenderComponent<ArticleDetail>(p => p.Add(c => c.Id, "the-sacraments"));

        Assert.Empty(cut.FindAll(".fade-enter"));
    }

    [Fact]
    public void Renders_sources_with_links()
    {
        _articles.GetArticleAsync("the-sacraments").Returns(SampleArticle());

        var cut = RenderComponent<ArticleDetail>(p => p.Add(c => c.Id, "the-sacraments"));

        var link = cut.Find(".content-sources a");
        Assert.Equal("https://example.org/ccc", link.GetAttribute("href"));
        Assert.Equal("Catechism", link.TextContent.Trim());
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_article()
    {
        _articles.GetArticleAsync("nope").Returns((Article?)null);

        var cut = RenderComponent<ArticleDetail>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Article not found", cut.Markup);
        Assert.Contains("nope", cut.Find("code").TextContent);
    }
}
