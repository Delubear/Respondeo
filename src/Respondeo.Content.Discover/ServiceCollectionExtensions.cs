using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Discover.Services;

namespace Respondeo.Content.Discover;

/// <summary>
/// Registration surface for the Discover feature: prayers, devotions, and articles for living the
/// faith, plus the catalog of Catholic miracles.
/// Consumers call <see cref="AddRespondeoDiscover"/> and depend only on <see cref="IDiscoverService"/>
/// and <see cref="IMiracleService"/>; the service implementations stay internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IDiscoverService"/> and <see cref="IMiracleService"/> implementations.
    /// The services are scoped because they depend on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// </summary>
    public static IServiceCollection AddRespondeoDiscover(this IServiceCollection services)
    {
        services.AddScoped<IDiscoverService, DiscoverService>();
        services.AddScoped<IMiracleService, MiracleService>();
        return services;
    }
}
