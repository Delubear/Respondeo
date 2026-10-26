namespace Respondeo.Bible.Contracts;

/// <summary>The bundled Bible translations, loaded on first use from <c>translations.json</c> and each translation's manifest.</summary>
public interface IBibleTranslations
{
    /// <summary>Every bundled translation, the default first.</summary>
    Task<IReadOnlyList<BibleTranslation>> GetAllAsync();

    /// <summary>The default translation (the first one listed).</summary>
    Task<BibleTranslation> GetDefaultAsync();

    /// <summary>Finds a translation by code or alias, or <c>null</c> when it is unknown.</summary>
    Task<BibleTranslation?> FindAsync(string? code);
}
