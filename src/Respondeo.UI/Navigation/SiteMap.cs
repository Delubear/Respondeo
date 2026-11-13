using Microsoft.AspNetCore.Components;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services.Summa;

namespace Respondeo.Navigation;

/// <summary>
/// The pinnable destinations shown in the quick menu. Routes come from <see cref="SitePillars"/> and the Discover routes,
/// and every Discover destination honors its feature flag.
/// </summary>
public static class SiteMap
{
    /// <summary>A single destination in the site map.</summary>
    /// <param name="Href">The relative route (empty string is Home).</param>
    /// <param name="Label">The visible link text.</param>
    /// <param name="Children">Nested destinations shown beneath this one (e.g. each devotion or hour).</param>
    /// <param name="Action">When set, the link runs this action (resolving services from the provider) instead of navigating; Href remains the pin key.</param>
    public sealed record Entry(string Href, string Label, IReadOnlyList<Entry>? Children = null, Func<IServiceProvider, Task>? Action = null);

    /// <summary>The href of the Devotions entry, whose children are loaded at runtime.</summary>
    public const string DevotionsHref = ContentRoutes.Devotions;

    /// <summary>
    /// Builds the pinnable destinations, honoring the feature flags. Devotion children are supplied by the caller
    /// because the devotion catalog is loaded at runtime.
    /// </summary>
    public static IReadOnlyList<Entry> Build(FeatureFlags features, IReadOnlyList<Entry>? devotions = null)
    {
        var entries = new List<Entry>
        {
            new("", "Inquiry",
            [
                new("why-god", "Why God"),
                new("which-god", "Which God"),
                new("why-jesus", "Why Jesus"),
                new("why-the-church", "Why the Church"),
                new("coming-home", "Coming Home"),
            ]),
            new(Href(SitePillars.SummaId), "Summa",
            [
                new("summa/random", "Random Summa Article", Action: OpenRandomSummaAsync)
            ]),
            new(Href(SitePillars.BibleId), "Bible"),
        };

        Add(ContentRoutes.Miracles, "Miracles", true);
        Add(ContentRoutes.Saints, "Saints", true);
        Add(ContentRoutes.Prayers, "Prayers", true);
        Add(DevotionsHref, "Devotions", true, devotions);
        Add(ContentRoutes.Hours, "Hours", features.LiturgyOfTheHoursFeature,
            [.. Enum.GetValues<Hour>().Select(h => new Entry(ContentRoutes.HourHref(Pages.Hours.Slug(h)), Pages.Hours.Label(h)))]);
        Add(ContentRoutes.Articles, "Articles", true);

        return entries;

        void Add(string href, string label, bool enabled, IReadOnlyList<Entry>? children = null)
        {
            if (enabled)
            {
                entries.Add(new(href, label, children));
            }
        }
    }

    /// <summary>Prefix marking a pin that opens a floating help orb's dialog instead of navigating.</summary>
    public const string OrbPrefix = "orb:";

    /// <summary>The pinnable help orbs, honoring the feature flags.</summary>
    public static IReadOnlyList<Entry> Guides(FeatureFlags features)
    {
        var guides = new List<Entry>();
        if (features.LiturgicalCalendarFeature)
        {
            guides.Add(Orb(OrbLauncher.Liturgical, "Today in the Church"));
        }

        guides.AddRange(
        [
            Orb(OrbLauncher.Prayers, "Guide to Prayers"),
            Orb(OrbLauncher.Saints, "Reading the saints"),
            Orb(OrbLauncher.Summa, "How to read the Summa"),
            Orb(OrbLauncher.Bible, "How to read the Bible"),
        ]);
        return guides;
    }

    /// <summary>Finds the entry for an href anywhere in the site map, or null when it is not present.</summary>
    public static Entry? Find(IReadOnlyList<Entry> entries, string href) =>
        Flatten(entries).FirstOrDefault(e => string.Equals(e.Href, href, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Entry> Flatten(IEnumerable<Entry> entries) =>
        entries.SelectMany(e => new[] { e }.Concat(Flatten(e.Children ?? [])));

    private static Entry Orb(string id, string label) =>
        new(OrbPrefix + id, label, Action: sp =>
        {
            sp.GetRequiredService<OrbLauncher>().Request(id);
            return Task.CompletedTask;
        });

    private static async Task OpenRandomSummaAsync(IServiceProvider sp) =>
        sp.GetRequiredService<NavigationManager>().NavigateTo(await sp.GetRequiredService<RandomSummaArticle>().PickUrlAsync() ?? ContentRoutes.Summa);

    private static string Href(string pillarId) => SitePillars.Items.First(p => p.Id == pillarId).Href;
}
