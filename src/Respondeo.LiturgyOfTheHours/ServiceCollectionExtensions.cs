using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>Dispatches to the registered form engines.</summary>
internal sealed class HoursEngine(IEnumerable<IOfficeForm> forms) : ILiturgyOfTheHours
{
    private readonly Dictionary<OfficeForm, IOfficeForm> _forms = forms.ToDictionary(f => f.Form);

    public Office Compose(DateOnly date, Hour hour, OfficeForm form = OfficeForm.OrdinaryForm1970) =>
        _forms.TryGetValue(form, out var engine)
            ? engine.Compose(date, hour)
            : throw new NotSupportedException($"The office form {form} is not registered.");
}

/// <summary>
/// Registration surface for the <c>Respondeo.LiturgyOfTheHours</c> library.
/// Requires <c>AddRespondeoLiturgy()</c> from the calendar library, since the office is built on the liturgical day.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the Liturgy of the Hours engine (post-1970 form) as scoped, matching the scoped liturgical calendar.</summary>
    public static IServiceCollection AddRespondeoHours(this IServiceCollection services)
    {
        services.AddScoped<IOfficeForm, OrdinaryFormOffice>();
        services.AddScoped<ILiturgyOfTheHours, HoursEngine>();

        return services;
    }
}
