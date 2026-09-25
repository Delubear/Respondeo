using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Summa;
using Respondeo.Content.Summa.Services;
using Respondeo.Services;
using SummaQuestionPage = Respondeo.Pages.SummaQuestion;

namespace Respondeo.UnitTests.Pages;

public class SummaQuestionTests : TestContext
{
    private const string IndexJson = """
        {
          "parts": [
            { "id": "p1", "title": "First Part", "questions": [] }
          ]
        }
        """;

    private const string QuestionJson = """
        {
          "id": "p1-q002",
          "partId": "p1",
          "number": 2,
          "title": "The Simplicity of God",
          "prologueHtml": "<p>We consider the simplicity of God.</p>",
          "articles": [
            {
              "number": 1,
              "title": "Whether God is a body?",
              "respondeoHtml": "<p>I answer that God is not a body.</p>"
            },
            {
              "number": 2,
              "title": "Whether God is composed of matter and form?",
              "respondeoHtml": "<p>I answer that God is not composed.</p>"
            }
          ]
        }
        """;

    private const string IndexPath = "_content/Respondeo.Content.Summa/summa/summa-index.json";
    private const string QuestionPath = "_content/Respondeo.Content.Summa/summa/first-part/p1-q002.json";

    public SummaQuestionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new StubHandler(new Dictionary<string, string>
        {
            [IndexPath] = IndexJson,
            [QuestionPath] = QuestionJson,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<ISummaService>(new SummaService(http));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    [Fact]
    public void Renders_the_question_title_and_number()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("The Simplicity of God", cut.Markup);
            Assert.Contains("Question 2", cut.Markup);
        });
    }

    [Fact]
    public void Renders_the_prologue_html()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() =>
            Assert.Contains("We consider the simplicity of God.", cut.Markup));
    }

    [Fact]
    public void Renders_a_toc_link_for_each_article()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll(".summa-question__toc-item").ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains("Whether God is a body?", items[0].TextContent);
        });
    }

    [Fact]
    public void Renders_a_details_panel_for_each_article()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() =>
        {
            var articles = cut.FindAll("details.summa-article").ToList();
            Assert.Equal(2, articles.Count);
        });
    }

    [Fact]
    public void Articles_start_collapsed()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() =>
        {
            var articles = cut.FindAll("details.summa-article").ToList();
            Assert.All(articles, a => Assert.False(a.HasAttribute("open")));
        });
    }

    [Fact]
    public void Clicking_a_toc_link_opens_its_article()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".summa-question__toc-item a").Count));

        cut.FindAll(".summa-question__toc-item a").ToList()[1].Click();

        cut.WaitForAssertion(() =>
        {
            var articles = cut.FindAll("details.summa-article").ToList();
            Assert.False(articles[0].HasAttribute("open"));
            Assert.True(articles[1].HasAttribute("open"));
        });
    }

    [Fact]
    public void Clicking_an_article_summary_toggles_it_open_and_closed()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q002"));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("details.summa-article").Count));

        cut.FindAll(".summa-article__summary").ToList()[0].Click();
        cut.WaitForAssertion(() => Assert.True(cut.FindAll("details.summa-article").ToList()[0].HasAttribute("open")));

        cut.FindAll(".summa-article__summary").ToList()[0].Click();
        cut.WaitForAssertion(() => Assert.False(cut.FindAll("details.summa-article").ToList()[0].HasAttribute("open")));
    }

    [Fact]
    public void Renders_not_found_for_an_unknown_question()
    {
        var cut = RenderComponent<SummaQuestionPage>(p => p.Add(c => c.Id, "prima-q999"));

        cut.WaitForAssertion(() => Assert.Contains("Question not found", cut.Markup));
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
