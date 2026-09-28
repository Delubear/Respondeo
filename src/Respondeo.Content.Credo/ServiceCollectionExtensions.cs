using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Credo.Services;

namespace Respondeo.Content.Credo;

/// <summary>
/// Registration surface for the Credo feature (prayers, devotions, and articles for living the faith).
/// Consumers call <see cref="AddRespondeoCredo"/> and depend only on <see cref="ICredoService"/>;
/// the service implementation stays internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ICredoService"/> implementation.
    /// The service is scoped because it depends on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// </summary>
    public static IServiceCollection AddRespondeoCredo(this IServiceCollection services)
    {
        services.AddScoped<ICredoService, CredoService>();
        return services;
    }
}
