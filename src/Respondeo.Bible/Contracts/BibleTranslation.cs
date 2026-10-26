namespace Respondeo.Bible.Contracts;

/// <summary>
/// A bundled Bible translation, described entirely by data: its metadata comes from <c>translations.json</c>
/// and its books (names, chapter counts, abbreviations, files) from <c>{Folder}/manifest.json</c>.
/// </summary>
public sealed class BibleTranslation : IEquatable<BibleTranslation>
{
    private readonly IReadOnlyDictionary<string, BibleTranslationBook> _books;
    private readonly IReadOnlyList<BibleBook> _order;

    public BibleTranslation(
        string code,
        string name,
        string shortName,
        string folder,
        string? description,
        IReadOnlyList<string> aliases,
        IReadOnlyDictionary<string, BibleTranslationBook> books)
    {
        Code = code;
        Name = name;
        ShortName = shortName;
        Folder = folder;
        Description = description;
        Aliases = aliases;
        _books = books;
        _order = [.. books.Keys.Select(key => new BibleBook(key))];
    }

    /// <summary>The short code used in route segments and stored preferences.</summary>
    public string Code { get; }

    /// <summary>The full display name.</summary>
    public string Name { get; }

    /// <summary>The compact label shown in the translation toggle.</summary>
    public string ShortName { get; }

    /// <summary>The folder (under the Bible static assets) holding the manifest and per-book JSON files.</summary>
    public string Folder { get; }

    /// <summary>A short description of the translation's provenance, or <c>null</c>.</summary>
    public string? Description { get; }

    /// <summary>Additional codes that resolve to this translation.</summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>Whether <paramref name="code"/> is this translation's code or one of its aliases.</summary>
    public bool Matches(string? code)
    {
        var value = code?.Trim();
        return !string.IsNullOrEmpty(value) &&
            (string.Equals(Code, value, StringComparison.OrdinalIgnoreCase) ||
             Aliases.Any(a => string.Equals(a, value, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>The manifest entry for the book, or <c>null</c> when the translation lacks it.</summary>
    public BibleTranslationBook? Entry(BibleBook book) => _books.TryGetValue(book.Slug, out var entry) ? entry : null;

    /// <summary>Every book in the manifest, keyed by modern slug.</summary>
    public IReadOnlyDictionary<string, BibleTranslationBook> Manifest => _books;

    /// <summary>Whether the translation's manifest includes the book.</summary>
    public bool Contains(BibleBook book) => Entry(book) is not null;

    /// <summary>The number of chapters the translation has for the book (0 when the book is absent).</summary>
    public int ChapterCount(BibleBook book) => Entry(book)?.Chapters ?? 0;

    /// <summary>The book's name as the translation's manifest titles it.</summary>
    public string BookName(BibleBook book) => Entry(book)?.Name ?? book.Slug;

    /// <summary>
    /// The manifest's common modern name for a book whose title differs from it (e.g. "3 Kings" is 1 Kings), or <c>null</c>.
    /// </summary>
    public string? BookNameClarification(BibleBook book) => Entry(book)?.ModernName;

    /// <summary>The section of the Bible the manifest places the book in.</summary>
    public BibleTestament Testament(BibleBook book) => Entry(book)?.Testament ?? BibleTestament.Old;

    /// <summary>The books the translation includes, in manifest order.</summary>
    public IReadOnlyList<BibleBook> Books() => _order;

    /// <summary>The book's route slug in this translation, derived from the name its manifest gives the book.</summary>
    public string Slug(BibleBook book) => BookName(book).ToLowerInvariant().Replace(' ', '-');

    /// <summary>Finds a book by its route slug in this translation, or <c>null</c> when the slug is unknown.</summary>
    public BibleBook? FindBook(string? slug) =>
        _order.FirstOrDefault(b => string.Equals(Slug(b), slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>The relative link to a book's chapter in this translation, optionally selecting verses.</summary>
    public string ChapterHref(BibleBook book, int chapter, string? verses = null) =>
        $"bible/{Code}/{Slug(book)}/{chapter}" + (string.IsNullOrEmpty(verses) ? "" : $"?v={verses}");

    public bool Equals(BibleTranslation? other) =>
        other is not null && string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as BibleTranslation);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Code);

    public override string ToString() => Code;

    public static bool operator ==(BibleTranslation? left, BibleTranslation? right) => left?.Equals(right) ?? right is null;

    public static bool operator !=(BibleTranslation? left, BibleTranslation? right) => !(left == right);
}

/// <summary>One book's entry in a translation manifest.</summary>
public sealed record BibleTranslationBook(
    string? File,
    string? Name,
    string? ModernName,
    BibleTestament? Testament,
    int? Chapters,
    IReadOnlyList<string>? Abbreviations);
