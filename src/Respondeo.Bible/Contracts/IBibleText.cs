namespace Respondeo.Bible.Contracts;

/// <summary>
/// The public contract for reading the bundled Bible translations.
/// Resolves modern (Hebrew-numbered) citations to passage text and loads whole chapters.
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// </summary>
public interface IBibleText
{
    /// <summary>Returns the passage text, one verse per line, or <c>null</c> when it cannot be resolved.</summary>
    Task<string?> GetPassageAsync(string citation);

    /// <summary>
    /// Returns every verse of one chapter of a book (see <see cref="BibleBooks"/>) in the chosen translation,
    /// or <c>null</c> when it cannot be loaded or that translation lacks the chapter.
    /// </summary>
    Task<IReadOnlyList<BibleVerse>?> GetChapterAsync(string bookSlug, int chapter, BibleTranslation translation = BibleTranslation.DouayRheims);

    /// <summary>The book abbreviations declared in the translation's manifest, keyed by modern slug (empty when it cannot be loaded).</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAbbreviationsAsync(BibleTranslation translation);
}
