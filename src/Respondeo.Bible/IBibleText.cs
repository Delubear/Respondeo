namespace Respondeo.Bible;

/// <summary>Resolves modern (Hebrew-numbered) Bible citations to passage text in the bundled translation.</summary>
public interface IBibleText
{
    /// <summary>Returns the passage text, one verse per line, or <c>null</c> when it cannot be resolved.</summary>
    Task<string?> GetPassageAsync(string citation);

    /// <summary>Returns every verse of one chapter of a book (see <see cref="BibleBooks"/>), or <c>null</c> when it cannot be loaded.</summary>
    Task<IReadOnlyList<BibleVerse>?> GetChapterAsync(string bookSlug, int chapter);
}
