using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Summa.Services;

namespace Respondeo.Content.Summa;

/// <summary>
/// Registration surface for the Summa Theologica feature.
/// Consumers call <see cref="AddRespondeoSumma"/> and depend only on <see cref="ISummaService"/> and <see cref="ISummaReferenceRenderer"/>; the implementations stay internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ISummaService"/> implementation and the Summa reference rendering pipeline.
    /// The service is scoped because it depends on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// The <see cref="ISummaReferenceRenderer"/> resolves part labels/slugs through <see cref="ISummaPartMap"/>.
    /// </summary>
    public static IServiceCollection AddRespondeoSumma(this IServiceCollection services)
    {
        services.AddScoped<ISummaService, SummaService>();
        services.AddSingleton<ISummaPartMap, SummaPartMap>();
        services.AddSingleton<ISummaReferenceRenderer, SummaReferenceRenderer>();
        return services;
    }
}
