namespace Respondeo.Bible.Contracts;

/// <summary>A book of the Douay-Rheims Bible.</summary>
/// <param name="FileSlug">The Douay-Rheims data-file identifier (e.g. <c>isaie</c>).</param>
/// <param name="Name">The Douay-Rheims display name.</param>
/// <param name="ModernName">The common modern name when it differs from <paramref name="Name"/>.</param>
/// <param name="Testament">Old or New Testament.</param>
public sealed record BibleBook(string FileSlug, string Name, string? ModernName, BibleTestament Testament)
{
    /// <summary>The route identifier, derived from the modern name (e.g. <c>isaiah</c>).</summary>
    public string Slug => (ModernName ?? Name).ToLowerInvariant().Replace(' ', '-');

    /// <summary>The name shown to readers: the common modern name, falling back to the Douay-Rheims name.</summary>
    public string DisplayName => ModernName ?? Name;

    /// <summary>The Douay-Rheims name when it differs from <see cref="DisplayName"/>, otherwise <c>null</c>.</summary>
    public string? DouayName => ModernName is null ? null : Name;
}

/// <summary>The two testaments of the canon.</summary>
public enum BibleTestament
{
    Old,
    New,

    /// <summary>The Clementine Vulgate appendix, printed after the New Testament; not part of the canon.</summary>
    Appendix,
}

/// <summary>A single numbered verse.</summary>
public sealed record BibleVerse(int Number, string Text);

/// <summary>The 73 books of the Catholic canon in Douay-Rheims order, followed by the non-canonical Vulgate appendix.</summary>
public static class BibleBooks
{
    public static readonly IReadOnlyList<BibleBook> All =
    [
        Old("genesis", "Genesis", null),
        Old("exodus", "Exodus", null),
        Old("leviticus", "Leviticus", null),
        Old("numbers", "Numbers", null),
        Old("deuteronomy", "Deuteronomy", null),
        Old("josue", "Josue", "Joshua"),
        Old("judges", "Judges", null),
        Old("ruth", "Ruth", null),
        Old("1-kings", "1 Kings", "1 Samuel"),
        Old("2-kings", "2 Kings", "2 Samuel"),
        Old("3-kings", "3 Kings", "1 Kings"),
        Old("4-kings", "4 Kings", "2 Kings"),
        Old("1-paralipomenon", "1 Paralipomenon", "1 Chronicles"),
        Old("2-paralipomenon", "2 Paralipomenon", "2 Chronicles"),
        Old("1-esdras", "1 Esdras", "Ezra"),
        Old("2-esdras", "2 Esdras", "Nehemiah"),
        Old("tobias", "Tobias", "Tobit"),
        Old("judith", "Judith", null),
        Old("esther", "Esther", null),
        Old("job", "Job", null),
        Old("psalms", "Psalms", null),
        Old("proverbs", "Proverbs", null),
        Old("ecclesiastes", "Ecclesiastes", null),
        Old("canticle-of-canticles", "Canticle of Canticles", "Song of Songs"),
        Old("wisdom", "Wisdom", null),
        Old("ecclesiasticus", "Ecclesiasticus", "Sirach"),
        Old("isaie", "Isaias", "Isaiah"),
        Old("jeremie", "Jeremias", "Jeremiah"),
        Old("lamentations", "Lamentations", null),
        Old("baruch", "Baruch", null),
        Old("ezechiel", "Ezechiel", "Ezekiel"),
        Old("daniel", "Daniel", null),
        Old("osee", "Osee", "Hosea"),
        Old("joel", "Joel", null),
        Old("amos", "Amos", null),
        Old("abdias", "Abdias", "Obadiah"),
        Old("jonas", "Jonas", "Jonah"),
        Old("micheas", "Micheas", "Micah"),
        Old("nahum", "Nahum", null),
        Old("habacuc", "Habacuc", "Habakkuk"),
        Old("sophonias", "Sophonias", "Zephaniah"),
        Old("aggeus", "Aggeus", "Haggai"),
        Old("zacharias", "Zacharias", "Zechariah"),
        Old("malachie", "Malachias", "Malachi"),
        Old("1-machabees", "1 Machabees", "1 Maccabees"),
        Old("2-machabees", "2 Machabees", "2 Maccabees"),
        New("matthew", "Matthew"),
        New("mark", "Mark"),
        New("luke", "Luke"),
        New("john", "John"),
        New("acts", "Acts"),
        New("romans", "Romans"),
        New("1-corinthians", "1 Corinthians"),
        New("2-corinthians", "2 Corinthians"),
        New("galatians", "Galatians"),
        New("ephesians", "Ephesians"),
        New("philippians", "Philippians"),
        New("colossians", "Colossians"),
        New("1-thessalonians", "1 Thessalonians"),
        New("2-thessalonians", "2 Thessalonians"),
        New("1-timothy", "1 Timothy"),
        New("2-timothy", "2 Timothy"),
        New("titus", "Titus"),
        New("philemon", "Philemon"),
        New("hebrews", "Hebrews"),
        New("james", "James"),
        New("1-peter", "1 Peter"),
        New("2-peter", "2 Peter"),
        New("1-john", "1 John"),
        New("2-john", "2 John"),
        New("3-john", "3 John"),
        New("jude", "Jude"),
        New("apocalypse", "Apocalypse", "Revelation"),
        Appendix("prayer-of-manasses", "Prayer of Manasses"),
        Appendix("3-esdras", "3 Esdras"),
        Appendix("4-esdras", "4 Esdras"),
    ];

    /// <summary>Finds a book by its slug, or <c>null</c> when the slug is unknown.</summary>
    public static BibleBook? Find(string? slug) =>
        All.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase)) ??
        All.FirstOrDefault(b => string.Equals(b.FileSlug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds a book by its Douay-Rheims data-file slug.</summary>
    public static BibleBook? FindByFileSlug(string? slug) =>
        All.FirstOrDefault(b => string.Equals(b.FileSlug, slug, StringComparison.OrdinalIgnoreCase));

    private static BibleBook Old(string slug, string name, string? modern) => new(slug, name, modern, BibleTestament.Old);

    private static BibleBook New(string slug, string name) => new(slug, name, null, BibleTestament.New);

    private static BibleBook New(string slug, string name, string? modern) => new(slug, name, modern, BibleTestament.New);

    private static BibleBook Appendix(string slug, string name) => new(slug, name, null, BibleTestament.Appendix);
}
