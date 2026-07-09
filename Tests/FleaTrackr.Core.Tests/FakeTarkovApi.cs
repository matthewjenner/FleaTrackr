using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.Core.Tests;

/// <summary>
/// In-memory <see cref="ITarkovApi"/> for testing everything that consumes the API (watchlist
/// refresh, profit, flips) with no network. Seed items/barters/crafts/history, then assert on how
/// the code under test uses them. Records the game mode of the last call for toggle assertions.
/// </summary>
public sealed class FakeTarkovApi : ITarkovApi
{
    private readonly Dictionary<string, Item> _items = new();
    private readonly Dictionary<string, List<Barter>> _barters = new();
    private readonly Dictionary<string, List<Craft>> _crafts = new();
    private readonly Dictionary<string, List<HistoricalPricePoint>> _history = new();

    /// <summary>The game mode passed to the most recent call, for verifying the PVP/PVE toggle.</summary>
    public GameMode? LastMode { get; private set; }

    /// <summary>How many item fetches (search + by-id) have been made, for rate-limit assertions.</summary>
    public int FetchCount { get; private set; }

    public FakeTarkovApi Add(Item item, IEnumerable<Barter>? barters = null,
        IEnumerable<Craft>? crafts = null, IEnumerable<HistoricalPricePoint>? history = null)
    {
        _items[item.Id] = item;
        if (barters is not null) _barters[item.Id] = barters.ToList();
        if (crafts is not null) _crafts[item.Id] = crafts.ToList();
        if (history is not null) _history[item.Id] = history.ToList();
        return this;
    }

    public Task<IReadOnlyList<Item>> SearchItemsAsync(
        string query, GameMode mode, int limit = 20, CancellationToken ct = default)
    {
        LastMode = mode;
        FetchCount++;
        IReadOnlyList<Item> hits = _items.Values
            .Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit).ToList();
        return Task.FromResult(hits);
    }

    public Task<Item?> GetItemAsync(string id, GameMode mode, CancellationToken ct = default)
    {
        LastMode = mode;
        FetchCount++;
        return Task.FromResult(_items.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<Item>> GetItemsByIdsAsync(
        IReadOnlyList<string> ids, GameMode mode, CancellationToken ct = default)
    {
        LastMode = mode;
        FetchCount++;
        IReadOnlyList<Item> hits = ids.Where(_items.ContainsKey).Select(id => _items[id]).ToList();
        return Task.FromResult(hits);
    }

    public Task<IReadOnlyList<Item>> GetItemsPageAsync(
        int limit, int offset, GameMode mode, CancellationToken ct = default)
    {
        LastMode = mode;
        FetchCount++;
        IReadOnlyList<Item> page = _items.Values.Skip(offset).Take(limit).ToList();
        return Task.FromResult(page);
    }

    public Task<ItemTrades> GetItemTradesAsync(string id, GameMode mode, CancellationToken ct = default)
    {
        LastMode = mode;
        return Task.FromResult(new ItemTrades(
            _barters.GetValueOrDefault(id) ?? [],
            _crafts.GetValueOrDefault(id) ?? [],
            [], []));
    }

    public Task<IReadOnlyList<HistoricalPricePoint>> GetPriceHistoryAsync(
        string id, int days, GameMode mode, CancellationToken ct = default)
    {
        LastMode = mode;
        return Task.FromResult<IReadOnlyList<HistoricalPricePoint>>(_history.GetValueOrDefault(id) ?? []);
    }
}
