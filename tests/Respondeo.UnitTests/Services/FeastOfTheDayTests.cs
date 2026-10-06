using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.LiturgicalCalendar;
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
    public async Task GetTodayAsync_returns_the_saint_the_calendar_celebrates_today()
    {
        // 4 October 2025 is Saint Francis in the General Roman Calendar, wired to his profile slug.
        var saints = StubbedSaints(("francis-of-assisi", "St. Francis of Assisi", "October 4"));
        var sut = Sut(saints, new DateOnly(2025, 10, 4));

        var feast = await sut.GetTodayAsync();

        Assert.NotNull(feast);
        Assert.Equal("francis-of-assisi", feast!.Id);
        Assert.Equal("St. Francis of Assisi", feast.Title);
    }

    [Fact]
    public async Task GetTodayAsync_falls_back_to_the_dataset_name_when_no_profile_exists()
    {
        // The slug is wired in the dataset but the catalog has no matching profile: use the dataset name.
        var sut = Sut(Substitute.For<ISaintService>(), new DateOnly(2025, 10, 4));

        var feast = await sut.GetTodayAsync();

        Assert.NotNull(feast);
        Assert.Equal("francis-of-assisi", feast!.Id);
        Assert.Equal("Saint Francis of Assisi", feast.Title);
    }

    [Fact]
    public async Task GetTodayAsync_returns_null_when_todays_celebration_links_to_no_saint()
    {
        // 25 December is the Nativity: a solemnity with no linked saint profile.
        var sut = Sut(Substitute.For<ISaintService>(), new DateOnly(2025, 12, 25));

        var feast = await sut.GetTodayAsync();

        Assert.Null(feast);
    }

    [Fact]
    public async Task GetTodayAsync_returns_null_on_a_ferial_day()
    {
        // 15 July 2025 carries an unlinked memorial over a ferial weekday: nothing to highlight.
        var sut = Sut(Substitute.For<ISaintService>(), new DateOnly(2025, 7, 1));

        var feast = await sut.GetTodayAsync();

        Assert.Null(feast);
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

    // Builds the system under test with an injected fixed date. The real LiturgicalCalendar supplies the
    // day's celebrations, so GetTodayAsync is driven by the General Roman Calendar, not by catalog scanning.
    private static FeastOfTheDay Sut(ISaintService saints, DateOnly date) =>
        new(saints, new RomanCalendar(), () => date);

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
