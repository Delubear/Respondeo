namespace Respondeo.Services;

/// <summary>
/// The browser <c>localStorage</c> keys the app persists user preferences and state under.
/// Centralized so every key is unique and discoverable in one place.
/// </summary>
public static class StorageKeys
{
    /// <summary>Theme preference. Also read by the pre-render script in <c>wwwroot/index.html</c>; keep in sync.</summary>
    public const string Theme = "respondeo.theme";

    /// <summary>Text size preference. Also read by the pre-render script in <c>wwwroot/index.html</c>; keep in sync.</summary>
    public const string TextSize = "respondeo.textsize";

    public const string PrayerLanguage = "respondeo.prayerLanguage";
    public const string Pins = "respondeo.pins";
    public const string Visited = "respondeo.visited";
    /// <summary>Breadcrumb trail. Stored in <c>sessionStorage</c>.</summary>
    public const string Breadcrumb = "respondeo.breadcrumb";

    /// <summary>Stage of the current breadcrumb trail. Stored in <c>sessionStorage</c>.</summary>
    public const string BreadcrumbStage = "respondeo.breadcrumb.stage";

    public const string Calendar = "respondeo.calendar";
    public const string BibleTranslation = "respondeo.bibleTranslation";
    public const string DevotionProgress = "respondeo.devotion.progress";
    public const string HourSettings = "respondeo.hours.settings";
}
