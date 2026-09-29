using Bunit;
using Respondeo.Components;

namespace Respondeo.UnitTests.Components;

public class DialogTests : TestContext
{
    public DialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Dialog> Render(Action<ComponentParameterCollectionBuilder<Dialog>>? configure = null) =>
        RenderComponent<Dialog>(p =>
        {
            p.Add(c => c.Title, "A title");
            p.AddChildContent("<p>Body text</p>");
            configure?.Invoke(p);
        });

    [Fact]
    public void Renders_title_and_body_with_aria_labelling()
    {
        var cut = Render();

        var title = cut.Find(".dialog__title");
        Assert.Equal("A title", title.TextContent);

        var dialog = cut.Find("dialog.dialog");
        Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Contains("Body text", cut.Find(".dialog__body").TextContent);
    }

    [Fact]
    public void Omits_eyebrow_and_footer_when_not_provided()
    {
        var cut = Render();

        Assert.Empty(cut.FindAll(".dialog__eyebrow"));
        Assert.Empty(cut.FindAll(".dialog__foot"));
    }

    [Fact]
    public void Renders_eyebrow_and_footer_when_provided()
    {
        var cut = Render(p => p
            .Add(c => c.Eyebrow, "Orientation")
            .Add(c => c.Footer, "<button>Done</button>"));

        Assert.Equal("Orientation", cut.Find(".dialog__eyebrow").TextContent);
        Assert.Contains("Done", cut.Find(".dialog__foot").TextContent);
    }

    [Fact]
    public void Applies_extra_class_and_max_width()
    {
        var cut = Render(p => p
            .Add(c => c.Class, "dialog--roomy")
            .Add(c => c.MaxWidth, "38rem"));

        var dialog = cut.Find("dialog.dialog");
        Assert.Contains("dialog--roomy", dialog.ClassList);
        Assert.Contains("--dialog-max-width: 38rem;", dialog.GetAttribute("style"));
    }

    [Fact]
    public async Task Show_invokes_the_respondeoDialog_helper()
    {
        var cut = Render();

        await cut.InvokeAsync(() => cut.Instance.Show());

        Assert.Single(JSInterop.Invocations["respondeoDialog.show"]);
    }

    [Fact]
    public async Task Close_invokes_the_respondeoDialog_helper()
    {
        var cut = Render();

        await cut.InvokeAsync(() => cut.Instance.Close());

        Assert.Single(JSInterop.Invocations["respondeoDialog.close"]);
    }

    [Fact]
    public void Close_button_invokes_the_respondeoDialog_helper()
    {
        var cut = Render();

        cut.Find(".dialog__close").Click();

        Assert.Single(JSInterop.Invocations["respondeoDialog.close"]);
    }
}
