namespace Respondeo.Bible.Contracts;

/// <summary>Translation-aware helpers for displaying citations alongside the bundled Bible text.</summary>
public static class BibleReferences
{
    /// <summary>
    /// Returns the citation as numbered in the bundled translation (for example a psalm in Vulgate numbering),
    /// or <c>null</c> when it is identical to the modern citation.
    /// </summary>
    public static string? TranslationCitation(string citation)
    {
        if (!citation.StartsWith("Psalm ", StringComparison.Ordinal))
        {
            return null;
        }

        var rest = citation["Psalm ".Length..];
        var colon = rest.IndexOf(':');
        if (!int.TryParse(colon < 0 ? rest : rest[..colon], out var number))
        {
            return null;
        }

        var douay = number switch
        {
            <= 8 => number,
            9 or 10 => 9,
            <= 113 => number - 1,
            114 or 115 => 113,
            116 => 114,
            <= 146 => number - 1,
            147 => 146,
            _ => number,
        };

        return douay == number ? null : $"Psalm {douay} (Douay-Rheims)";
    }
}
