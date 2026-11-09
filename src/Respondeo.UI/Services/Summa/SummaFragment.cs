namespace Respondeo.Services.Summa;

/// <summary>
/// Pure parsing helpers for Summa article URL fragments.
/// A fragment names an article, optionally with a sub-anchor: <c>article-3</c>, <c>article-3-reply-2</c>,
/// <c>article-3-objection-1</c>, or <c>article-3-contra</c>. Kept free of Blazor/JS so the rules are unit-testable in isolation.
/// </summary>
public static class SummaFragment
{
    private const string ArticlePrefix = "article-";

    /// <summary>
    /// Extracts the owning article number from a fragment, whether bare (<c>article-3</c>) or a sub-anchor such as <c>article-3-reply-2</c>.
    /// Returns false for any non-article fragment.
    /// </summary>
    public static bool TryGetArticleNumber(string? fragment, out int articleNumber)
    {
        articleNumber = 0;
        if (string.IsNullOrEmpty(fragment) || !fragment.StartsWith(ArticlePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = fragment[ArticlePrefix.Length..];
        var end = rest.IndexOf('-');
        var digits = end < 0 ? rest : rest[..end];
        return int.TryParse(digits, out articleNumber);
    }

    /// <summary>The bare anchor for an article number (e.g. 3 -> <c>article-3</c>).</summary>
    public static string ArticleAnchor(int articleNumber) => $"{ArticlePrefix}{articleNumber}";
}
