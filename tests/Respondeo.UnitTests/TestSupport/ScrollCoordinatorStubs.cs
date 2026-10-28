using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Registers the real <see cref="ScrollCoordinator"/> (and the <see cref="NavigationIntent"/> it depends on)
/// so pages that claim or defer the navigation landing can render inside bUnit. Its JS calls fall through to
/// bUnit's JSInterop, so tests must run in loose mode.
/// </summary>
internal static class ScrollCoordinatorStubs
{
    public static void AddScrollCoordinator(this TestContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        if (!context.Services.Any(s => s.ServiceType == typeof(NavigationIntent)))
        {
            context.Services.AddScoped<NavigationIntent>();
        }

        if (!context.Services.Any(s => s.ServiceType == typeof(ScrollCoordinator)))
        {
            context.Services.AddScoped<ScrollCoordinator>();
        }
    }
}
