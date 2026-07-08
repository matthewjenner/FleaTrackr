using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>Verifies the watchlist service persists every mutation and dedupes by item id.</summary>
public class WatchlistServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "fleatrackr-tests", Guid.NewGuid().ToString("N"));
    private readonly string _path;

    public WatchlistServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "watchlist.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private WatchlistService NewService() =>
        new(new FakeApi(), () => GameMode.Pvp, new WatchlistStore(_path), new ControllableTime(DateTimeOffset.UnixEpoch));

    private IReadOnlyList<WatchedItemConfig> Persisted() => new WatchlistStore(_path).Load();

    [Fact]
    public void Add_persists_and_is_idempotent()
    {
        using var svc = NewService();
        int added = 0;
        svc.Added += _ => added++;

        var cfg = new WatchedItemConfig { ItemId = "gpu", Name = "Graphics card", RefreshSeconds = 60 };
        svc.Add(cfg);
        svc.Add(cfg); // duplicate id -> ignored

        svc.IsWatched("gpu").Should().BeTrue();
        added.Should().Be(1);
        Persisted().Should().ContainSingle();
    }

    [Fact]
    public void SetInterval_and_SetRules_persist()
    {
        using var svc = NewService();
        svc.Add(new WatchedItemConfig { ItemId = "gpu", Name = "Graphics card", RefreshSeconds = 60 });

        svc.SetInterval("gpu", 30);
        svc.SetRules("gpu", [new AlertRule(AlertKind.PriceDropsBelow, 100_000)]);

        WatchedItemConfig saved = Persisted().Single();
        saved.RefreshSeconds.Should().Be(30);
        saved.Rules.Should().ContainSingle().Which.Kind.Should().Be(AlertKind.PriceDropsBelow);
    }

    [Fact]
    public void Remove_persists_the_deletion()
    {
        using var svc = NewService();
        svc.Add(new WatchedItemConfig { ItemId = "gpu", Name = "Graphics card" });

        svc.Remove("gpu");

        svc.IsWatched("gpu").Should().BeFalse();
        Persisted().Should().BeEmpty();
    }

    [Fact]
    public void Items_reload_from_disk_on_construction()
    {
        using (var first = NewService())
            first.Add(new WatchedItemConfig { ItemId = "gpu", Name = "Graphics card", RefreshSeconds = 45 });

        using var second = NewService();
        second.Items.Should().ContainSingle().Which.RefreshSeconds.Should().Be(45);
    }
}
