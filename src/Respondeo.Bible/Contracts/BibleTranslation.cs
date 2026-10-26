namespace Respondeo.Bible.Contracts;

using Respondeo.Bible;

/// <summary>A bundled English translation of the Bible.</summary>
public enum BibleTranslation
{
    /// <summary>The Douay-Rheims Bible, Challoner revision (the default).</summary>
    DouayRheims,

    /// <summary>The Catholic Public Domain Version (Ronald L. Conte Jr.); unofficial, without an imprimatur.</summary>
    Cpdv,
}

/// <summary>Display names, route codes, and data folders for each <see cref="BibleTranslation"/>.</summary>
public static class BibleTranslations
{
    /// <summary>Every bundled translation, the default first.</summary>
    public static readonly IReadOnlyList<BibleTranslation> All = [BibleTranslation.DouayRheims, BibleTranslation.Cpdv];

    /// <summary>The short code used in route segments and stored preferences.</summary>
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

    /// <summary>Whether the translation's manifest includes the book.</summary>
    public static bool Contains(this BibleTranslation translation, BibleBook book) => BibleCatalog.Entry(translation, book) is not null;

    /// <summary>The number of chapters the translation has for the book (0 when the book is absent).</summary>
    public static int ChapterCount(this BibleTranslation translation, BibleBook book) =>
        BibleCatalog.Entry(translation, book) is { } entry ? entry.Chapters ?? book.ChapterCount : 0;

    /// <summary>The book's name as the translation's manifest titles it.</summary>
    public static string BookName(this BibleTranslation translation, BibleBook book) =>
        BibleCatalog.Entry(translation, book)?.Name ?? book.DisplayName;

    /// <summary>
    /// The common modern name to show alongside a title that differs from it (e.g. "3 Kings" is 1 Kings, "Josue" is Joshua), or <c>null</c> when the names match.
    /// </summary>
    public static string? BookNameClarification(this BibleTranslation translation, BibleBook book) =>
        string.Equals(translation.BookName(book), book.DisplayName, StringComparison.Ordinal) ? null : book.DisplayName;

    /// <summary>The books the translation includes, in canonical order.</summary>
    public static IReadOnlyList<BibleBook> Books(this BibleTranslation translation) => BibleBooks.All.Where(b => translation.Contains(b)).ToList();

    /// <summary>Parses a <see cref="Code"/>, returning <c>null</c> when it is unknown.</summary>
    public static BibleTranslation? Parse(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "cpdv" => BibleTranslation.Cpdv,
        "drb" or "dr" or "douay-rheims" => BibleTranslation.DouayRheims,
        _ => null,
    };

    /// <summary>The book's route slug in this translation, derived from the name its manifest gives the book.</summary>
    public static string Slug(this BibleTranslation translation, BibleBook book) =>
        translation.BookName(book).ToLowerInvariant().Replace(' ', '-');

    /// <summary>Finds a book by its route slug in this translation, or <c>null</c> when the slug is unknown.</summary>
    public static BibleBook? FindBook(this BibleTranslation translation, string? slug) =>
        BibleBooks.All.FirstOrDefault(b => string.Equals(translation.Slug(b), slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>The relative link to a book's chapter in this translation, optionally selecting verses.</summary>
    public static string ChapterHref(this BibleTranslation translation, BibleBook book, int chapter, string? verses = null) =>
        $"bible/{translation.Code()}/{translation.Slug(book)}/{chapter}" + (string.IsNullOrEmpty(verses) ? "" : $"?v={verses}");
}
