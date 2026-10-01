using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class AccordionItemTests : TestContext
{
    [Fact]
    public void Button_mode_renders_an_accessible_button_trigger()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Mode, AccordionItem.AccordionTriggerMode.Button)
            .Add(c => c.Title, "Section One")
            .Add(c => c.HeaderId, "header-one")
            .Add(c => c.PanelId, "panel-one"));

        var trigger = cut.Find("button.accordion__trigger");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal("panel-one", trigger.GetAttribute("aria-controls"));
        Assert.Contains("Section One", trigger.TextContent);
        Assert.Empty(cut.FindAll(".accordion__panel"));
    }

    [Fact]
    public void Button_mode_renders_the_panel_only_when_open()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Mode, AccordionItem.AccordionTriggerMode.Button)
            .Add(c => c.Title, "Section One")
            .Add(c => c.Open, true)
            .Add(c => c.PanelId, "panel-one")
            .Add(c => c.HeaderId, "header-one")
            .AddChildContent("<p>Body one</p>"));

        var trigger = cut.Find("button.accordion__trigger");
        Assert.Equal("true", trigger.GetAttribute("aria-expanded"));

        var panel = cut.Find(".accordion__panel");
        Assert.Contains("content-body", panel.ClassList);
        Assert.Contains("Body one", panel.TextContent);
    }

    [Fact]
    public void Details_mode_renders_a_native_details_with_the_id_and_always_present_panel()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Mode, AccordionItem.AccordionTriggerMode.Details)
            .Add(c => c.Id, "article-1")
            .Add(c => c.Title, "Whether God is a body?")
            .AddChildContent("<p>I answer that God is not a body.</p>"));

        var details = cut.Find("details.accordion__item");
        Assert.Equal("article-1", details.GetAttribute("id"));
        Assert.False(details.HasAttribute("open"));

        // In details mode the panel is always in the DOM; native open drives visibility.
        var panel = cut.Find(".accordion__panel");
        Assert.Contains("I answer that God is not a body.", panel.TextContent);
        Assert.NotNull(cut.Find("summary.accordion__trigger"));
    }

    [Fact]
    public void Details_mode_sets_the_open_attribute_when_open()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Mode, AccordionItem.AccordionTriggerMode.Details)
            .Add(c => c.Id, "article-1")
            .Add(c => c.Title, "Whether God is a body?")
            .Add(c => c.Open, true));

        Assert.True(cut.Find("details.accordion__item").HasAttribute("open"));
    }

    [Fact]
    public void Renders_the_eyebrow_and_summary_when_present()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Title, "Section One")
            .Add(c => c.Eyebrow, "Article 1")
            .Add(c => c.Summary, "A short description"));

        Assert.Equal("Article 1", cut.Find(".accordion__eyebrow").TextContent);
        Assert.Equal("A short description", cut.Find(".accordion__summary").TextContent);
    }

    [Fact]
    public void Omits_the_eyebrow_and_summary_when_not_provided()
    {
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Title, "Section One"));

        Assert.Empty(cut.FindAll(".accordion__eyebrow"));
        Assert.Empty(cut.FindAll(".accordion__summary"));
    }

    [Fact]
    public void Marker_reflects_the_open_state()
    {
        var collapsed = RenderComponent<AccordionItem>(p => p.Add(c => c.Title, "Section One"));
        Assert.Equal("+", collapsed.Find(".accordion__marker").TextContent);

        var open = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Title, "Section One")
            .Add(c => c.Open, true)
            .Add(c => c.HeaderId, "header-one")
            .Add(c => c.PanelId, "panel-one"));
        Assert.Equal("\u2212", open.Find(".accordion__marker").TextContent);
    }

    [Fact]
    public void Clicking_the_trigger_raises_OnToggle()
    {
        var toggled = 0;
        var cut = RenderComponent<AccordionItem>(p => p
            .Add(c => c.Title, "Section One")
            .Add(c => c.HeaderId, "header-one")
            .Add(c => c.PanelId, "panel-one")
            .Add(c => c.OnToggle, () => toggled++));

        cut.Find("button.accordion__trigger").Click();

        Assert.Equal(1, toggled);
    }
}
