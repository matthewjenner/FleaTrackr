using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>
/// Drives <see cref="RefreshScheduler.TickAsync"/> with a manual clock to verify due-based
/// refreshing, batched fetches, and edge-triggered alerts - all without real timers.
/// </summary>
public class RefreshSchedulerTests
{
    private static Item Gpu(int fleaSell, double? change = null) => new()
    {
        Id = "gpu", Name = "Graphics card",
        SellFor = [new VendorPrice(VendorPrice.FleaMarketVendorName, fleaSell, "RUB", fleaSell)],
        ChangeLast48hPercent = change,
    };

    private static WatchedItemConfig Config(int interval, params AlertRule[] rules) => new()
    {
        ItemId = "gpu", Name = "Graphics card", RefreshSeconds = interval, Rules = rules,
    };

    [Fact]
    public async Task Refreshes_when_due_and_fires_alert_on_the_crossing()
    {
        var time = new ControllableTime(DateTimeOffset.UnixEpoch);
        var api = new FakeApi().SetItem(Gpu(150_000));
        using var scheduler = new RefreshScheduler(api, () => GameMode.Pvp, time);

        var updates = new List<PriceSnapshot>();
        var alerts = new List<TriggeredAlert>();
        scheduler.Updated += (_, s) => updates.Add(s);
        scheduler.AlertTriggered += alerts.Add;

        scheduler.Set(Config(60, new AlertRule(AlertKind.PriceDropsBelow, 100_000)));

        // Initial fetch: 150k, above threshold -> update, no alert.
        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        updates.Should().ContainSingle();
        alerts.Should().BeEmpty();

        // 30s later: not yet due (60s interval) -> no fetch.
        time.Now = time.Now.AddSeconds(30);
        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        updates.Should().HaveCount(1);

        // Price drops below the threshold and the interval has elapsed -> update + one alert.
        api.SetItem(Gpu(90_000));
        time.Now = time.Now.AddSeconds(31);
        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        updates.Should().HaveCount(2);
        alerts.Should().ContainSingle().Which.Rule.Kind.Should().Be(AlertKind.PriceDropsBelow);
    }

    [Fact]
    public async Task Due_items_are_fetched_in_one_batch_call()
    {
        var time = new ControllableTime(DateTimeOffset.UnixEpoch);
        var api = new FakeApi()
            .SetItem(new Item { Id = "a", Name = "A" })
            .SetItem(new Item { Id = "b", Name = "B" });
        using var scheduler = new RefreshScheduler(api, () => GameMode.Pvp, time);
        scheduler.Set(new WatchedItemConfig { ItemId = "a", Name = "A", RefreshSeconds = 60 });
        scheduler.Set(new WatchedItemConfig { ItemId = "b", Name = "B", RefreshSeconds = 60 });

        await scheduler.TickAsync(TestContext.Current.CancellationToken);

        api.BatchCallCount.Should().Be(1, "both due items should be coalesced into a single fetch");
    }

    [Fact]
    public async Task InvalidateAll_forces_a_refetch_on_the_next_tick()
    {
        var time = new ControllableTime(DateTimeOffset.UnixEpoch);
        var api = new FakeApi().SetItem(Gpu(150_000));
        using var scheduler = new RefreshScheduler(api, () => GameMode.Pvp, time);
        scheduler.Set(Config(600));

        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        api.BatchCallCount.Should().Be(1);

        // Well within the 600s interval, so normally not due...
        time.Now = time.Now.AddSeconds(5);
        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        api.BatchCallCount.Should().Be(1);

        // ...but a mode switch invalidates and forces a refetch.
        scheduler.InvalidateAll();
        await scheduler.TickAsync(TestContext.Current.CancellationToken);
        api.BatchCallCount.Should().Be(2);
    }
}
