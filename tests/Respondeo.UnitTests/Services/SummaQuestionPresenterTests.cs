using Respondeo.Content.Summa;
using Respondeo.Content.Summa.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class SummaQuestionPresenterTests
{
    // A stub index service returning a fixed index, so the presenter's part/treatise lookup is exercised
    // without touching the bundled corpus.
    private sealed class StubSummaService(SummaIndex index) : ISummaService
    {
        public Task<SummaIndex> GetIndexAsync() => Task.FromResult(index);

        public Task<SummaQuestionContent?> GetQuestionAsync(string id) => Task.FromResult<SummaQuestionContent?>(null);
    }

    private static SummaQuestionPresenter Presenter(SummaIndex index) =>
        new(new StubSummaService(index), new SummaPartCatalog());

    private static SummaIndex IndexWith(string partId, string partTitle, string questionId, string? treatise) => new()
    {
        Parts =
        [
            new SummaPartEntry
            {
                Id = partId,
                Title = partTitle,
                Questions =
                [
                    new SummaQuestionEntry { Id = questionId, Number = 2, Title = "Whatever", Treatise = treatise },
                ],
            },
        ],
    };

    [Fact]
    public async Task BuildAsync_NullQuestion_ReturnsEmptyViewWithDefaultDescription()
    {
        var view = await Presenter(new SummaIndex()).BuildAsync(null);

        Assert.Equal(string.Empty, view.PartTitle);
        Assert.Null(view.Treatise);
        Assert.Null(view.Part);
        Assert.Equal(SiteMeta.DefaultDescription, view.SeoDescription);
    }

    [Fact]
    public async Task BuildAsync_ResolvesPartTitleTreatiseAndAncestor()
    {
        var index = IndexWith("p1", "First Part", "p1-q002", "Treatise on Creation");
        var question = new SummaQuestionContent
        {
            Id = "p1-q002",
            PartId = "p1",
            Number = 2,
            Title = "The existence of God",
            Articles =
            [
                new SummaArticleContent { Number = 1, Title = "Whether the existence of God is self-evident?" },
                new SummaArticleContent { Number = 2, Title = "Whether it can be demonstrated?" },
            ],
        };

        var view = await Presenter(index).BuildAsync(question);

        Assert.Equal("First Part", view.PartTitle);
        Assert.Equal("Treatise on Creation", view.Treatise);
        Assert.NotNull(view.Part);
        Assert.Equal("p1", view.Part!.Key);
        Assert.Contains("The existence of God", view.SeoDescription);
        Assert.Contains("Whether the existence of God is self-evident?", view.SeoDescription);
    }

    [Fact]
    public async Task BuildAsync_Prologue_UsesPrologueSeoDescription()
    {
        var index = IndexWith("p1", "First Part", "p1-prologue", null);
        var question = new SummaQuestionContent
        {
            Id = "p1-prologue",
            PartId = "p1",
            Number = 0,
            Title = "Introduction",
            IsPrologue = true,
        };

        var view = await Presenter(index).BuildAsync(question);

        Assert.Contains("Prologue: Introduction", view.SeoDescription);
    }

    [Fact]
    public void BuildBackCrumb_NullOrigin_ReturnsNone()
    {
        var (href, label) = Presenter(new SummaIndex()).BuildBackCrumb(null, "prima-q002");

        Assert.Null(href);
        Assert.Null(label);
    }

    [Fact]
    public void BuildBackCrumb_SelfReference_ReturnsNone()
    {
        var origin = new ReferenceOrigin("prima-q002", 1, "The existence of God");

        var (href, label) = Presenter(new SummaIndex()).BuildBackCrumb(origin, "prima-q002");

        Assert.Null(href);
        Assert.Null(label);
    }

    [Fact]
    public void BuildBackCrumb_WithArticle_DeepLinksToArticleAnchor()
    {
        var origin = new ReferenceOrigin("prima-q002", 3, "The existence of God");

        var (href, label) = Presenter(new SummaIndex()).BuildBackCrumb(origin, "prima-q084");

        Assert.Equal("summa/prima-q002#article-3", href);
        Assert.Equal("\u2190 The existence of God", label);
    }

    [Fact]
    public void BuildBackCrumb_WithoutArticle_LinksToQuestion()
    {
        var origin = new ReferenceOrigin("prima-q002", null, "The existence of God");

        var (href, label) = Presenter(new SummaIndex()).BuildBackCrumb(origin, "prima-q084");

        Assert.Equal("summa/prima-q002", href);
        Assert.Equal("\u2190 The existence of God", label);
    }
}
