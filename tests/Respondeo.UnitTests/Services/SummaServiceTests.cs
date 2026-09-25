using System.Net;
using System.Text;
using Respondeo.Content.Summa.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class SummaServiceTests
{
    private const string IndexJson = """
        {
          "parts": [
            {
              "id": "p1",
              "title": "First Part",
              "questions": [
                {
                  "id": "p1-q001",
                  "number": 1,
                  "title": "The Existence of God",
                  "articles": [
                    { "number": 1, "title": "Whether the existence of God is self-evident?" },
                    { "number": 2, "title": "Whether it can be demonstrated that God exists?" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string QuestionJson = """
        {
          "id": "p1-q001",
          "partId": "p1",
          "number": 1,
          "title": "The Existence of God",
          "prologueHtml": "<p>Prologue.</p>",
          "articles": [
            { "number": 1, "title": "Whether the existence of God is self-evident?", "respondeoHtml": "<p>Answer one.</p>" }
          ]
        }
        """;

    private const string IndexPath = "_content/Respondeo.Content.Summa/summa/summa-index.json";
    private const string QuestionPath = "_content/Respondeo.Content.Summa/summa/first-part/p1-q001.json";

    private static SummaService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [IndexPath] = IndexJson,
            [QuestionPath] = QuestionJson,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new SummaService(http);
    }

    [Fact]
    public async Task GetIndex_returns_parts_questions_and_article_titles()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        var part = Assert.Single(index.Parts);
        Assert.Equal("First Part", part.Title);
        var question = Assert.Single(part.Questions);
        Assert.Equal("The Existence of God", question.Title);
        Assert.Equal(2, question.Articles.Count);
        Assert.Equal("Whether the existence of God is self-evident?", question.Articles[0].Title);
    }

    [Fact]
    public async Task GetQuestion_returns_full_content_with_body_html()
    {
        var service = CreateService();

        var question = await service.GetQuestionAsync("p1-q001");

        Assert.NotNull(question);
        Assert.Equal("p1", question!.PartId);
        Assert.Contains("<p>Prologue.</p>", question.PrologueHtml);
        var article = Assert.Single(question.Articles);
        Assert.Contains("Answer one.", article.RespondeoHtml);
    }

    [Fact]
    public async Task GetQuestion_returns_null_for_unknown_id()
    {
        var service = CreateService();

        var question = await service.GetQuestionAsync("p1-q999");

        Assert.Null(question);
    }

    [Fact]
    public async Task GetQuestion_returns_null_for_empty_id()
    {
        var service = CreateService();

        Assert.Null(await service.GetQuestionAsync(""));
    }

    [Fact]
    public async Task GetIndex_is_cached_across_calls()
    {
        var handler = new StubHandler(new Dictionary<string, string> { [IndexPath] = IndexJson });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var service = new SummaService(http);

        await service.GetIndexAsync();
        await service.GetIndexAsync();

        Assert.Equal(1, handler.RequestCount(IndexPath));
    }
}
