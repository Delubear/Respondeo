using Microsoft.Extensions.DependencyInjection;

namespace Respondeo.Bible;

public static class BibleServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IBibleText"/>. Requires a scoped <see cref="HttpClient"/> rooted at the app base address.</summary>
    public static IServiceCollection AddBible(this IServiceCollection services)
    {
        services.AddScoped<IBibleTranslations, BibleTranslationCatalog>();
        services.AddScoped<IBibleText, DouayRheimsBibleText>();
        services.AddScoped<IBibleChapterReader, BibleChapterReader>();
        return services;
    }
}
