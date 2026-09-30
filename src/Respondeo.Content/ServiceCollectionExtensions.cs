using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Articles;
using Respondeo.Content.Devotions;
using Respondeo.Content.Discover;
using Respondeo.Content.Miracles;
using Respondeo.Content.Prayers;
using Respondeo.Content.Services;

namespace Respondeo.Content;

/// <summary>
/// Registration surface for the Discover feature: four peer content services (prayers, devotions, articles, and the catalog of Catholic miracles),
/// plus a thin <see cref="IDiscoverOverview"/> facade used by the landing page to warm them all.
/// Consumers call <see cref="AddRespondeoDiscover"/> and depend only on the public interfaces; the service implementations stay internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the four content services and the <see cref="IDiscoverOverview"/> facade.
    /// The services are scoped because they depend on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// </summary>
    public static IServiceCollection AddRespondeoDiscover(this IServiceCollection services)
    {
        services.AddScoped<IPrayerService, PrayerService>();
        services.AddScoped<IDevotionService, DevotionService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IMiracleService, MiracleService>();
        services.AddScoped<IDiscoverOverview, DiscoverOverview>();
        return services;
    }
}
