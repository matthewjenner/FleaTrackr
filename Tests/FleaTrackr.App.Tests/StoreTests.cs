using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>Round-trip and crash-safety tests for the JSON stores (session + watchlist).</summary>
public class StoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "fleatrackr-tests", Guid.NewGuid().ToString("N"));

    public StoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void SessionStore_round_trips_state()
    {
        string path = Path.Combine(_dir, "session.json");
        var store = new SessionStore(path);
        var state = new SessionState
        {
            ActiveTabIndex = 2, WindowWidth = 800, WindowHeight = 600, WindowX = 10, WindowY = 20,
            Maximized = true, GameMode = GameMode.Pve, LastSearchQuery = "ledx", SelectedItemId = "abc",
        };

        store.Save(state);

        new SessionStore(path).Load().Should().Be(state);
    }

    [Fact]
    public void SessionStore_flush_writes_the_debounced_state()
    {
        string path = Path.Combine(_dir, "session.json");
        using var store = new SessionStore(path, debounce: TimeSpan.FromMinutes(5));
        var state = new SessionState { LastSearchQuery = "gpu" };

        store.SaveDebounced(state); // would not fire for 5 minutes on its own
        store.Flush();              // ...but Flush writes it immediately

        new SessionStore(path).Load().LastSearchQuery.Should().Be("gpu");
    }

    [Fact]
    public void Missing_session_file_loads_defaults()
    {
        var store = new SessionStore(Path.Combine(_dir, "does-not-exist.json"));

        store.Load().Should().Be(new SessionState());
    }

    [Fact]
    public void WatchlistStore_round_trips_items_with_rules()
    {
        string path = Path.Combine(_dir, "watchlist.json");
        var store = new WatchlistStore(path);
        var items = new List<WatchedItemConfig>
        {
            new()
            {
                ItemId = "gpu", Name = "Graphics card", ShortName = "GPU", RefreshSeconds = 30,
                Rules = [new AlertRule(AlertKind.PriceDropsBelow, 100_000), new AlertRule(AlertKind.PercentRiseExceeds, 10)],
            },
        };

        store.Save(items);

        IReadOnlyList<WatchedItemConfig> loaded = new WatchlistStore(path).Load();
        loaded.Should().BeEquivalentTo(items);
    }
}
