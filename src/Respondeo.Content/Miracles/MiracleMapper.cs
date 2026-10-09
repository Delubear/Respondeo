using Respondeo.Content.Contracts;

namespace Respondeo.Content.Miracles;

/// <summary>
/// Maps the internal miracle domain models onto the public
/// <see cref="MiracleRecord"/> / <see cref="MiracleIndex"/> contract DTOs at the
/// service boundary, so consumers never depend on the parsing/domain types.
/// </summary>
internal static class MiracleMapper
{
    public static MiracleRecord ToContract(this MiracleRecordDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Types = document.Types,
        Approval = document.Approval,
        Region = document.Region,
        Country = document.Country,
        Year = document.Year,
        FeastDay = document.FeastDay,
        Tags = document.Tags,
        IsUnvetted = document.IsUnvetted,
        BodyHtml = document.BodyHtml,
        Sources = [.. document.Sources.Select(s => new MiracleSource { Label = s.Label, Url = s.Url })],
    };

    public static MiracleIndexEntry ToIndexEntryContract(this MiracleRecordDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Types = document.Types,
        Approval = document.Approval,
        Region = document.Region,
        Country = document.Country,
        Year = document.Year,
        Tags = document.Tags,
        IsUnvetted = document.IsUnvetted,
    };
}
