using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Tests;

/// <summary>Programmable in-memory <see cref="ITarkovApi"/> for App-side scheduler/service tests.</summary>
public sealed class FakeApi : ITarkovApi
{
    private readonly Dictionary<string, Item> _items = new();

    public int BatchCallCount { get; private set; }

    /// <summary>Adds or replaces an item (call again between ticks to change its price).</summary>
    public FakeApi SetItem(Item item)
    {
        _items[item.Id] = item;
        return this;
    }

    public Task<IReadOnlyList<Item>> SearchItemsAsync(string query, GameMode mode, int limit = 20, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Item>>(_items.Values.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<Item?> GetItemAsync(string id, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Item>> GetItemsByIdsAsync(IReadOnlyList<string> ids, GameMode mode, CancellationToken ct = default)
    {
        BatchCallCount++;
        return Task.FromResult<IReadOnlyList<Item>>(ids.Where(_items.ContainsKey).Select(id => _items[id]).ToList());
    }

    public Task<IReadOnlyList<Barter>> GetBartersForAsync(string id, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Barter>>([]);

    public Task<IReadOnlyList<Craft>> GetCraftsForAsync(string id, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Craft>>([]);

    public Task<IReadOnlyList<HistoricalPricePoint>> GetPriceHistoryAsync(string id, int days, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<HistoricalPricePoint>>([]);
}

/// <summary>A <see cref="TimeProvider"/> whose clock is set manually, for deterministic tick tests.</summary>
public sealed class ControllableTime(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;
    public override DateTimeOffset GetUtcNow() => Now;
}
