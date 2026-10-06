using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// Registration surface for the <c>Respondeo.LiturgicalCalendar</c> library.
/// Consumers call <see cref="AddRespondeoLiturgy"/> to register the pure, deterministic calendar engine and depend only on the public <see cref="ILiturgicalCalendar"/> interface;
/// the engine implementation stays internal.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Ordinary Form liturgical calendar engine as a singleton.
    /// The engine is pure and stateless (it holds only cached fixed-date data), so a single shared instance is safe everywhere. 
    /// </summary>
    public static IServiceCollection AddRespondeoLiturgy(this IServiceCollection services)
    {
        services.AddSingleton<ILiturgicalCalendar, RomanCalendar>();

        return services;
    }
}
