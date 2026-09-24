using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Summa.Services;

namespace Respondeo.Content.Summa;

/// <summary>
/// Registration surface for the Summa Theologica feature.
/// Consumers call <see cref="AddRespondeoSumma"/> and depend only on <see cref="ISummaService"/>; theservice implementation stays internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ISummaService"/> implementation.
    /// The service is scoped because it depends on the scoped <see cref="HttpClient"/> in Blazor WebAssembly.
    /// </summary>
    public static IServiceCollection AddRespondeoSumma(this IServiceCollection services)
    {
        services.AddScoped<ISummaService, SummaService>();
        return services;
    }
}
