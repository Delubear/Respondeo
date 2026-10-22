namespace Respondeo.Bible;

/// <summary>A book of the Douay-Rheims Bible.</summary>
/// <param name="Slug">The route and data-file identifier (e.g. <c>isaie</c>).</param>
/// <param name="Name">The Douay-Rheims display name.</param>
/// <param name="ModernName">The common modern name when it differs from <paramref name="Name"/>.</param>
/// <param name="Testament">Old or New Testament.</param>
/// <param name="ChapterCount">The number of chapters in the bundled text.</param>
public sealed record BibleBook(string Slug, string Name, string? ModernName, BibleTestament Testament, int ChapterCount);

/// <summary>The two testaments of the canon.</summary>
public enum BibleTestament
{
    Old,
    New,
}

/// <summary>A single numbered verse.</summary>
public sealed record BibleVerse(int Number, string Text);

/// <summary>The 73 books of the Catholic canon in Douay-Rheims order.</summary>
public static class BibleBooks
{
    public static readonly IReadOnlyList<BibleBook> All =
    [
        Old("genesis", "Genesis", null, 50),
        Old("exodus", "Exodus", null, 40),
        Old("leviticus", "Leviticus", null, 27),
        Old("numbers", "Numbers", null, 36),
        Old("deuteronomy", "Deuteronomy", null, 34),
        Old("josue", "Josue", "Joshua", 24),
        Old("judges", "Judges", null, 21),
        Old("ruth", "Ruth", null, 4),
        Old("1-kings", "1 Kings", "1 Samuel", 31),
        Old("2-kings", "2 Kings", "2 Samuel", 24),
        Old("3-kings", "3 Kings", "1 Kings", 22),
        Old("4-kings", "4 Kings", "2 Kings", 25),
        Old("1-paralipomenon", "1 Paralipomenon", "1 Chronicles", 29),
        Old("2-paralipomenon", "2 Paralipomenon", "2 Chronicles", 36),
        Old("1-esdras", "1 Esdras", "Ezra", 10),
        Old("2-esdras", "2 Esdras", "Nehemiah", 13),
        Old("tobias", "Tobias", "Tobit", 15),
        Old("judith", "Judith", null, 16),
        Old("esther", "Esther", null, 16),
        Old("job", "Job", null, 42),
        Old("psalms", "Psalms", null, 150),
        Old("proverbs", "Proverbs", null, 31),
        Old("ecclesiastes", "Ecclesiastes", null, 12),
        Old("canticle-of-canticles", "Canticle of Canticles", "Song of Songs", 8),
        Old("wisdom", "Wisdom", null, 19),
        Old("ecclesiasticus", "Ecclesiasticus", "Sirach", 51),
        Old("isaie", "Isaias", "Isaiah", 66),
        Old("jeremie", "Jeremias", "Jeremiah", 52),
        Old("lamentations", "Lamentations", null, 5),
        Old("baruch", "Baruch", null, 6),
        Old("ezechiel", "Ezechiel", "Ezekiel", 48),
        Old("daniel", "Daniel", null, 14),
        Old("osee", "Osee", "Hosea", 14),
        Old("joel", "Joel", null, 3),
        Old("amos", "Amos", null, 9),
        Old("abdias", "Abdias", "Obadiah", 1),
        Old("jonas", "Jonas", "Jonah", 4),
        Old("micheas", "Micheas", "Micah", 7),
        Old("nahum", "Nahum", null, 3),
        Old("habacuc", "Habacuc", "Habakkuk", 3),
        Old("sophonias", "Sophonias", "Zephaniah", 3),
        Old("aggeus", "Aggeus", "Haggai", 2),
        Old("zacharias", "Zacharias", "Zechariah", 14),
        Old("malachie", "Malachias", "Malachi", 4),
        Old("1-machabees", "1 Machabees", "1 Maccabees", 16),
        Old("2-machabees", "2 Machabees", "2 Maccabees", 15),
        New("matthew", "Matthew", 28),
        New("mark", "Mark", 16),
        New("luke", "Luke", 24),
        New("john", "John", 21),
        New("acts", "Acts", 28),
        New("romans", "Romans", 16),
        New("1-corinthians", "1 Corinthians", 16),
        New("2-corinthians", "2 Corinthians", 13),
        New("galatians", "Galatians", 6),
        New("ephesians", "Ephesians", 6),
        New("philippians", "Philippians", 4),
        New("colossians", "Colossians", 4),
        New("1-thessalonians", "1 Thessalonians", 5),
        New("2-thessalonians", "2 Thessalonians", 3),
        New("1-timothy", "1 Timothy", 6),
        New("2-timothy", "2 Timothy", 4),
        New("titus", "Titus", 3),
        New("philemon", "Philemon", 1),
        New("hebrews", "Hebrews", 13),
        New("james", "James", 5),
        New("1-peter", "1 Peter", 5),
        New("2-peter", "2 Peter", 3),
        New("1-john", "1 John", 5),
        New("2-john", "2 John", 1),
        New("3-john", "3 John", 1),
        New("jude", "Jude", 1),
        New("apocalypse", "Apocalypse", "Revelation", 22),
    ];

    /// <summary>Finds a book by its slug, or <c>null</c> when the slug is unknown.</summary>
    public static BibleBook? Find(string? slug) =>
        All.FirstOrDefault(b => string.Equals(b.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private static BibleBook Old(string slug, string name, string? modern, int chapters) =>
        new(slug, name, modern, BibleTestament.Old, chapters);

    private static BibleBook New(string slug, string name, int chapters) =>
        new(slug, name, null, BibleTestament.New, chapters);

    private static BibleBook New(string slug, string name, string? modern, int chapters) =>
        new(slug, name, modern, BibleTestament.New, chapters);
}
