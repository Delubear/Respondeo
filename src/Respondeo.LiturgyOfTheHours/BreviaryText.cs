namespace Respondeo.LiturgyOfTheHours;

/// <summary>The breviary marks that are set in red within office texts.</summary>
public enum BreviarySymbol
{
    Text,
    Versicle,
    Response,
    Mediant,
    Flexa,
    Cross,
}

public sealed record BreviarySegment(BreviarySymbol Kind, string Text);

/// <summary>
/// Splits office text into plain runs and breviary marks:
/// ℣ (versicle), ℟ (response), * (mediant pause), † (flexa) and ✠ (sign of the cross).
/// </summary>
public static class BreviaryText
{
    public const char Versicle = '\u2123';
    public const char Response = '\u211F';
    public const char Mediant = '*';
    public const char Flexa = '\u2020';
    public const char Cross = '\u2720';

    /// <summary>The source tokens that content JSON uses in place of the raw glyphs, mapped to the glyph each one renders as.</summary>
    public static readonly IReadOnlyDictionary<string, char> Tokens = new Dictionary<string, char>(StringComparer.Ordinal)
    {
        ["{{V}}"] = Versicle,
        ["{{R}}"] = Response,
        ["{{star}}"] = Mediant,
        ["{{dagger}}"] = Flexa,
        ["{{cross}}"] = Cross,
    };

    /// <summary>The glyphs that content JSON must spell as tokens rather than write directly.</summary>
    public static readonly IReadOnlyList<char> TokenOnlyGlyphs = [Versicle, Response, Flexa, Cross];

    /// <summary>Replaces each <c>{{V}}</c>, <c>{{R}}</c>, <c>{{star}}</c>, <c>{{dagger}}</c> and <c>{{cross}}</c> token with its glyph.</summary>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(text))]
    public static string? ExpandTokens(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
        {
            return text;
        }

        foreach (var (token, glyph) in Tokens)
        {
            text = text.Replace(token, glyph.ToString(), StringComparison.Ordinal);
        }

        return text;
    }

    public static BreviarySymbol Classify(char c) => c switch
    {
        Versicle => BreviarySymbol.Versicle,
        Response => BreviarySymbol.Response,
        Mediant => BreviarySymbol.Mediant,
        Flexa => BreviarySymbol.Flexa,
        Cross => BreviarySymbol.Cross,
        _ => BreviarySymbol.Text,
    };

    public static IReadOnlyList<BreviarySegment> Tokenize(string? text)
    {
        var segments = new List<BreviarySegment>();
        if (string.IsNullOrEmpty(text))
        {
            return segments;
        }

        text = ExpandTokens(text);
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var kind = Classify(text[i]);
            if (kind == BreviarySymbol.Text)
            {
                continue;
            }

            if (i > start)
            {
                segments.Add(new BreviarySegment(BreviarySymbol.Text, text[start..i]));
            }

            segments.Add(new BreviarySegment(kind, text[i].ToString()));
            start = i + 1;
        }

        if (start < text.Length)
        {
            segments.Add(new BreviarySegment(BreviarySymbol.Text, text[start..]));
        }

        return segments;
    }
}
