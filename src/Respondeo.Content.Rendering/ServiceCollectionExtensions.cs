using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Abstractions;

namespace Respondeo.Content.Rendering;

/// <summary>
/// Registration surface for the shared content HTML rendering implementation.
/// Consumers depend only on <see cref="IContentHtmlRenderer"/> from the abstractions project; the
/// Markdig-based implementation stays internal to this project.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IContentHtmlRenderer"/> implementation as a singleton (it is stateless).
    /// </summary>
    public static IServiceCollection AddRespondeoContentRendering(this IServiceCollection services)
    {
        services.AddSingleton<IContentHtmlRenderer, MarkdigContentHtmlRenderer>();
        return services;
    }
}
