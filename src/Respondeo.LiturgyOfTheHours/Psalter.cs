namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// The four-week psalter of the post-1970 Liturgy of the Hours: psalm and canticle citations for Morning, Evening, and Night Prayer.
/// Only citations are stored here; the copyrighted translations are read from official sources.
/// </summary>
internal static class Psalter
{
    private const string BenedictusSunday = "Daniel 3:57-88, 56";
    private const string EveningSaturday = "Philippians 2:6-11";
    private const string EveningSunday = "Revelation 19:1-7";
    private const string EveningMonday = "Ephesians 1:3-10";
    private const string EveningTuesday = "Revelation 4:11; 5:9, 10, 12";
    private const string EveningWednesday = "Colossians 1:12-20";
    private const string EveningThursday = "Revelation 11:17-18; 12:10b-12a";
    private const string EveningFriday = "Revelation 15:3-4";

    // [week - 1][(int)DayOfWeek] -> psalm, canticle, psalm
    private static readonly string[][][] Morning =
    [
        [
            ["Psalm 63:2-9", BenedictusSunday, "Psalm 149"],
            ["Psalm 5:2-10, 12-13", "1 Chronicles 29:10-13", "Psalm 29"],
            ["Psalm 24", "Tobit 13:1-8", "Psalm 33"],
            ["Psalm 36", "Judith 16:2-3a, 13-15", "Psalm 47"],
            ["Psalm 57", "Jeremiah 31:10-14", "Psalm 48"],
            ["Psalm 51", "Isaiah 45:15-25", "Psalm 100"],
            ["Psalm 119:145-152", "Exodus 15:1-4a, 8-13, 17-18", "Psalm 117"],
        ],
        [
            ["Psalm 118", "Daniel 3:52-57", "Psalm 150"],
            ["Psalm 42", "Sirach 36:1-5, 10-13", "Psalm 19:2-7"],
            ["Psalm 43", "Isaiah 38:10-14, 17-20", "Psalm 65"],
            ["Psalm 77", "1 Samuel 2:1-10", "Psalm 97"],
            ["Psalm 80", "Isaiah 12:1-6", "Psalm 81"],
            ["Psalm 51", "Habakkuk 3:2-4, 13a, 15-19", "Psalm 147:12-20"],
            ["Psalm 92", "Deuteronomy 32:1-12", "Psalm 8"],
        ],
        [
            ["Psalm 93", BenedictusSunday, "Psalm 148"],
            ["Psalm 84", "Isaiah 2:2-5", "Psalm 96"],
            ["Psalm 85", "Isaiah 26:1-4, 7-9, 12", "Psalm 67"],
            ["Psalm 86", "Isaiah 33:13-16", "Psalm 98"],
            ["Psalm 87", "Isaiah 40:10-17", "Psalm 99"],
            ["Psalm 51", "Jeremiah 14:17-21", "Psalm 100"],
            ["Psalm 119:145-152", "Wisdom 9:1-6, 9-11", "Psalm 117"],
        ],
        [
            ["Psalm 118", "Daniel 3:52-57", "Psalm 150"],
            ["Psalm 90", "Isaiah 42:10-16", "Psalm 135:1-12"],
            ["Psalm 101", "Daniel 3:26-27, 29, 34-41", "Psalm 144:1-10"],
            ["Psalm 108", "Isaiah 61:10-62:5", "Psalm 146"],
            ["Psalm 143:1-11", "Isaiah 66:10-14a", "Psalm 147:1-11"],
            ["Psalm 51", "Tobit 13:8-11, 13-15", "Psalm 147:12-20"],
            ["Psalm 92", "Ezekiel 36:24-28", "Psalm 8"],
        ],
    ];

    // [week - 1][(int)DayOfWeek] -> psalm, psalm, canticle. Saturday is First Vespers of Sunday.
    private static readonly string[][][] Evening =
    [
        [
            ["Psalm 110:1-5, 7", "Psalm 114", EveningSunday],
            ["Psalm 11", "Psalm 15", EveningMonday],
            ["Psalm 20", "Psalm 21:2-8, 14", EveningTuesday],
            ["Psalm 27:1-6", "Psalm 27:7-14", EveningWednesday],
            ["Psalm 30", "Psalm 32", EveningThursday],
            ["Psalm 41", "Psalm 46", EveningFriday],
            ["Psalm 141:1-9", "Psalm 142", EveningSaturday],
        ],
        [
            ["Psalm 110:1-5, 7", "Psalm 115", EveningSunday],
            ["Psalm 45:2-10", "Psalm 45:11-18", EveningMonday],
            ["Psalm 49:1-13", "Psalm 49:14-21", EveningTuesday],
            ["Psalm 62", "Psalm 67", EveningWednesday],
            ["Psalm 72:1-11", "Psalm 72:12-19", EveningThursday],
            ["Psalm 116:1-9", "Psalm 121", EveningFriday],
            ["Psalm 119:105-112", "Psalm 16", EveningSaturday],
        ],
        [
            ["Psalm 110:1-5, 7", "Psalm 111", EveningSunday],
            ["Psalm 123", "Psalm 124", EveningMonday],
            ["Psalm 125", "Psalm 131", EveningTuesday],
            ["Psalm 126", "Psalm 127", EveningWednesday],
            ["Psalm 132:1-10", "Psalm 132:11-18", EveningThursday],
            ["Psalm 135:1-12", "Psalm 135:13-21", EveningFriday],
            ["Psalm 113", "Psalm 116:10-19", EveningSaturday],
        ],
        [
            ["Psalm 110:1-5, 7", "Psalm 112", EveningSunday],
            ["Psalm 136:1-9", "Psalm 136:10-26", EveningMonday],
            ["Psalm 137:1-6", "Psalm 138", EveningTuesday],
            ["Psalm 139:1-12", "Psalm 139:13-18, 23-24", EveningWednesday],
            ["Psalm 144:1-8", "Psalm 144:9-15", EveningThursday],
            ["Psalm 145:1-13", "Psalm 145:14-21", EveningFriday],
            ["Psalm 122", "Psalm 130", EveningSaturday],
        ],
    ];

    // [(int)DayOfWeek] -> psalms, short reading. Saturday is Night Prayer after First Vespers of Sunday.
    private static readonly (string[] Psalms, string Reading)[] Night =
    [
        (["Psalm 91"], "Revelation 22:4-5"),
        (["Psalm 86"], "1 Thessalonians 5:9-10"),
        (["Psalm 143:1-11"], "1 Peter 5:8-9a"),
        (["Psalm 31:1-6", "Psalm 130"], "Ephesians 4:26-27"),
        (["Psalm 16"], "1 Thessalonians 5:23"),
        (["Psalm 88"], "Jeremiah 14:9a"),
        (["Psalm 4", "Psalm 134"], "Deuteronomy 6:4-7"),
    ];

    public static IReadOnlyList<string> MorningPsalmody(int week, DayOfWeek day) => Morning[week - 1][(int)day];

    public static IReadOnlyList<string> EveningPsalmody(int week, DayOfWeek day) => Evening[week - 1][(int)day];

    public static (IReadOnlyList<string> Psalms, string Reading) NightPsalmody(DayOfWeek day) => Night[(int)day];

    public static bool IsCanticle(string citation) => !citation.StartsWith("Psalm ", StringComparison.Ordinal);

    /// <summary>Converts a modern (Hebrew) psalm citation into the bundled translation's numbering for display.</summary>
    public static string? DouayCitation(string citation) => Respondeo.Bible.BibleReferences.TranslationCitation(citation);
}
