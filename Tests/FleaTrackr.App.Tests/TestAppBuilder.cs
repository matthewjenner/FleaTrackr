using Avalonia;
using Avalonia.Headless;
using FleaTrackr.App.Tests;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace FleaTrackr.App.Tests;

/// <summary>Headless Avalonia app used by [AvaloniaFact] UI tests.</summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<FleaTrackr.App.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
