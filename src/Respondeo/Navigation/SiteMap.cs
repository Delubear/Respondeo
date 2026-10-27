using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

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
    public sealed record Entry(string Href, string Label, IReadOnlyList<Entry>? Children = null);

    /// <summary>The href of the Devotions entry, whose children are loaded at runtime.</summary>
    public const string DevotionsHref = "discover/devotions";

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
            new(Href(SitePillars.SummaId), "Summa"),
            new(Href(SitePillars.BibleId), "Bible"),
        };

        if (!features.DiscoverPillar)
        {
            return entries;
        }

        Add("discover/miracles", "Miracles", features.MiraclesFeature);
        Add("discover/saints", "Saints", features.SaintsFeature);
        Add("discover/prayers", "Prayers", features.PrayerFeature);
        Add(DevotionsHref, "Devotions", features.DevotionsFeature, devotions);
        Add("discover/hours", "Hours", features.LiturgyOfTheHoursFeature,
            [.. Enum.GetValues<Hour>().Select(h => new Entry($"discover/hours/{Pages.Hours.Slug(h)}", Pages.Hours.Label(h)))]);
        Add("discover/articles", "Articles", features.ArticlesFeature);

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
        if (features.DiscoverPillar)
        {
            guides.Add(new(OrbPrefix + OrbLauncher.Liturgical, "Today in the Church"));
            if (features.PrayerFeature)
            {
                guides.Add(new(OrbPrefix + OrbLauncher.Prayers, "Guide to Prayers"));
            }

            if (features.SaintsFeature)
            {
                guides.Add(new(OrbPrefix + OrbLauncher.Saints, "Reading the saints"));
            }
        }

        guides.Add(new(OrbPrefix + OrbLauncher.Summa, "How to read the Summa"));
        return guides;
    }

    /// <summary>Finds the entry for an href anywhere in the site map, or null when it is not present.</summary>
    public static Entry? Find(IReadOnlyList<Entry> entries, string href) =>
        Flatten(entries).FirstOrDefault(e => string.Equals(e.Href, href, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Entry> Flatten(IEnumerable<Entry> entries) =>
        entries.SelectMany(e => new[] { e }.Concat(Flatten(e.Children ?? [])));

    private static string Href(string pillarId) => SitePillars.Items.First(p => p.Id == pillarId).Href;
}
