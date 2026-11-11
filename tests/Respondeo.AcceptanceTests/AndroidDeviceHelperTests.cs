using System.Xml.Linq;
using Respondeo.AcceptanceTests.Support;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class AndroidDeviceHelperTests
{
    [Theory]
    [InlineData("[10,20][110,220]", 10, 20, 110, 220)]
    [InlineData("[0,0][0,0]", 0, 0, 0, 0)]
    public void Bounds_preserve_exact_device_coordinates(string value, int left, int top, int right, int bottom) =>
        Assert.Equal((left, top, right, bottom), AndroidDevice.ParseBounds(value));

    [Theory]
    [InlineData("")]
    [InlineData("10,20,110,220")]
    [InlineData("[-1,0][10,20]")]
    [InlineData("[10,20][5,30]")]
    [InlineData("[10,20][30,5]")]
    public void Malformed_or_reversed_bounds_fail_explicitly(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => AndroidDevice.ParseBounds(value));
        Assert.Contains("Android accessibility bounds", exception.Message);
        Assert.EndsWith(value, exception.Message);
    }

    [Theory]
    [InlineData("[10,20][110,220]", true)]
    [InlineData("[0,0][0,0]", false)]
    [InlineData("[10,20][110,20]", false)]
    [InlineData("[10,20][10,220]", false)]
    public void Visibility_excludes_clipped_and_zero_area_nodes(string bounds, bool expected)
    {
        var node = new XElement("node", new XAttribute("bounds", bounds));
        Assert.Equal(expected, AndroidDevice.IsVisible(node));
    }

    [Theory]
    [InlineData("text", " Dark ", "Dark", false, true)]
    [InlineData("content-desc", "Open the quick menu", "open the quick menu", false, true)]
    [InlineData("text", "Dark", "ark", false, false)]
    [InlineData("text", "Dark", "ark", true, true)]
    [InlineData("text", "Large text", "Larger text", false, false)]
    [InlineData("text", "Light", "Dark", true, false)]
    public void Labels_distinguish_exact_matches_from_explicit_substrings(string attribute, string value, string label, bool contains, bool expected)
    {
        var node = new XElement("node", new XAttribute(attribute, value));
        Assert.Equal(expected, AndroidDevice.HasLabel(node, label, contains));
    }

    [Fact]
    public void Missing_labels_do_not_match() => Assert.False(AndroidDevice.HasLabel(new XElement("node"), "Dark"));
}
