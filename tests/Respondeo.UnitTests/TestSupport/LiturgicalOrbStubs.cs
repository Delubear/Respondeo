using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Content.Liturgy;
using Respondeo.Services;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Registers the services the floating <c>LiturgicalOrb</c> injects (<see cref="ILiturgicalCalendar"/>,
/// the current-date delegate, and <see cref="IFeastOfTheDay"/>) so Discover root pages that host the orb
/// can render inside bUnit without wiring up the liturgical calendar in every page test.
/// </summary>
internal static class LiturgicalOrbStubs
{
    public static void AddLiturgicalOrbStubs(this TestContext context)
    {
        context.Services.AddSingleton<ILiturgicalCalendar>(new LiturgicalCalendar());
        context.Services.AddSingleton<Func<DateOnly>>(() => new DateOnly(2025, 1, 1));

        var feast = Substitute.For<IFeastOfTheDay>();
        feast.GetTodayAsync().Returns((FeastHighlight?)null);
        feast.GetTodayFeastIdsAsync().Returns((IReadOnlySet<string>)new HashSet<string>());
        context.Services.AddSingleton(feast);

        // The orb's FloatingGuide hosts a Dialog, which injects DialogInterop.
        if (!context.Services.Any(s => s.ServiceType == typeof(DialogInterop)))
        {
            context.Services.AddScoped<DialogInterop>();
        }
    }
}
