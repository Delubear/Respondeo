namespace Respondeo.Content.Summa;

/// <summary>
/// The public contract for reading the bundled Summa Theologica. Implementations own loading, parsing,
/// and caching; consumers depend only on this surface. The lightweight index is loaded once for browsing
/// and title/question search, while full question content is fetched on demand.
/// </summary>
public interface ISummaService
{
    /// <summary>Returns the browse/search index (parts, questions, article titles), loading it if needed.</summary>
    Task<SummaIndex> GetIndexAsync();

    /// <summary>Returns the full content of a single question by id, or null if it does not exist.</summary>
    Task<SummaQuestionContent?> GetQuestionAsync(string id);
}
