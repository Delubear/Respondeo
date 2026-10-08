using Respondeo.Content.Contracts;

namespace Respondeo.Content.Saints;

/// <summary>
/// Maps the internal saint domain models onto the public <see cref="SaintRecord"/> / <see cref="SaintIndex"/>
/// contract DTOs at the service boundary, so consumers never depend on the parsing/domain types.
/// </summary>
internal static class SaintMapper
{
    public static SaintRecord ToContract(this SaintRecordDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Era = document.Era,
        Region = document.Region,
        Patronages = document.Patronages,
        StatesOfLife = document.StatesOfLife,
        Canonizations = document.Canonizations,
        Dates = document.Dates,
        FeastDay = document.FeastDay,
        Tags = document.Tags,
        IsUnvetted = document.IsUnvetted,
        BodyHtml = document.BodyHtml,
        Sources = [.. document.Sources.Select(s => new SaintSource { Label = s.Label, Url = s.Url })],
    };

    public static SaintIndexEntry ToIndexEntryContract(this SaintRecordDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Era = document.Era,
        Region = document.Region,
        Patronages = document.Patronages,
        StatesOfLife = document.StatesOfLife,
        Canonizations = document.Canonizations,
        Dates = document.Dates,
        Tags = document.Tags,
    };
}
