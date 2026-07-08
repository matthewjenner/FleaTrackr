using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Tests;

/// <summary>Programmable in-memory <see cref="ITarkovApi"/> for App-side scheduler/service tests.</summary>
public sealed class FakeApi : ITarkovApi
{
    private readonly Dictionary<string, Item> _items = new();
    private readonly Dictionary<string, List<Barter>> _barters = new();
    private readonly Dictionary<string, List<Craft>> _crafts = new();

    public int BatchCallCount { get; private set; }

    /// <summary>Adds or replaces an item (call again between ticks to change its price).</summary>
    public FakeApi SetItem(Item item)
    {
        _items[item.Id] = item;
        return this;
    }

    public FakeApi SetBarters(string id, params Barter[] barters)
    {
        _barters[id] = barters.ToList();
        return this;
    }

    public FakeApi SetCrafts(string id, params Craft[] crafts)
    {
        _crafts[id] = crafts.ToList();
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

    public Task<IReadOnlyList<Item>> GetItemsPageAsync(int limit, int offset, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Item>>(_items.Values.Skip(offset).Take(limit).ToList());

    public Task<IReadOnlyList<Barter>> GetBartersForAsync(string id, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Barter>>(_barters.GetValueOrDefault(id) ?? []);

    public Task<IReadOnlyList<Craft>> GetCraftsForAsync(string id, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Craft>>(_crafts.GetValueOrDefault(id) ?? []);

    public Task<IReadOnlyList<HistoricalPricePoint>> GetPriceHistoryAsync(string id, int days, GameMode mode, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<HistoricalPricePoint>>([]);
}

/// <summary>A <see cref="TimeProvider"/> whose clock is set manually, for deterministic tick tests.</summary>
public sealed class ControllableTime(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;
    public override DateTimeOffset GetUtcNow() => Now;
}
