using Respondeo.Bible.Contracts;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.Services;

/// <summary>The Bible text resolved for an office: per-step passages and per-option passages or stanzas.</summary>
public sealed record HourPassages(
    IReadOnlyDictionary<int, string> Passages,
    IReadOnlyDictionary<(int Step, int Option), string> OptionPassages,
    IReadOnlyDictionary<(int Step, int Option), List<string>> OptionStanzas)
{
    public static HourPassages Empty { get; } = new(
        new Dictionary<int, string>(),
        new Dictionary<(int Step, int Option), string>(),
        new Dictionary<(int Step, int Option), List<string>>());
}

/// <summary>Loads the Douay-Rheims text for every step and option of an office that has no inline text.</summary>
public sealed class HourPassageLoader(IBibleText bible)
{
    public const string GloryBe =
        "Glory be to the Father, and to the Son, * and to the Holy Spirit,\nas it was in the beginning, is now, * and ever shall be,\nworld without end. Amen.";

    public async Task<HourPassages> LoadAsync(Office office)
    {
        var passages = new Dictionary<int, string>();
        var optionPassages = new Dictionary<(int Step, int Option), string>();
        var optionStanzas = new Dictionary<(int Step, int Option), List<string>>();
        for (var i = 0; i < office.Steps.Count; i++)
        {
            var step = office.Steps[i];
            if (step.Text is null && await LoadPassageAsync(step) is { } text)
            {
                passages[i] = text;
            }

            if (step.Options is not { } stepOptions)
            {
                continue;
            }

            for (var j = 0; j < stepOptions.Count; j++)
            {
                var option = stepOptions[j];
                if (option.Text is null && option.Passage is null && option.Stanzas is { Count: > 0 } stanzaRefs)
                {
                    List<string>? texts = [];
                    foreach (var stanzaRef in stanzaRefs)
                    {
                        var stanzaText = await bible.GetPassageAsync(stanzaRef.Citation);
                        if (string.IsNullOrWhiteSpace(stanzaText))
                        {
                            texts = null;
                            break;
                        }

                        texts.Add(stanzaText);
                    }

                    if (texts is not null)
                    {
                        optionStanzas[(i, j)] = texts;
                    }
                    continue;
                }

                if (option.Text is null && await LoadPassageAsync(option) is { } optionText)
                {
                    optionPassages[(i, j)] = optionText;
                }
            }
        }

        return new HourPassages(passages, optionPassages, optionStanzas);
    }

    private async Task<string?> LoadPassageAsync(OfficeStep step)
    {
        if (step.Passage is { } passage)
        {
            if (passage.Parts.Count == 0 || string.IsNullOrWhiteSpace(passage.Translation))
            {
                throw new InvalidDataException($"The liturgical passage '{step.Title}' needs parts and a translation label.");
            }

            var parts = new List<string>(passage.Parts.Count);
            foreach (var part in passage.Parts)
            {
                if (string.IsNullOrWhiteSpace(part.Citation) == string.IsNullOrWhiteSpace(part.Text))
                {
                    throw new InvalidDataException($"Each part of '{step.Title}' must supply either a citation or text, not both.");
                }

                var text = part.Citation is { } citation ? await bible.GetPassageAsync(citation) : part.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                parts.Add(text);
            }

            return string.Join("\n\n", parts);
        }

        if (step.Reference is not { } reference)
        {
            return null;
        }

        if (await bible.GetPassageAsync(reference.Citation) is not { } body)
        {
            return null;
        }

        return step.Kind == OfficeStepKind.Reading ? body : body + "\n\n" + GloryBe;
    }
}
