namespace Respondeo.Services;

/// <summary>
/// Deploy-time toggles for optional site features.
/// Bound from the "FeatureFlags" configuration section by <see cref="FeatureFlagsServiceCollectionExtensions.AddFeatureFlags"/>:
/// the web host reads <c>wwwroot/appsettings.json</c> and the MAUI host reads its embedded <c>appsettings.json</c>,
/// so flipping a flag there turns the feature off for a build or deploy without a code change.
/// Every flag defaults to <see langword="true"/> so features are on unless explicitly disabled.
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
