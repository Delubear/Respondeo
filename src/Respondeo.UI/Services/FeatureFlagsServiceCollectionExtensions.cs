using Microsoft.Extensions.Configuration;

namespace Respondeo.Services;

/// <summary>Registers <see cref="FeatureFlags"/> bound from configuration, shared by every host.</summary>
public static class FeatureFlagsServiceCollectionExtensions
{
    /// <summary>Binds required deploy-time flags and registers them as a singleton; incomplete configuration fails at startup.</summary>
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(FeatureFlags.SectionName);
        var flags = new FeatureFlags
        {
            LiturgicalCalendarFeature = section.GetValue<bool?>(nameof(FeatureFlags.LiturgicalCalendarFeature))
                ?? throw new InvalidOperationException("Required feature flag 'LiturgicalCalendarFeature' is missing."),
            LiturgyOfTheHoursFeature = section.GetValue<bool?>(nameof(FeatureFlags.LiturgyOfTheHoursFeature))
                ?? throw new InvalidOperationException("Required feature flag 'LiturgyOfTheHoursFeature' is missing."),
            ExternalFeedback = section.GetValue<bool?>(nameof(FeatureFlags.ExternalFeedback))
                ?? throw new InvalidOperationException("Required feature flag 'ExternalFeedback' is missing.")
        };
        return services.AddSingleton(flags);
    }
}
