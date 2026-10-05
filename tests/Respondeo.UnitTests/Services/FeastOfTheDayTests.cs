using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="FeastOfTheDay"/>: the fixed-date matching helper and the end-to-end
/// lookup over a stubbed <see cref="ISaintService"/> with an injected, deterministic current date.
/// </summary>
public class FeastOfTheDayTests
{
    [Theory]
    [InlineData("October 4", 10, 4, true)]
    [InlineData("Oct 4", 10, 4, true)]
    [InlineData("October 4", 10, 5, false)]
    [InlineData("October 4", 9, 4, false)]
    [InlineData("  October 4  ", 10, 4, true)]
    [InlineData("December 12", 12, 12, true)]
    [InlineData("Corpus Christi", 6, 19, false)]
    [InlineData("", 10, 4, false)]
    [InlineData(null, 10, 4, false)]
    [InlineData("February 29", 2, 28, false)]
    public void TryMatchFixedDate_matches_only_fixed_calendar_dates(string? feastDay, int month, int day, bool expected)
    {
        var date = new DateOnly(2025, month, day);

        var result = FeastOfTheDay.TryMatchFixedDate(feastDay, date);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsToday_is_true_when_the_feast_falls_on_the_injected_date()
    {
        var sut = Sut(Substitute.For<ISaintService>(), new DateOnly(2025, 10, 4));

        Assert.True(sut.IsToday("October 4"));
        Assert.False(sut.IsToday("October 5"));
        Assert.False(sut.IsToday(null));
    }

    [Fact]
    public async Task GetTodayAsync_returns_the_saint_whose_feast_is_today()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("therese-of-lisieux", "St. Thérèse of Lisieux", "October 1"));
        var sut = Sut(saints, new DateOnly(2025, 10, 4));

        var feast = await sut.GetTodayAsync();

        Assert.NotNull(feast);
        Assert.Equal("francis-of-assisi", feast!.Id);
        Assert.Equal("St. Francis of Assisi", feast.Title);
    }

    [Fact]
    public async Task GetTodayAsync_returns_null_when_no_feast_matches()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("therese-of-lisieux", "St. Thérèse of Lisieux", "October 1"));
        var sut = Sut(saints, new DateOnly(2025, 7, 15));

        var feast = await sut.GetTodayAsync();

        Assert.Null(feast);
    }

    [Fact]
    public async Task GetTodayAsync_ignores_saints_without_a_feast_day()
    {
        var saints = StubbedSaints(
            ("no-feast", "A saint without a feast", null),
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"));
        var sut = Sut(saints, new DateOnly(2025, 10, 4));

        var feast = await sut.GetTodayAsync();

        Assert.NotNull(feast);
        Assert.Equal("francis-of-assisi", feast!.Id);
    }

    [Fact]
    public async Task GetTodayAsync_picks_a_random_saint_when_several_share_the_feast()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("francis-borgia", "St. Francis Borgia", "October 4"),
            ("petronius", "St. Petronius", "October 4"));

        // pickIndex is driven deterministically to prove the chosen index is honored.
        var byFirst = await Sut(saints, new DateOnly(2025, 10, 4), _ => 0).GetTodayAsync();
        var bySecond = await Sut(saints, new DateOnly(2025, 10, 4), _ => 1).GetTodayAsync();
        var byThird = await Sut(saints, new DateOnly(2025, 10, 4), _ => 2).GetTodayAsync();

        Assert.Equal("francis-of-assisi", byFirst!.Id);
        Assert.Equal("francis-borgia", bySecond!.Id);
        Assert.Equal("petronius", byThird!.Id);
    }

    [Fact]
    public async Task GetTodayAsync_asks_the_selector_for_an_index_within_range()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("francis-borgia", "St. Francis Borgia", "October 4"));
        int? requestedCount = null;
        var sut = Sut(saints, new DateOnly(2025, 10, 4), count => { requestedCount = count; return 0; });

        await sut.GetTodayAsync();

        Assert.Equal(2, requestedCount);
    }

    [Fact]
    public async Task GetTodayFeastIdsAsync_returns_every_saint_whose_feast_is_today()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("francis-borgia", "St. Francis Borgia", "October 4"),
            ("therese-of-lisieux", "St. Thérèse of Lisieux", "October 1"));
        var sut = Sut(saints, new DateOnly(2025, 10, 4));

        var ids = await sut.GetTodayFeastIdsAsync();

        Assert.Equal(2, ids.Count);
        Assert.Contains("francis-of-assisi", ids);
        Assert.Contains("francis-borgia", ids);
        Assert.DoesNotContain("therese-of-lisieux", ids);
    }

    [Fact]
    public async Task GetTodayFeastIdsAsync_returns_empty_when_no_feast_matches()
    {
        var saints = StubbedSaints(
            ("francis-of-assisi", "St. Francis of Assisi", "October 4"),
            ("therese-of-lisieux", "St. Thérèse of Lisieux", "October 1"));
        var sut = Sut(saints, new DateOnly(2025, 7, 15));

        var ids = await sut.GetTodayFeastIdsAsync();

        Assert.Empty(ids);
    }

    [Fact]
    public async Task GetTodayFeastIdsAsync_matches_ids_case_insensitively()
    {
        var saints = StubbedSaints(("francis-of-assisi", "St. Francis of Assisi", "October 4"));
        var sut = Sut(saints, new DateOnly(2025, 10, 4));

        var ids = await sut.GetTodayFeastIdsAsync();

        Assert.Contains("FRANCIS-OF-ASSISI", ids);
    }

    // Builds the system under test with an injected fixed date and a deterministic index selector
    // (first match by default) so tests never depend on real randomness.
    private static FeastOfTheDay Sut(ISaintService saints, DateOnly date, Func<int, int>? pickIndex = null) =>
        new(saints, () => date, pickIndex ?? (_ => 0));

    private static ISaintService StubbedSaints(params (string Id, string Title, string? FeastDay)[] entries)
    {
        var saints = Substitute.For<ISaintService>();

        var index = new SaintIndex
        {
            Entries = entries
                .Select(e => new SaintIndexEntry
                {
                    Id = e.Id,
                    Title = e.Title,
                    Era = "medieval",
                    Patronages = [],
                    StatesOfLife = [],
                    Canonizations = [],
                })
                .ToList(),
        };
        saints.GetIndexAsync().Returns(index);

        foreach (var entry in entries)
        {
            var record = new SaintRecord
            {
                Id = entry.Id,
                Title = entry.Title,
                Era = "medieval",
                Patronages = [],
                StatesOfLife = [],
                Canonizations = [],
                FeastDay = entry.FeastDay,
            };
            saints.GetByIdAsync(entry.Id).Returns(record);
        }

        return saints;
    }
}
