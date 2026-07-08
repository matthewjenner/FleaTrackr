using System.Collections.Concurrent;
using FleaTrackr.Core;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.Services;

/// <summary>
/// Refreshes watched items on their own cadences without blocking the UI. A single background loop
/// ticks once a second; on each tick <see cref="RefreshPolicy"/> selects the items due and they are
/// fetched in one coalesced batch call, so a fast 30s item and a slow 5m item coexist without a
/// timer each, and bursts stay under the API rate limit. All API work runs off the UI thread;
/// <see cref="Updated"/> and <see cref="AlertTriggered"/> fire from the background thread, so
/// subscribers (view models) marshal to the UI thread themselves.
///
/// The public <see cref="TickAsync"/> runs exactly one refresh cycle and is what the tests drive,
/// so scheduling behaviour is verified without real timers.
/// </summary>
public sealed class RefreshScheduler : IDisposable
{
    private sealed class Entry
    {
        public required WatchedItemConfig Config;
        public DateTimeOffset? LastRefreshed;
        public PriceSnapshot? Last;
    }

    private readonly ITarkovApi _api;
    private readonly Func<GameMode> _mode;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public RefreshScheduler(ITarkovApi api, Func<GameMode> mode, TimeProvider? time = null)
    {
        _api = api;
        _mode = mode;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>A fresh price snapshot arrived for an item (id, snapshot). Fired off the UI thread.</summary>
    public event Action<string, PriceSnapshot>? Updated;

    /// <summary>An alert rule tripped. Fired off the UI thread.</summary>
    public event Action<TriggeredAlert>? AlertTriggered;

    /// <summary>Adds a new watched item or replaces the config (interval/rules) of an existing one.</summary>
    public void Set(WatchedItemConfig config) =>
        _entries.AddOrUpdate(config.ItemId,
            _ => new Entry { Config = config },
            (_, existing) => { existing.Config = config; return existing; });

    /// <summary>Stops tracking an item.</summary>
    public void Remove(string itemId) => _entries.TryRemove(itemId, out _);

    /// <summary>
    /// Forces every item to be considered stale so the next tick refetches them all. Called when
    /// the PVP/PVE mode changes, since prices differ per economy and the cached snapshots no longer
    /// apply. Clears last snapshots too so alerts re-baseline against the new economy.
    /// </summary>
    public void InvalidateAll()
    {
        foreach (Entry e in _entries.Values)
        {
            e.LastRefreshed = null;
            e.Last = null;
        }
    }

    /// <summary>Marks a single item due immediately (backs the per-row "refresh now" action).</summary>
    public void RefreshNow(string itemId)
    {
        if (_entries.TryGetValue(itemId, out Entry? e)) e.LastRefreshed = null;
    }

    /// <summary>Starts the background refresh loop. Idempotent.</summary>
    public void Start() => _loop ??= Task.Run(() => RunLoopAsync(_cts.Token));

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), _time);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await TickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Runs one refresh cycle: fetch all due items in a batch, snapshot, and raise events.</summary>
    public async Task TickAsync(CancellationToken ct = default)
    {
        DateTimeOffset now = _time.GetUtcNow();

        IReadOnlyList<string> due = RefreshPolicy.SelectDue(
            _entries.Values.Select(e => new RefreshState(e.Config.ItemId, e.Config.RefreshSeconds, e.LastRefreshed)),
            now);
        if (due.Count == 0) return;

        // Mark due items as attempted now, so a failed/empty fetch does not hot-loop retrying them.
        foreach (string id in due)
            if (_entries.TryGetValue(id, out Entry? e)) e.LastRefreshed = now;

        IReadOnlyList<Item> items;
        try
        {
            items = await _api.GetItemsByIdsAsync(due, _mode(), ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return; // Transient failure - the items stay tracked and retry next interval.
        }

        foreach (Item item in items)
        {
            if (!_entries.TryGetValue(item.Id, out Entry? entry)) continue;

            var snapshot = PriceSnapshot.FromItem(item, now);
            IReadOnlyList<TriggeredAlert> alerts =
                AlertEvaluator.Evaluate(entry.Config, entry.Last, snapshot);
            entry.Last = snapshot;

            Updated?.Invoke(item.Id, snapshot);
            foreach (TriggeredAlert alert in alerts)
                AlertTriggered?.Invoke(alert);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
