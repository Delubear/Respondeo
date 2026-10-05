using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class DevotionSessionTests
{
    private static Prayer Prayer(string id, string title) => new() { Id = id, Title = title, Html = $"<p>{title}</p>" };

    // A chaplet with no mystery sets: an opening prayer followed by three repeated Hail Marys.
    private static DevotionSession SimpleSession()
    {
        var devotion = new Devotion
        {
            Id = "divine-mercy-chaplet",
            Title = "The Divine Mercy Chaplet",
            Sequence =
            [
                new DevotionStep { Kind = "prayer", Title = "Opening", PrayerId = "our-father", Repeat = 1, Note = "Slowly" },
                new DevotionStep { Kind = "prayer", Title = "Hail Marys", PrayerId = "hail-mary", Repeat = 3 },
            ],
        };

        var prayers = new Dictionary<string, Prayer>(StringComparer.OrdinalIgnoreCase)
        {
            ["our-father"] = Prayer("our-father", "Our Father"),
            ["hail-mary"] = Prayer("hail-mary", "Hail Mary"),
        };

        return new DevotionSession(devotion, prayers);
    }

    // A devotion with a "mysteries" step iterating a set of two mysteries, each followed by one prayer.
    private static DevotionSession MysterySession()
    {
        var devotion = new Devotion
        {
            Id = "holy-rosary",
            Title = "The Holy Rosary",
            MysterySets =
            [
                new MysterySet
                {
                    Id = "joyful",
                    Name = "The Joyful Mysteries",
                    Mysteries =
                    [
                        new Mystery { Title = "The Annunciation", ReflectionHtml = "<p>Annunciation</p>" },
                        new Mystery { Title = "The Visitation" },
                    ],
                },
            ],
            Sequence =
            [
                new DevotionStep
                {
                    Kind = "mysteries",
                    Title = "Decade",
                    PerMystery = [new DevotionStep { Kind = "prayer", PrayerId = "hail-mary", Repeat = 1 }],
                },
            ],
        };

        var prayers = new Dictionary<string, Prayer>(StringComparer.OrdinalIgnoreCase)
        {
            ["hail-mary"] = Prayer("hail-mary", "Hail Mary"),
        };

        return new DevotionSession(devotion, prayers);
    }

    [Fact]
    public void Start_expands_every_repetition_into_its_own_prayer_row()
    {
        var session = SimpleSession();

        session.Start(null);

        Assert.Equal(4, session.Beads.Count);
        Assert.Equal(4, session.PrayerCount);
        Assert.All(session.Beads, b => Assert.True(b.IsPrayer));
    }

    [Fact]
    public void Start_numbers_prayer_rows_sequentially_from_zero()
    {
        var session = SimpleSession();

        session.Start(null);

        Assert.Equal([0, 1, 2, 3], session.Beads.Select(b => b.PrayerOrdinal));
    }

    [Fact]
    public void Start_only_labels_the_first_of_a_repeated_group()
    {
        var session = SimpleSession();

        session.Start(null);

        Assert.Equal("Hail Marys", session.Beads[1].Heading);
        Assert.Null(session.Beads[2].Heading);
        Assert.Null(session.Beads[3].Heading);
    }

    [Fact]
    public void Start_clamps_a_resume_point_to_the_prayer_count()
    {
        var session = SimpleSession();

        session.Start(null, startCompleted: 99);

        Assert.Equal(4, session.CompletedCount);
        Assert.True(session.IsComplete);
    }

    [Fact]
    public void CountPrayers_counts_without_disturbing_the_live_thread()
    {
        var session = SimpleSession();
        session.Start(null, startCompleted: 2);

        var count = session.CountPrayers(null);

        Assert.Equal(4, count);
        Assert.Equal(2, session.CompletedCount);
    }

    [Fact]
    public void Toggle_advances_the_current_step_in_order()
    {
        var session = SimpleSession();
        session.Start(null);

        var result = session.Toggle(0);

        Assert.Equal(DevotionToggleResult.Advanced, result);
        Assert.Equal(1, session.CompletedCount);
    }

    [Fact]
    public void Toggle_ignores_a_step_that_is_not_current_or_just_prayed()
    {
        var session = SimpleSession();
        session.Start(null);

        var result = session.Toggle(2);

        Assert.Equal(DevotionToggleResult.NoChange, result);
        Assert.Equal(0, session.CompletedCount);
    }

    [Fact]
    public void Toggle_can_step_back_the_most_recently_prayed_step()
    {
        var session = SimpleSession();
        session.Start(null);
        session.Toggle(0);

        var result = session.Toggle(0);

        Assert.Equal(DevotionToggleResult.SteppedBack, result);
        Assert.Equal(0, session.CompletedCount);
    }

    [Fact]
    public void Toggle_reports_completion_on_the_final_step()
    {
        var session = SimpleSession();
        session.Start(null, startCompleted: 3);

        var result = session.Toggle(3);

        Assert.Equal(DevotionToggleResult.Completed, result);
        Assert.True(session.IsComplete);
    }

    [Fact]
    public void CurrentStep_tracks_the_next_prayer_and_is_null_when_complete()
    {
        var session = SimpleSession();
        session.Start(null);

        Assert.Equal("Our Father", session.CurrentStep?.PrayerTitle);

        session.Start(null, startCompleted: 4);
        Assert.Null(session.CurrentStep);
    }

    [Fact]
    public void DescribeCurrentStep_names_the_position_and_title()
    {
        var session = SimpleSession();
        session.Start(null, startCompleted: 1);

        Assert.Equal("Step 2 of 4: Hail Mary.", session.DescribeCurrentStep());
    }

    [Fact]
    public void PrayerStepLabel_describes_position_state_and_note()
    {
        var session = SimpleSession();
        session.Start(null);
        var opening = session.Beads[0];

        var label = session.PrayerStepLabel(opening, done: false, current: true);

        Assert.Equal("Our Father, Slowly, step 1 of 4, current, press to mark done", label);
    }

    [Fact]
    public void Mystery_step_emits_an_announcement_row_followed_by_its_prayer()
    {
        var session = MysterySession();

        session.Start("joyful");

        Assert.Equal(4, session.Beads.Count);
        Assert.Equal("The Annunciation", session.Beads[0].MysteryTitle);
        Assert.False(session.Beads[0].IsPrayer);
        Assert.Equal(-1, session.Beads[0].PrayerOrdinal);
        Assert.True(session.Beads[1].IsPrayer);
        Assert.Equal("The Visitation", session.Beads[2].MysteryTitle);
        Assert.Equal(2, session.PrayerCount);
    }

    [Fact]
    public void IsItemDone_treats_a_mystery_announcement_as_done_once_its_prayer_is_prayed()
    {
        var session = MysterySession();
        session.Start("joyful");

        // Pray the first mystery's prayer (ordinal 0).
        session.Toggle(0);

        Assert.True(session.IsItemDone(0));  // the announcement for the first mystery
        Assert.True(session.IsItemDone(1));  // its prayer
        Assert.False(session.IsItemDone(2)); // the second mystery's announcement
    }

    // A liturgical walkthrough (e.g. the Mass): a section banner followed by an inline-text response
    // that has no cataloged prayer, carrying an explanation and a role.
    private static DevotionSession MassSession()
    {
        var devotion = new Devotion
        {
            Id = "mass-ordinary-form",
            Title = "Walkthrough of the Mass",
            Kind = "mass",
            Sequence =
            [
                new DevotionStep { Kind = "section", Title = "The Introductory Rites", ExplanationHtml = "<p>We gather.</p>" },
                new DevotionStep
                {
                    Kind = "prayer",
                    Title = "The Greeting",
                    TextHtml = "<p>And with your spirit.</p>",
                    Role = "people",
                    ExplanationHtml = "<p>The priest greets us.</p>",
                },
            ],
        };

        return new DevotionSession(devotion, new Dictionary<string, Prayer>(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Start_emits_a_section_banner_row_that_is_not_a_prayer()
    {
        var session = MassSession();

        session.Start(null);

        var banner = session.Beads[0];
        Assert.True(banner.IsSection);
        Assert.False(banner.IsPrayer);
        Assert.Equal("The Introductory Rites", banner.Heading);
        Assert.Equal("<p>We gather.</p>", banner.ExplanationHtml);
    }

    [Fact]
    public void Start_builds_a_prayer_row_from_inline_text_when_there_is_no_catalogued_prayer()
    {
        var session = MassSession();

        session.Start(null);

        // Only the inline response is a prayer row; the section banner is not counted.
        Assert.Equal(1, session.PrayerCount);

        var response = session.Beads[1];
        Assert.True(response.IsPrayer);
        Assert.Equal("The Greeting", response.PrayerTitle);
        Assert.Equal("<p>And with your spirit.</p>", response.PrayerHtml);
        Assert.Equal("people", response.Role);
        Assert.Equal("<p>The priest greets us.</p>", response.ExplanationHtml);
    }

    [Fact]
    public void Start_skips_a_prayer_step_with_neither_prayer_nor_inline_text()
    {
        var devotion = new Devotion
        {
            Id = "empty",
            Title = "Empty",
            Sequence = [new DevotionStep { Kind = "prayer", Title = "Nothing here" }],
        };
        var session = new DevotionSession(devotion, new Dictionary<string, Prayer>(StringComparer.OrdinalIgnoreCase));

        session.Start(null);

        Assert.Empty(session.Beads);
        Assert.Equal(0, session.PrayerCount);
    }
}
