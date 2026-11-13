using Respondeo.Services;

namespace Respondeo.Maui;

public partial class App : Application
{
    private readonly IAppUpdateService _updates;

    public App(IAppUpdateService updates)
    {
        _updates = updates;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MainPage();
        var window = new Window(page) { Title = "Respondeo" };
#if ANDROID && APK_UPDATES
        page.Loaded += async (_, _) => await _updates.CheckAsync();
        window.Activated += async (_, _) =>
        {
            if (page.IsLoaded)
            {
                await _updates.CheckAsync();
            }
        };
#endif
        return window;
    }
}
