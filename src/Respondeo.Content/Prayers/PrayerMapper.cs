using Respondeo.Content.Contracts;

namespace Respondeo.Content.Prayers;

/// <summary>
/// Maps the internal <see cref="PrayerDocument"/> domain model onto the public
/// <see cref="Prayer"/> / <see cref="PrayerSummary"/> contract DTOs at the service boundary.
/// </summary>
internal static class PrayerMapper
{
    public static Prayer ToContract(this PrayerDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Category = document.Category,
        Language = document.Language,
        TranslationKey = document.TranslationKey,
        Html = document.Html,
        LatinHtml = document.LatinHtml,
        Tags = document.Tags,
        Attribution = document.Attribution,
    };

    public static PrayerSummary ToSummaryContract(this PrayerDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        SortValue = document.SortValue,
        Summary = document.Summary,
        Category = document.Category,
        Tags = document.Tags,
    };
}
