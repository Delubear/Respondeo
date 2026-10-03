using Respondeo.Content.Contracts;

namespace Respondeo.Content.Devotions;

/// <summary>
/// Maps the internal <see cref="DevotionDocument"/> domain model onto the public
/// <see cref="Devotion"/> / <see cref="DevotionSummary"/> contract DTOs at the service boundary.
/// </summary>
internal static class DevotionMapper
{
    public static Devotion ToContract(this DevotionDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Kind = document.Kind,
        IntroHtml = document.IntroHtml,
        LatinAvailable = document.LatinAvailable,
        MysterySets = [.. document.MysterySets.Select(ToContract)],
        Sequence = [.. document.Sequence.Select(ToContract)],
    };

    public static DevotionSummary ToSummaryContract(this DevotionDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Kind = document.Kind,
    };

    private static MysterySet ToContract(MysterySetDocument set) => new()
    {
        Id = set.Id,
        Name = set.Name,
        When = set.When,
        Summary = set.Summary,
        Mysteries = [.. set.Mysteries.Select(m => new Mystery { Title = m.Title, ReflectionHtml = m.ReflectionHtml })],
    };

    private static DevotionStep ToContract(DevotionStepDocument step) => new()
    {
        Kind = step.Kind,
        Title = step.Title,
        PrayerId = step.PrayerId,
        Repeat = step.Repeat,
        Bead = step.Bead,
        Note = step.Note,
        NoteLatin = step.NoteLatin,
        PerMystery = [.. step.PerMystery.Select(ToContract)],
    };
}
