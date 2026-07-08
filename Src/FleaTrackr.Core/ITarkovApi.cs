using FleaTrackr.Core.Models;

namespace FleaTrackr.Core;

/// <summary>
/// The network contract for the tarkov.dev GraphQL API, defined in Core as an interface only so
/// that all consuming logic (search, watchlist refresh, profit, flips) is testable against an
/// in-memory fake with no live API. The real implementation (<c>TarkovApiClient</c>) lives in the
/// App project. Every method takes a <see cref="GameMode"/> because the API serves separate
/// PVP/PVE economies.
/// </summary>
public interface ITarkovApi
{
    /// <summary>Searches items by (partial) name, returning up to <paramref name="limit"/> matches.</summary>
    Task<IReadOnlyList<Item>> SearchItemsAsync(
        string query, GameMode mode, int limit = 20, CancellationToken ct = default);

    /// <summary>Fetches a single item by its API id, or null if not found.</summary>
    Task<Item?> GetItemAsync(string id, GameMode mode, CancellationToken ct = default);

    /// <summary>
    /// Fetches many items by id in one request. Used by the watchlist refresh to coalesce all
    /// items due at the same time into a single query and stay under the API rate limit.
    /// </summary>
    Task<IReadOnlyList<Item>> GetItemsByIdsAsync(
        IReadOnlyList<string> ids, GameMode mode, CancellationToken ct = default);

    /// <summary>Barter trades that produce the given item (its <c>bartersFor</c>).</summary>
    Task<IReadOnlyList<Barter>> GetBartersForAsync(string id, GameMode mode, CancellationToken ct = default);

    /// <summary>Hideout crafts that produce the given item (its <c>craftsFor</c>).</summary>
    Task<IReadOnlyList<Craft>> GetCraftsForAsync(string id, GameMode mode, CancellationToken ct = default);

    /// <summary>Flea price history for the given item over the last <paramref name="days"/> days.</summary>
    Task<IReadOnlyList<HistoricalPricePoint>> GetPriceHistoryAsync(
        string id, int days, GameMode mode, CancellationToken ct = default);
}
