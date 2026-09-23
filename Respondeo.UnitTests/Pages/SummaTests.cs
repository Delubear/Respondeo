using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Summa;
using Respondeo.Content.Summa.Services;
using Respondeo.Services;
using SummaPage = Respondeo.Pages.Summa;

namespace Respondeo.UnitTests.Pages;

public class SummaTests : TestContext
{
    private const string IndexJson = """
        {
          "parts": [
            {
              "id": "fp",
              "title": "First Part",
              "questions": [
                {
                  "id": "fp-q001",
                  "number": 1,
                  "title": "The Existence of God",
                  "articles": [
                    { "number": 1, "title": "Whether the existence of God is self-evident?" }
                  ]
                },
                {
                  "id": "fp-q002",
                  "number": 2,
                  "title": "The Simplicity of God",
                  "articles": [
                    { "number": 1, "title": "Whether God is a body?" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string IndexPath = "_content/Respondeo.Content.Summa/summa/summa-index.json";

    public SummaTests()
    {
        var handler = new StubHandler(new Dictionary<string, string> { [IndexPath] = IndexJson });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<ISummaService>(new SummaService(http));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    [Fact]
    public void Renders_a_link_for_each_question()
    {
        var cut = RenderComponent<SummaPage>();

        var hrefs = cut.FindAll("a.summa__question-link").Select(a => a.GetAttribute("href")).ToList();
        Assert.Contains("summa/fp-q001", hrefs);
        Assert.Contains("summa/fp-q002", hrefs);
    }

    [Fact]
    public void Search_by_question_title_filters_to_matching_question()
    {
        var cut = RenderComponent<SummaPage>();

        cut.Find("#summa-search").Input("Simplicity");

        cut.WaitForAssertion(() =>
        {
            var matches = cut.FindAll("a.summa__match-link").Select(a => a.GetAttribute("href")).ToList();
            Assert.Single(matches);
            Assert.Equal("summa/fp-q002", matches[0]);
        });
    }

    [Fact]
    public void Search_by_article_title_matches_whether_text()
    {
        var cut = RenderComponent<SummaPage>();

        cut.Find("#summa-search").Input("self-evident");

        cut.WaitForAssertion(() =>
        {
            var titles = cut.FindAll(".summa__match-title").Select(e => e.TextContent).ToList();
            Assert.Contains(titles, t => t.Contains("self-evident", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void Search_with_no_match_shows_empty_message()
    {
        var cut = RenderComponent<SummaPage>();

        cut.Find("#summa-search").Input("zzznomatch");

        cut.WaitForAssertion(() => Assert.Contains("No questions or articles match", cut.Markup));
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            if (responses.TryGetValue(path, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
