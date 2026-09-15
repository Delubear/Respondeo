using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.Content.Markdown;

/// <summary>
/// Registration surface for the content feature. Consumers call <see cref="AddRespondeoContent"/> and
/// depend only on <see cref="IContentService"/>; the parser and service implementations stay internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the content parser and the <see cref="IContentService"/> implementation.
    /// The service is scoped because it depends on the scoped <see cref="HttpClient"/> in Blazor WebAssembly;
    /// the parser is stateless and registered as a singleton.
    /// </summary>
    public static IServiceCollection AddRespondeoContent(this IServiceCollection services)
    {
        services.AddSingleton<ContentParser>();
        services.AddScoped<IContentService, ContentService>();
        return services;
    }
}
