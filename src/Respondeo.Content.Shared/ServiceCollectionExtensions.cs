using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.Content.Shared;

/// <summary>
/// Registration surface for the shared Summa reference rendering implementation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ISummaReferenceRenderer"/> implementation,
    /// which resolves part labels/slugs through <see cref="ISummaPartMap"/> (registered by the Summa feature).
    /// </summary>
    public static IServiceCollection AddRespondeoContentRendering(this IServiceCollection services)
    {
        services.AddSingleton<ISummaReferenceRenderer, SummaReferenceRenderer>();
        return services;
    }
}
