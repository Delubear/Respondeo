using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Articles;
using Respondeo.Content.Devotions;
using Respondeo.Content.Discover;
using Respondeo.Content.Inquiry.Services;
using Respondeo.Content.Miracles;
using Respondeo.Content.Prayers;
using Respondeo.Content.Services;
using Respondeo.Content.Shared;

namespace Respondeo.Content;

/// <summary>
/// Registration surface for the <c>Respondeo.Content</c> library. Consumers call <see cref="AddRespondeoContent"/>
/// to register both content pillars (Discover and Inquiry) and depend only on the public interfaces; the service
/// and parser implementations stay internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both content pillars:
    /// <list type="bullet">
    /// <item><description>Discover: four peer content services (prayers, devotions, articles, and the catalog of Catholic miracles), plus a thin <see cref="IDiscoverOverview"/> facade used by the landing page to warm them all.</description></item>
    /// <item><description>Inquiry: the content parser and the <see cref="IContentService"/> implementation.</description></item>
    /// </list>
    /// The services are scoped because they depend on the scoped <see cref="HttpClient"/> in Blazor WebAssembly; the stateless parser is registered as a singleton.
    /// </summary>
    public static IServiceCollection AddRespondeoContent(this IServiceCollection services)
    {
        // Discover pillar
        services.AddScoped<IPrayerService, PrayerService>();
        services.AddScoped<IDevotionService, DevotionService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IMiracleService, MiracleService>();
        services.AddScoped<IDiscoverOverview, DiscoverOverview>();

        // Inquiry pillar
        services.AddSingleton<ContentParser>();
        services.AddScoped<IContentService, ContentService>();

        return services;
    }
}
