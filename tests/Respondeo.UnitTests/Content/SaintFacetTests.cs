using Respondeo.Content.Contracts;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Unit tests for <see cref="SaintFacetCatalog"/> label/description lookup and the
/// <see cref="SaintFacets.Humanize"/> slug fallback used whenever a slug is not present in the loaded catalog.
/// </summary>
public class SaintFacetTests
{
    private static readonly SaintFacetCatalog Catalog = new()
    {
        Eras = new Dictionary<string, SaintEra>(StringComparer.OrdinalIgnoreCase)
        {
            ["medieval"] = new SaintEra("Medieval", "The Middle Ages."),
        },
        Regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["middle-east"] = "Middle East",
        },
        StatesOfLife = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["religious"] = "Religious",
        },
        Canonizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["canonized"] = "Canonized",
        },
    };

    [Fact]
    public void Era_resolves_mapped_label_and_description()
    {
        Assert.Equal("Medieval", Catalog.Era("medieval"));
        Assert.Equal("Medieval", Catalog.Era(" MEDIEVAL "));
        Assert.Equal("The Middle Ages.", Catalog.EraDescription("medieval"));
    }

    [Fact]
    public void Lookups_humanize_unmapped_slugs()
    {
        Assert.Equal("Baroque", Catalog.Era("baroque"));
        Assert.Equal("Oceania", Catalog.Region("oceania"));
        Assert.Equal("Lay", Catalog.StateOfLife("lay"));
        Assert.Equal("Beatified", Catalog.Canonization("beatified"));
    }

    [Fact]
    public void EraDescription_is_empty_for_unmapped_slug()
    {
        Assert.Empty(Catalog.EraDescription("baroque"));
    }

    [Theory]
    [InlineData("middle-east", "Middle east")]
    [InlineData("middle_east", "Middle east")]
    [InlineData("europe", "Europe")]
    [InlineData("", "Other")]
    [InlineData(null, "Other")]
    [InlineData("   ", "Other")]
    public void Humanize_formats_slug_into_readable_label(string? slug, string expected)
    {
        Assert.Equal(expected, SaintFacets.Humanize(slug));
    }

    [Fact]
    public void Empty_catalog_always_humanizes()
    {
        Assert.Equal("Medieval", SaintFacetCatalog.Empty.Era("medieval"));
        Assert.Empty(SaintFacetCatalog.Empty.EraDescription("medieval"));
    }
}
