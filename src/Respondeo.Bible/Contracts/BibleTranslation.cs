namespace Respondeo.Bible.Contracts;

/// <summary>A bundled English translation of the Bible.</summary>
public enum BibleTranslation
{
    /// <summary>The Douay-Rheims Bible, Challoner revision (the default).</summary>
    DouayRheims,

    /// <summary>The Catholic Public Domain Version (Ronald L. Conte Jr.); unofficial, without an imprimatur.</summary>
    Cpdv,
}

/// <summary>Display names, query codes, and data folders for each <see cref="BibleTranslation"/>.</summary>
public static class BibleTranslations
{
    /// <summary>Every bundled translation, the default first.</summary>
    public static readonly IReadOnlyList<BibleTranslation> All = [BibleTranslation.DouayRheims, BibleTranslation.Cpdv];

    /// <summary>The short code used in <c>?tr=</c> links and stored preferences.</summary>
    public static string Code(this BibleTranslation translation) => translation switch
    {
        BibleTranslation.Cpdv => "cpdv",
        _ => "drb",
    };

    /// <summary>The full display name.</summary>
    public static string Name(this BibleTranslation translation) => translation switch
    {
        BibleTranslation.Cpdv => "Catholic Public Domain Version",
        _ => "Douay-Rheims",
    };

    /// <summary>The compact label shown in the translation toggle.</summary>
    public static string ShortName(this BibleTranslation translation) => translation switch
    {
        BibleTranslation.Cpdv => "CPDV",
        _ => "Douay-Rheims",
    };

    /// <summary>The wwwroot folder holding the per-book JSON files.</summary>
    internal static string Folder(this BibleTranslation translation) => translation switch
    {
        BibleTranslation.Cpdv => "cpdv",
        _ => "douay-rheims",
    };

    private static readonly HashSet<string> CpdvMissingBooks = ["3-esdras", "4-esdras", "prayer-of-manasses"];

    private static readonly Dictionary<string, int> CpdvChapterCounts = new()
    {
        ["esther"] = 15,
        ["tobias"] = 14,
    };

    /// <summary>Whether the translation includes the book at all.</summary>
    public static bool Contains(this BibleTranslation translation, BibleBook book) => translation != BibleTranslation.Cpdv || !CpdvMissingBooks.Contains(book.FileSlug);

    /// <summary>The number of chapters the translation has for the book (0 when the book is absent).</summary>
    public static int ChapterCount(this BibleTranslation translation, BibleBook book)
    {
        if (!translation.Contains(book)) return 0;
        return translation == BibleTranslation.Cpdv && CpdvChapterCounts.TryGetValue(book.FileSlug, out var count) ? count : book.ChapterCount;
    }

    /// <summary>The book's name as the translation titles it: the Douay name for Douay-Rheims, the common modern name otherwise.</summary>
    public static string BookName(this BibleTranslation translation, BibleBook book) => translation == BibleTranslation.DouayRheims ? book.Name : book.DisplayName;

    /// <summary>
    /// The common modern name to show alongside a Douay-Rheims title that differs from it (e.g. "3 Kings" is 1 Kings, "Josue" is Joshua), or <c>null</c> when the names match.
    /// </summary>
    public static string? BookNameClarification(this BibleTranslation translation, BibleBook book) => translation == BibleTranslation.DouayRheims ? book.ModernName : null;

    /// <summary>The books the translation includes, in canonical order.</summary>
    public static IReadOnlyList<BibleBook> Books(this BibleTranslation translation) => BibleBooks.All.Where(b => translation.Contains(b)).ToList();

    /// <summary>Parses a <see cref="Code"/>, returning
    public static BibleTranslation? Parse(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "cpdv" => BibleTranslation.Cpdv,
        "drb" or "dr" or "douay-rheims" => BibleTranslation.DouayRheims,
        _ => null,
    };
}
