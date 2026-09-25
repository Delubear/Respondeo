using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Miracles.Services;

namespace Respondeo.Content.Miracles;

/// <summary>
/// Registration surface for the Miracles catalog feature.
/// Consumers call <see cref="AddRespondeoMiracles"/> and depend only on <see cref="IMiracleService"/>;
/// the service implementation stays internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IMiracleService"/> implementation.
    /// The service is scoped because it depends on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// </summary>
    public static IServiceCollection AddRespondeoMiracles(this IServiceCollection services)
    {
        services.AddScoped<IMiracleService, MiracleService>();
        return services;
    }
}
