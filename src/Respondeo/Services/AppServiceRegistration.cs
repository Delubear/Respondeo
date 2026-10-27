using Respondeo.Bible;
using Respondeo.LiturgicalCalendar;
using Respondeo.Services.Devotions;
using Respondeo.Services.Prayers;
using Respondeo.Services.Summa;

namespace Respondeo.Services;

/// <summary>
/// Groups the app's service registrations by concern so Program.cs reads as a short table of contents.
/// The content, liturgy, and Summa libraries register themselves through their own extension methods.
/// </summary>
public static class AppServiceRegistration
{
    /// <summary>Per-reader state: navigation memory, preferences, and progress, persisted in session or local storage.</summary>
    public static IServiceCollection AddRespondeoReaderState(this IServiceCollection services)
    {
        // The visitor's navigation path (sessionStorage) for breadcrumbs.
        services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

        // Which nodes the visitor has opened this journey (sessionStorage) to mark visited branch cards.
        services.AddScoped<IVisitedNodes, VisitedNodes>();

        // Preferred light/dark theme (localStorage).
        services.AddScoped<IThemeService, ThemeService>();

        // Reader's preferred text size (localStorage).
        services.AddScoped<ITextSizeService, TextSizeService>();

        // Pinned quick-menu destinations (localStorage).
        services.AddScoped<IPinService, PinService>();
        services.AddScoped<OrbLauncher>();

        // Whether the reader prefers Latin prayer names in devotions (localStorage).
        services.AddScoped<IPrayerLanguageService, PrayerLanguageService>();

        // The reader's preferred Bible translation (localStorage).
        services.AddScoped<IBibleTranslationService, BibleTranslationService>();

        // The reader's place in a devotion (localStorage) so it can be resumed after leaving.
        services.AddScoped<IDevotionProgressService, DevotionProgressService>();

        // Whether a navigation should reset to the top (masthead nav) or scroll to content (cards/articles).
        services.AddScoped<NavigationIntent>();

        // Transient browse view state (search text + facet filters), scoped per area path.
        services.AddScoped<BrowseState>();

        return services;
    }

    /// <summary>Pure browse, search, and presentation logic for each content area, kept out of the pages so it is unit-testable.</summary>
    public static IServiceCollection AddRespondeoBrowse(this IServiceCollection services)
    {
        // Assembles the Summa question page's display data (part context, SEO description, back-crumb).
        services.AddScoped<SummaQuestionPresenter>();

        // Title/treatise search over the Summa index.
        services.AddScoped<SummaSearch>();

        // Facet/search/sort logic (plus card presentation where applicable) for each Discover catalog.
        services.AddScoped<PrayerBrowse>();
        services.AddScoped<MiracleBrowse>();
        services.AddScoped<SaintBrowse>();
        services.AddScoped<DevotionBrowse>();
        services.AddScoped<ArticleBrowse>();

        // Warms every Discover content index so the landing page can pre-load the whole pillar in one call.
        services.AddScoped<DiscoverOverview>();

        return services;
    }

    /// <summary>Date-driven surfaces: the saint of the day, day-rollover detection, and the seasonal masthead portrait.</summary>
    public static IServiceCollection AddRespondeoToday(this IServiceCollection services)
    {
        // "Today" in the reader's local time zone, injected so the date-driven logic stays testable.
        services.AddScoped<Func<DateOnly>>(_ => () => DateOnly.FromDateTime(DateTime.Now));

        // The reader's chosen liturgical calendar (General Roman or United States, localStorage); it replaces the
        // engine's default registration so every date-driven surface follows the same preference.
        services.AddScoped<PreferredLiturgicalCalendar>();
        services.AddScoped<ICalendarPreference>(sp => sp.GetRequiredService<PreferredLiturgicalCalendar>());
        services.AddScoped<ILiturgicalCalendar>(sp => sp.GetRequiredService<PreferredLiturgicalCalendar>());

        // Finds the saint whose feast the Church keeps today (404 page and liturgy dialog).
        services.AddScoped<IFeastOfTheDay, FeastOfTheDay>();

        // Detects the calendar day rolling over in a long-lived tab so date-driven surfaces refresh in place.
        services.AddScoped<IDayWatcher, DayWatcher>();

        // Picks the masthead portrait of Aquinas proper to the day.
        services.AddScoped<ISeasonalPortrait, SeasonalPortrait>();

        return services;
    }

    /// <summary>Typed wrappers over the window-level JS modules.</summary>
    public static IServiceCollection AddRespondeoInterop(this IServiceCollection services)
    {
        services.AddScoped<BrowseStateInterop>();
        services.AddScoped<NavigationInterop>();
        services.AddScoped<SummaRefInterop>();
        services.AddScoped<ImmersiveInterop>();
        services.AddScoped<DialogInterop>();
        services.AddScoped<AccessibilityInterop>();
        services.AddScoped<InstallInterop>();
        services.AddScoped<TallyInterop>();
        services.AddScoped<OfflinePacksInterop>();
        services.AddScoped<OfflinePacks>();

        services.AddBible();

        return services;
    }
}
