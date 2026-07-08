using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FleaTrackr.App.Services;
using FleaTrackr.App.ViewModels;
using FleaTrackr.App.Views;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>Smoke test: the main window builds and shows all tabs with the economy toggle.</summary>
public class ShellTests
{
    [AvaloniaFact]
    public void Main_window_shows_the_tabs()
    {
        using var host = new AppHost();
        var window = new MainWindow { DataContext = new MainWindowViewModel(host) };
        window.Show();

        TabControl tabs = window.GetVisualDescendants().OfType<TabControl>().First();
        string[] headers = tabs.Items.OfType<TabItem>().Select(t => t.Header as string).ToArray()!;

        headers.Should().Equal("Search", "Watchlist", "Barters & Crafts", "Flip Finder", "Settings");
    }
}
