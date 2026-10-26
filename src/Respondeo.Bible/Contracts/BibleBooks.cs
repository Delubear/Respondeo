namespace Respondeo.Bible.Contracts;

/// <summary>A book, identified by its key in each translation's <c>manifest.json</c> (e.g. <c>isaiah</c>).</summary>
/// <remarks>Everything else about the book (order, names, chapters, testament) comes from the translation's manifest.</remarks>
public sealed record BibleBook(string Slug);

/// <summary>The section of the Bible a book belongs to, as declared by the manifest's <c>testament</c> field.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<BibleTestament>))]
public enum BibleTestament
{
    Old,
    New,

    /// <summary>The Clementine Vulgate appendix, printed after the New Testament; not part of the canon.</summary>
    Appendix,
}

/// <summary>A single numbered verse.</summary>
public sealed record BibleVerse(int Number, string Text);
