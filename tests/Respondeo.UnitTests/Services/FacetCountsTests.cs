using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class FacetCountsTests
{
    [Fact]
    public void Counts_each_row_once_per_distinct_value_case_insensitively()
    {
        var rows = new[] { new[] { "a", "A", "b" }, new[] { "b" }, new string[0] };

        var counts = FacetCounts.Of(rows, r => (IEnumerable<string?>)r);

        Assert.Equal(1, counts["a"]);
        Assert.Equal(2, counts["B"]);
    }

    [Fact]
    public void Skips_null_scalar_values()
    {
        var rows = new[] { "x", null, "x" };

        var counts = FacetCounts.Of(rows, r => r);

        Assert.Single(counts);
        Assert.Equal(2, counts["x"]);
    }
}
