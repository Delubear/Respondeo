using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class NavigationIntentTests
{
    [Fact]
    public void ResetToTop_defaults_to_false()
    {
        var intent = new NavigationIntent();

        Assert.False(intent.ResetToTop);
    }

    [Fact]
    public void ConsumeResetToTop_returns_the_pending_value()
    {
        var intent = new NavigationIntent { ResetToTop = true };

        Assert.True(intent.ConsumeResetToTop());
    }

    [Fact]
    public void ConsumeResetToTop_clears_the_flag_after_reading()
    {
        var intent = new NavigationIntent { ResetToTop = true };

        intent.ConsumeResetToTop();

        Assert.False(intent.ResetToTop);
    }

    [Fact]
    public void ConsumeResetToTop_is_single_use()
    {
        var intent = new NavigationIntent { ResetToTop = true };

        var first = intent.ConsumeResetToTop();
        var second = intent.ConsumeResetToTop();

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void ConsumeResetToTop_returns_false_when_never_set()
    {
        var intent = new NavigationIntent();

        Assert.False(intent.ConsumeResetToTop());
    }
}
