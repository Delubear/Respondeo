namespace Respondeo.Bible;

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

    /// <summary>Parses a <see cref="Code"/>, returning <c>null</c> when it is missing or unknown.</summary>
    public static BibleTranslation? Parse(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "cpdv" => BibleTranslation.Cpdv,
        "drb" or "dr" or "douay-rheims" => BibleTranslation.DouayRheims,
        _ => null,
    };
}
