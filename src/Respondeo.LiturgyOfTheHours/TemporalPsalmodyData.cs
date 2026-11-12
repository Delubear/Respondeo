using System.Collections.Frozen;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>Authored citation assignments, independent of the rules selecting the Office day.</summary>
internal sealed record TemporalPsalmodyData(
    IReadOnlyDictionary<int, TemporalDayPsalmody> Triduum,
    string LentenSundayEveningCanticle)
{
    internal TemporalPsalmodyData Validate(string source)
    {
        if (Triduum is null || !Triduum.Keys.OrderBy(offset => offset).SequenceEqual(new[] { -3, -2, -1, 0 }))
        {
            throw Invalid(source, "Triduum must define exactly the Easter offsets -3, -2, -1, and 0");
        }

        if (string.IsNullOrWhiteSpace(LentenSundayEveningCanticle) || !Psalter.IsCanticle(LentenSundayEveningCanticle))
        {
            throw Invalid(source, "Lenten Sunday Evening Prayer requires a canticle citation");
        }

        var frozen = new Dictionary<int, TemporalDayPsalmody>();
        foreach (var (offset, day) in Triduum)
        {
            if (day is null)
            {
                throw Invalid(source, $"Triduum offset {offset} requires an assignment");
            }

            if (offset == -3 && day.Morning is not null)
            {
                throw Invalid(source, "Holy Thursday morning must retain the Holy Week psalter");
            }

            var morning = offset == -3 ? null : ValidatePsalmody(day.Morning, morning: true, offset);
            var evening = ValidatePsalmody(day.Evening, morning: false, offset);
            frozen.Add(offset, new(morning, evening));
        }

        return this with { Triduum = frozen.ToFrozenDictionary() };

        IReadOnlyList<string> ValidatePsalmody(IReadOnlyList<string>? citations, bool morning, int offset)
        {
            var hour = morning ? "morning" : "evening";
            if (citations is null || citations.Count != 3 || citations.Any(string.IsNullOrWhiteSpace))
            {
                throw Invalid(source, $"Triduum offset {offset} {hour} requires exactly three nonblank citations");
            }

            var canticleIndex = morning ? 1 : 2;
            if (citations.Where((citation, index) => Psalter.IsCanticle(citation) != (index == canticleIndex)).Any())
            {
                throw Invalid(source, $"Triduum offset {offset} {hour} requires its canticle at position {canticleIndex + 1} and psalms elsewhere");
            }

            return Array.AsReadOnly(citations.ToArray());
        }
    }

    private static InvalidOperationException Invalid(string source, string message) =>
        new($"Invalid temporal psalmody data in '{source}': {message}.");
}

internal sealed record TemporalDayPsalmody(IReadOnlyList<string>? Morning = null, IReadOnlyList<string>? Evening = null);
