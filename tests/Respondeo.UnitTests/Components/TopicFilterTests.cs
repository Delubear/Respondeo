using AngleSharp.Dom;
using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class TopicFilterTests : TestContext
{
    private static readonly string[] Topics = ["Existence of God", "St. Thomas Aquinas"];

    [Fact]
    public void Renders_a_checkbox_for_each_topic()
    {
        var cut = RenderComponent<TopicFilter>(p => p.Add(c => c.Topics, Topics));

        var labels = cut.FindAll("label.topic-filter__option").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(Topics, labels);
    }

    [Fact]
    public void Renders_nothing_when_no_topics()
    {
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, Array.Empty<string>())
            .Add(c => c.Title, "Kind"));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Shows_counts_and_disables_zero_count_options_unless_selected()
    {
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, ["a", "b", "c"])
            .Add(c => c.Selected, new HashSet<string>(["c"], StringComparer.OrdinalIgnoreCase))
            .Add(c => c.Counts, new Dictionary<string, int> { ["a"] = 3 }));

        var options = cut.FindAll("label.topic-filter__option").ToList();
        Assert.Equal("3", options[0].QuerySelector(".topic-filter__count")!.TextContent);
        Assert.False(options[0].QuerySelector("input")!.HasAttribute("disabled"));
        Assert.True(options[1].QuerySelector("input")!.HasAttribute("disabled"));
        Assert.Contains("topic-filter__option--empty", options[1].ClassName);
        Assert.False(options[2].QuerySelector("input")!.HasAttribute("disabled"));
    }

    [Fact]
    public void Clicking_a_topic_adds_it_to_the_selection()
    {
        IReadOnlySet<string>? selection = null;
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, Topics)
            .Add(c => c.SelectedChanged, s => selection = s));

        cut.FindAll("label.topic-filter__option").First(b => b.TextContent.Trim() == "St. Thomas Aquinas").QuerySelector("input")!.Change(true);

        Assert.NotNull(selection);
        Assert.Equal(["St. Thomas Aquinas"], selection!);
    }

    [Fact]
    public void Clicking_a_selected_topic_removes_it()
    {
        IReadOnlySet<string>? selection = null;
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, Topics)
            .Add(c => c.Selected, new HashSet<string>(["St. Thomas Aquinas"], StringComparer.OrdinalIgnoreCase))
            .Add(c => c.SelectedChanged, s => selection = s));

        cut.FindAll("label.topic-filter__option").First(b => b.TextContent.Trim() == "St. Thomas Aquinas").QuerySelector("input")!.Change(false);

        Assert.NotNull(selection);
        Assert.Empty(selection!);
    }

    [Fact]
    public void Marks_selected_topics_as_checked()
    {
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, Topics)
            .Add(c => c.Selected, new HashSet<string>(["St. Thomas Aquinas"], StringComparer.OrdinalIgnoreCase)));

        var checkbox = cut.FindAll("label.topic-filter__option")
            .First(b => b.TextContent.Trim() == "St. Thomas Aquinas")
            .QuerySelector("input")!;
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void Clear_button_appears_only_with_a_selection_and_empties_it()
    {
        IReadOnlySet<string>? selection = null;
        var cut = RenderComponent<TopicFilter>(p => p
            .Add(c => c.Topics, Topics)
            .Add(c => c.Selected, new HashSet<string>(["St. Thomas Aquinas"], StringComparer.OrdinalIgnoreCase))
            .Add(c => c.SelectedChanged, s => selection = s));

        cut.Find("button.topic-filter__clear").Click();

        Assert.NotNull(selection);
        Assert.Empty(selection!);
    }

    [Fact]
    public void Omits_the_clear_button_without_a_selection()
    {
        var cut = RenderComponent<TopicFilter>(p => p.Add(c => c.Topics, Topics));

        Assert.Empty(cut.FindAll("button.topic-filter__clear"));
    }
}
