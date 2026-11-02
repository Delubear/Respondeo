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
