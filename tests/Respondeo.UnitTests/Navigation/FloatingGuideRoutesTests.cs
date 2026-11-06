using Respondeo.Navigation;
using Respondeo.Services;

namespace Respondeo.UnitTests.Navigation;

public class FloatingGuideRoutesTests
{
    [Theory]
    [InlineData("discover/prayers", OrbLauncher.Prayers, OrbLauncher.Liturgical)]
    [InlineData("discover/saints", OrbLauncher.Saints, OrbLauncher.Liturgical)]
    public void Browse_pages_stack_the_liturgical_orb_above_their_guide(string route, string lower, string upper)
    {
        var state = FloatingGuideRoutes.Resolve(route);

        Assert.Equal(0, state.SlotOf(lower));
        Assert.Equal(1, state.SlotOf(upper));
    }

    [Fact]
    public void Single_orb_routes_use_the_base_slot()
    {
        Assert.Equal(0, FloatingGuideRoutes.Resolve("discover").SlotOf(OrbLauncher.Liturgical));
        Assert.Equal(0, FloatingGuideRoutes.Resolve("bible/john/3").SlotOf(OrbLauncher.Bible));
    }

    [Fact]
    public void Every_visible_orb_gets_a_distinct_slot()
    {
        var state = new FloatingGuideState(true, true, true, true, ShowBibleGuide: true);
        string[] keys = [OrbLauncher.Bible, OrbLauncher.Summa, OrbLauncher.Saints, OrbLauncher.Prayers, OrbLauncher.Liturgical];

        Assert.Equal([0, 1, 2, 3, 4], keys.Select(state.SlotOf));
    }
}
