using Respondeo.Content.Contracts;

namespace Respondeo.Content.Articles;

/// <summary>
/// Maps the internal <see cref="ArticleDocument"/> domain model onto the public
/// <see cref="Article"/> / <see cref="ArticleSummary"/> contract DTOs at the service boundary,
/// so consumers never depend on the parsing/domain types.
/// </summary>
internal static class ArticleMapper
{
    public static Article ToContract(this ArticleDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Topic = document.Topic,
        Tags = document.Tags,
        Sections = [.. document.Sections.Select(s => new ArticleSection { Heading = s.Heading, Html = s.Html })],
        Sources = [.. document.Sources.Select(s => new ArticleSource { Label = s.Label, Url = s.Url })],
    };

    public static ArticleSummary ToSummaryContract(this ArticleDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Topic = document.Topic,
        Tags = document.Tags,
    };
}
