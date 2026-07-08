using System.Collections.Concurrent;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// A tiny thread-safe, time-boxed cache of fetched items keyed by (game mode, item id). It exists
/// to absorb bursty repeat lookups - e.g. clicking the same search result twice, or re-opening a
/// detail pane - without hitting the API again, helping stay under the ~60 req/min soft limit.
/// The TTL is short by design so tracked prices stay fresh; the watchlist refresh path bypasses
/// the cache entirely and always fetches live.
/// </summary>
public sealed class ItemCache(TimeSpan ttl)
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    private readonly record struct Entry(Item Item, DateTimeOffset ExpiresAt);

    private static string Key(GameMode mode, string id) => $"{mode.ToApiValue()}:{id}";

    /// <summary>Returns the cached item if present and not expired, else null.</summary>
    public Item? TryGet(GameMode mode, string id, DateTimeOffset now) =>
        _entries.TryGetValue(Key(mode, id), out Entry e) && e.ExpiresAt > now ? e.Item : null;

    /// <summary>Stores/refreshes an item, expiring <see cref="ttl"/> after <paramref name="now"/>.</summary>
    public void Set(GameMode mode, Item item, DateTimeOffset now) =>
        _entries[Key(mode, item.Id)] = new Entry(item, now + ttl);
}
