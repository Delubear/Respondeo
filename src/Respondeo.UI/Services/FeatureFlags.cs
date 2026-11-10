namespace Respondeo.Services;

/// <summary>
/// Deploy-time toggles for optional site features.
/// Bound from the "FeatureFlags" configuration section by <see cref="FeatureFlagsServiceCollectionExtensions.AddFeatureFlags"/>:
/// both hosts package the shared <c>Configuration/appsettings.json</c> baseline in their startup configuration location.
/// Host/environment overrides merge before registration. Startup requires every flag; property initializers are only
/// conveniences for explicitly constructed instances, not fallbacks for missing deployment configuration.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>The configuration section these flags bind from.</summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>Shows the Liturgy of the Hours sub-area within the Discover pillar when enabled.</summary>
    public bool LiturgyOfTheHoursFeature { get; set; } = true;

    /// <summary>
    /// When enabled, the feedback trigger opens the choice dialog (open a GitHub issue, or send anonymous feedback via the hosted Tally form).
    /// When disabled, the trigger skips the dialog entirely and goes straight to opening a GitHub issue.
    /// </summary>
    public bool ExternalFeedback { get; set; } = true;
}
