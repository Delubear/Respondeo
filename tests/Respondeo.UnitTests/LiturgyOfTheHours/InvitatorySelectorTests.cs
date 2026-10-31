using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class InvitatorySelectorTests
{
    private static readonly OfficeStep[] Options =
    [
        Option("Psalm 95"), Option("Psalm 100"), Option("Psalm 67"), Option("Psalm 24"),
    ];

    private static OfficeStep Option(string citation) =>
        new(OfficeStepKind.Psalm, citation) { Reference = new PsalmodyReference(citation, null) };

    [Fact]
    public void ShowAll_ReturnsEveryOption() =>
        Assert.Equal([0, 1, 2, 3], InvitatorySelector.Select(Options, InvitatoryMode.ShowAll, null, 0, new Random(1)));

    [Fact]
    public void Specific_ReturnsTheChosenPsalm() =>
        Assert.Equal([2], InvitatorySelector.Select(Options, InvitatoryMode.Specific, "Psalm 67", 0, new Random(1)));

    [Fact]
    public void Specific_UnknownKey_FallsBackToTheFirst() =>
        Assert.Equal([0], InvitatorySelector.Select(Options, InvitatoryMode.Specific, "Psalm 1", 0, new Random(1)));

    [Fact]
    public void Random_ReturnsOneValidOption()
    {
        var picked = Assert.Single(InvitatorySelector.Select(Options, InvitatoryMode.Random, null, 0, new Random(7)));
        Assert.InRange(picked, 0, Options.Length - 1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(5, 1)]
    public void Cycle_WrapsTheIndex(int cycleIndex, int expected) => Assert.Equal([expected], InvitatorySelector.Select(Options, InvitatoryMode.Cycle, null, cycleIndex, new Random(1)));

    [Fact]
    public void Select_NoOptions_ReturnsNothing() =>
        Assert.Empty(InvitatorySelector.Select([], InvitatoryMode.Cycle, null, 2, new Random(1)));

    [Fact]
    public void AdvanceCycle_SameDay_KeepsTheIndex()
    {
        var today = new DateOnly(2025, 3, 1);
        Assert.Equal((2, today), InvitatorySelector.AdvanceCycle(2, today, today, 4));
    }

    [Fact]
    public void AdvanceCycle_NextDay_Advances()
    {
        var today = new DateOnly(2025, 3, 2);
        Assert.Equal((3, today), InvitatorySelector.AdvanceCycle(2, today.AddDays(-1), today, 4));
    }

    [Fact]
    public void AdvanceCycle_NextDay_WrapsToTheStart()
    {
        var today = new DateOnly(2025, 3, 2);
        Assert.Equal((0, today), InvitatorySelector.AdvanceCycle(3, today.AddDays(-1), today, 4));
    }

    [Fact]
    public void AdvanceCycle_FirstUse_StartsAtTheStoredIndex()
    {
        var today = new DateOnly(2025, 3, 2);
        Assert.Equal((0, today), InvitatorySelector.AdvanceCycle(0, null, today, 4));
    }

    [Fact]
    public void Settings_DefaultsWhenMissing() => Assert.Equal(new HourPlayerSettings(), HourPlayerSettingsService.Deserialize(null));

    [Fact]
    public void Settings_DefaultsWhenCorrupt() => Assert.Equal(new HourPlayerSettings(), HourPlayerSettingsService.Deserialize("{not json"));

    [Fact]
    public void Settings_RoundTrip()
    {
        var settings = new HourPlayerSettings
        {
            ExpandAll = true,
            AntiphonAtStartOnly = true,
            InvitatoryMode = InvitatoryMode.Cycle,
            InvitatoryPsalm = "Psalm 24",
            InvitatoryCycleIndex = 2,
            InvitatoryCycleDate = new DateOnly(2025, 3, 2),
        };

        Assert.Equal(settings, HourPlayerSettingsService.Deserialize(System.Text.Json.JsonSerializer.Serialize(settings)));
    }
}
