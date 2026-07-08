using FleaTrackr.Core;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.Services;

/// <summary>
/// Owns the watchlist: the persisted set of <see cref="WatchedItemConfig"/> plus the
/// <see cref="RefreshScheduler"/> that keeps their prices live. This is the one place the rest of
/// the app touches the watchlist - the Search tab adds to it, the Watchlist tab renders and edits
/// it. Every mutation persists (atomically) and is reflected into the scheduler. Price updates and
/// alerts are forwarded from the scheduler and therefore arrive off the UI thread; subscribers
/// marshal as needed.
/// </summary>
public sealed class WatchlistService : IDisposable
{
    private readonly WatchlistStore _store;
    private readonly RefreshScheduler _scheduler;
    private readonly List<WatchedItemConfig> _items;
    private readonly Lock _gate = new();

    public WatchlistService(ITarkovApi api, Func<GameMode> mode, WatchlistStore store, TimeProvider? time = null)
    {
        _store = store;
        _items = [.. store.Load()];
        _scheduler = new RefreshScheduler(api, mode, time);
        _scheduler.Updated += (id, snap) => Updated?.Invoke(id, snap);
        _scheduler.AlertTriggered += a => AlertTriggered?.Invoke(a);

        foreach (WatchedItemConfig cfg in _items)
            _scheduler.Set(cfg);
        _scheduler.Start();
    }

    /// <summary>The watched items in display order (a snapshot copy).</summary>
    public IReadOnlyList<WatchedItemConfig> Items
    {
        get { lock (_gate) return _items.ToList(); }
    }

    public bool IsWatched(string itemId)
    {
        lock (_gate) return _items.Any(i => i.ItemId == itemId);
    }

    /// <summary>A new item was added to the watchlist.</summary>
    public event Action<WatchedItemConfig>? Added;

    /// <summary>An item was removed (by id).</summary>
    public event Action<string>? Removed;

    /// <summary>An item's config (interval or rules) changed.</summary>
    public event Action<WatchedItemConfig>? Changed;

    /// <summary>A fresh price snapshot arrived (id, snapshot). Off the UI thread.</summary>
    public event Action<string, PriceSnapshot>? Updated;

    /// <summary>An alert tripped. Off the UI thread.</summary>
    public event Action<TriggeredAlert>? AlertTriggered;

    /// <summary>Adds an item from a search result with the given refresh cadence (no-op if present).</summary>
    public void AddFromItem(Item item, int refreshSeconds)
    {
        var cfg = new WatchedItemConfig
        {
            ItemId = item.Id,
            Name = item.Name,
            ShortName = item.ShortName,
            IconLink = item.IconLink,
            RefreshSeconds = refreshSeconds,
        };
        Add(cfg);
    }

    /// <summary>Adds a fully-formed config (no-op if the item is already watched).</summary>
    public void Add(WatchedItemConfig config)
    {
        lock (_gate)
        {
            if (_items.Any(i => i.ItemId == config.ItemId)) return;
            _items.Add(config);
            Persist();
        }
        _scheduler.Set(config);
        Added?.Invoke(config);
    }

    public void Remove(string itemId)
    {
        bool removed;
        lock (_gate)
        {
            removed = _items.RemoveAll(i => i.ItemId == itemId) > 0;
            if (removed) Persist();
        }
        if (!removed) return;
        _scheduler.Remove(itemId);
        Removed?.Invoke(itemId);
    }

    /// <summary>Changes an item's auto-refresh cadence (seconds; 0 = manual only).</summary>
    public void SetInterval(string itemId, int refreshSeconds) =>
        Mutate(itemId, cfg => cfg with { RefreshSeconds = refreshSeconds });

    /// <summary>Replaces an item's alert rules.</summary>
    public void SetRules(string itemId, IReadOnlyList<AlertRule> rules) =>
        Mutate(itemId, cfg => cfg with { Rules = rules });

    /// <summary>Requests an immediate refresh of one item, ignoring its interval.</summary>
    public void RefreshNow(string itemId) => _scheduler.RefreshNow(itemId);

    /// <summary>Re-baselines all items after a PVP/PVE switch so the next tick refetches live.</summary>
    public void InvalidateForModeChange() => _scheduler.InvalidateAll();

    private void Mutate(string itemId, Func<WatchedItemConfig, WatchedItemConfig> update)
    {
        WatchedItemConfig? updated = null;
        lock (_gate)
        {
            int idx = _items.FindIndex(i => i.ItemId == itemId);
            if (idx < 0) return;
            updated = update(_items[idx]);
            _items[idx] = updated;
            Persist();
        }
        _scheduler.Set(updated);
        Changed?.Invoke(updated);
    }

    // Callers hold _gate.
    private void Persist() => _store.Save(_items.ToList());

    public void Dispose() => _scheduler.Dispose();
}
