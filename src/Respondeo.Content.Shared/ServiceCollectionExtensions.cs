using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.Content.Shared;

/// <summary>
/// Registration surface for the shared content HTML rendering implementation.
/// Consumers depend only on <see cref="IContentHtmlRenderer"/> from the abstractions project; the Markdig-based implementation stays internal to this project.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IContentHtmlRenderer"/> implementation as a singleton (it is stateless).
    /// Also registers the <see cref="ISummaReferenceRenderer"/> implementation,
    /// which resolves part labels/slugs through <see cref="ISummaPartMap"/> (registered by the Summa feature).
    /// </summary>
    public static IServiceCollection AddRespondeoContentRendering(this IServiceCollection services)
    {
        services.AddSingleton<IContentHtmlRenderer, MarkdigContentHtmlRenderer>();
        services.AddSingleton<ISummaReferenceRenderer, SummaReferenceRenderer>();
        return services;
    }
}
