namespace Respondeo.Bible.Contracts;

/// <summary>
/// The result of loading a chapter. <see cref="Unavailable"/> is true when the requested translation
/// lacks the chapter but another translation has it.
/// </summary>
public sealed record BibleChapterResult(IReadOnlyList<BibleVerse>? Verses, bool Unavailable)
{
    public static BibleChapterResult NotFound { get; } = new(null, false);
}

/// <summary>Loads a chapter for display, detecting when it exists only in another translation.</summary>
public interface IBibleChapterReader
{
    Task<BibleChapterResult> LoadAsync(BibleBook? book, int chapter, BibleTranslation translation);
}
