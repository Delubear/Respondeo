using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Services.Summa;

/// <summary>Picks a random Summa article and returns its relative URL, shared by the Summa button and the quick-menu action.</summary>
public sealed class RandomSummaArticle(ISummaService summa, SummaSearch search, ISummaPartCatalog parts)
{
    /// <summary>Returns a relative URL
    public async Task<string?> PickUrlAsync()
    {
        var index = await summa.GetIndexAsync();
        var articles = search.BuildEntries(index).Where(e => e.ArticleNumber is not null).ToArray();
        if (articles.Length == 0)
        {
            return null;
        }

        var pick = articles[Random.Shared.Next(articles.Length)];
        return $"summa/{parts.ToUrlId(pick.QuestionId)}#article-{pick.ArticleNumber}";
    }
}
