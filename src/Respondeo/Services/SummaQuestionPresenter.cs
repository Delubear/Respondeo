using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Services;

/// <summary>
/// The cross-reference origin recorded client-side when a <c>summa-ref</c> link is clicked,
/// read back (via JS) on the destination question page to offer a "back" breadcrumb to where the reader jumped from.
/// </summary>
public sealed record ReferenceOrigin(string QuestionId, int? ArticleNumber, string Label);

/// <summary>
/// The display data a <see cref="SummaQuestionContent"/> expands into: the part context,
/// a concise SEO description, and the Part ancestor (for the breadcrumb).
/// Pure data, assembled by <see cref="SummaQuestionPresenter"/>.
/// </summary>
public sealed record SummaQuestionView(string PartTitle, string? Treatise, SummaPart? Part, string SeoDescription);

/// <summary>
/// Assembles the non-interactive display data for a Summa question page out of the content services,
/// so the component is left with lifecycle, the open-article set, and JS interop.
/// Pure orchestration and formatting, kept out of the component to be unit-testable.
/// </summary>
public sealed class SummaQuestionPresenter(ISummaService summa, ISummaPartCatalog parts)
{
    /// <summary>
    /// Resolves the part title and treatise (from the index), the Part ancestor (from the catalog),
    /// and the SEO description for the given question. Returns an empty view for a null question.
    /// </summary>
    public async Task<SummaQuestionView> BuildAsync(SummaQuestionContent? question)
    {
        if (question is null)
        {
            return new SummaQuestionView(string.Empty, null, null, SiteMeta.DefaultDescription);
        }

        var index = await summa.GetIndexAsync();
        var part = index.Parts.FirstOrDefault(p => string.Equals(p.Id, question.PartId, StringComparison.OrdinalIgnoreCase));

        var partTitle = part?.Title ?? string.Empty;
        var treatise = part?.Questions.FirstOrDefault(q => string.Equals(q.Id, question.Id, StringComparison.OrdinalIgnoreCase))?.Treatise;

        var crumbPart = parts.ByStorageKey(question.PartId);
        var seo = BuildSeoDescription(question, partTitle);

        return new SummaQuestionView(partTitle, treatise, crumbPart, seo);
    }

    /// <summary>
    /// Maps a (consumed) reference origin into the back-crumb href and label,
    /// or <c>(null, null)</c> when there is no usable origin or it points at the question currently shown.
    /// </summary>
    public (string? Href, string? Label) BuildBackCrumb(ReferenceOrigin? origin, string currentId)
    {
        if (origin is null || string.IsNullOrEmpty(origin.QuestionId))
        {
            return (null, null);
        }

        // A self-reference (shouldn't happen since the click listener filters it) offers nothing to go back to.
        if (string.Equals(origin.QuestionId, currentId, StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        var href = origin.ArticleNumber is int articleNumber
            ? $"summa/{origin.QuestionId}#{SummaFragment.ArticleAnchor(articleNumber)}"
            : $"summa/{origin.QuestionId}";

        return (href, $"\u2190 {origin.Label}");
    }

    // A concise meta description built from the question's articles,
    // so search results summarize the specific points Aquinas treats rather than repeating the generic Summa description.
    private static string BuildSeoDescription(SummaQuestionContent question, string partTitle)
    {
        if (question.IsPrologue)
        {
            return $"{partTitle} \u00B7 Prologue: {question.Title}. From St. Thomas Aquinas's Summa Theologiae.";
        }

        var articles = string.Join("; ", question.Articles.Select(a => a.Title));
        return string.IsNullOrWhiteSpace(articles)
            ? $"{partTitle} \u00B7 Question {question.Number}: {question.Title}. From St. Thomas Aquinas's Summa Theologiae."
            : $"{question.Title} \u2014 St. Thomas Aquinas asks: {articles}.";
    }
}
