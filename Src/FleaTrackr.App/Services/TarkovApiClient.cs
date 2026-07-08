using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// The live <see cref="ITarkovApi"/> implementation: posts GraphQL to the tarkov.dev API over a
/// single pooled <see cref="HttpClient"/>, passing the selected <see cref="GameMode"/> as a typed
/// GraphQL variable. Handles the API's ~60 req/min soft limit by retrying HTTP 429 / transient 5xx
/// with exponential backoff, and de-dupes bursty single-item lookups through a short-TTL
/// <see cref="ItemCache"/>. Watchlist batch refreshes bypass the cache and always fetch live.
/// </summary>
public sealed class TarkovApiClient : ITarkovApi, IDisposable
{
    private const string Endpoint = "https://api.tarkov.dev/graphql";
    private const int MaxRetries = 4;

    // The set of price/identity fields fetched for every item, wherever it appears.
    private const string ItemFields = """
        fragment F on Item {
          id name shortName basePrice avg24hPrice lastLowPrice low24hPrice high24hPrice
          changeLast48hPercent lastOfferCount minLevelForFlea updated iconLink wikiLink
          buyFor { price currency priceRUB vendor { name } }
          sellFor { price currency priceRUB vendor { name } }
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly ItemCache _cache;
    private readonly TimeProvider _time;

    /// <param name="http">Shared HttpClient; if null one is created and owned by this client.</param>
    /// <param name="cacheTtl">How long single-item lookups are cached. Default 15s.</param>
    /// <param name="time">Clock, injectable for tests. Defaults to the system clock.</param>
    public TarkovApiClient(HttpClient? http = null, TimeSpan? cacheTtl = null, TimeProvider? time = null)
    {
        _ownsHttp = http is null;
        _http = http ?? new HttpClient();
        _cache = new ItemCache(cacheTtl ?? TimeSpan.FromSeconds(15));
        _time = time ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<Item>> SearchItemsAsync(
        string query, GameMode mode, int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        const string gql = ItemFields + """

            query Search($name: String!, $mode: GameMode!, $limit: Int!) {
              items(name: $name, gameMode: $mode, limit: $limit) { ...F }
            }
            """;

        ItemsData data = await PostAsync<ItemsData>(gql, new()
        {
            ["name"] = query,
            ["mode"] = mode.ToApiValue(),
            ["limit"] = limit,
        }, ct);

        return MapAndCache(data.Items, mode);
    }

    public async Task<Item?> GetItemAsync(string id, GameMode mode, CancellationToken ct = default)
    {
        if (_cache.TryGet(mode, id, _time.GetUtcNow()) is { } cached) return cached;

        IReadOnlyList<Item> items = await FetchByIdsAsync([id], mode, ct);
        return items.Count > 0 ? items[0] : null;
    }

    public Task<IReadOnlyList<Item>> GetItemsByIdsAsync(
        IReadOnlyList<string> ids, GameMode mode, CancellationToken ct = default) =>
        ids.Count == 0 ? Task.FromResult<IReadOnlyList<Item>>([]) : FetchByIdsAsync(ids, mode, ct);

    private async Task<IReadOnlyList<Item>> FetchByIdsAsync(
        IReadOnlyList<string> ids, GameMode mode, CancellationToken ct)
    {
        const string gql = ItemFields + """

            query ByIds($ids: [ID]!, $mode: GameMode!) {
              items(ids: $ids, gameMode: $mode) { ...F }
            }
            """;

        ItemsData data = await PostAsync<ItemsData>(gql, new()
        {
            ["ids"] = ids,
            ["mode"] = mode.ToApiValue(),
        }, ct);

        return MapAndCache(data.Items, mode);
    }

    public async Task<IReadOnlyList<Item>> GetItemsPageAsync(
        int limit, int offset, GameMode mode, CancellationToken ct = default)
    {
        const string gql = ItemFields + """

            query Page($limit: Int!, $offset: Int!, $mode: GameMode!) {
              items(limit: $limit, offset: $offset, gameMode: $mode) { ...F }
            }
            """;

        ItemsData data = await PostAsync<ItemsData>(gql, new()
        {
            ["limit"] = limit,
            ["offset"] = offset,
            ["mode"] = mode.ToApiValue(),
        }, ct);

        return MapAndCache(data.Items, mode);
    }

    public async Task<IReadOnlyList<Barter>> GetBartersForAsync(
        string id, GameMode mode, CancellationToken ct = default)
    {
        const string gql = ItemFields + """

            query Barters($ids: [ID]!, $mode: GameMode!) {
              items(ids: $ids, gameMode: $mode) {
                bartersFor {
                  trader { name } level
                  requiredItems { count item { ...F } }
                  rewardItems { count item { ...F } }
                }
              }
            }
            """;

        ItemsData data = await PostAsync<ItemsData>(gql, new()
        {
            ["ids"] = new[] { id },
            ["mode"] = mode.ToApiValue(),
        }, ct);

        return (data.Items?.FirstOrDefault()?.BartersFor ?? [])
            .Select(b => b.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<Craft>> GetCraftsForAsync(
        string id, GameMode mode, CancellationToken ct = default)
    {
        const string gql = ItemFields + """

            query Crafts($ids: [ID]!, $mode: GameMode!) {
              items(ids: $ids, gameMode: $mode) {
                craftsFor {
                  station { name } level duration
                  requiredItems { count item { ...F } }
                  rewardItems { count item { ...F } }
                }
              }
            }
            """;

        ItemsData data = await PostAsync<ItemsData>(gql, new()
        {
            ["ids"] = new[] { id },
            ["mode"] = mode.ToApiValue(),
        }, ct);

        return (data.Items?.FirstOrDefault()?.CraftsFor ?? [])
            .Select(c => c.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<HistoricalPricePoint>> GetPriceHistoryAsync(
        string id, int days, GameMode mode, CancellationToken ct = default)
    {
        const string gql = """
            query Hist($id: ID!, $days: Int!, $mode: GameMode!) {
              historicalItemPrices(id: $id, days: $days, gameMode: $mode) {
                price priceMin offerCount timestamp
              }
            }
            """;

        HistoryData data = await PostAsync<HistoryData>(gql, new()
        {
            ["id"] = id,
            ["days"] = days,
            ["mode"] = mode.ToApiValue(),
        }, ct);

        return (data.HistoricalItemPrices ?? [])
            .Select(p => p.ToModel())
            .OrderBy(p => p.Timestamp)
            .ToList();
    }

    private IReadOnlyList<Item> MapAndCache(List<ItemDto>? dtos, GameMode mode)
    {
        if (dtos is null) return [];
        DateTimeOffset now = _time.GetUtcNow();
        var items = new List<Item>(dtos.Count);
        foreach (ItemDto dto in dtos)
        {
            Item item = dto.ToModel();
            _cache.Set(mode, item, now);
            items.Add(item);
        }
        return items;
    }

    /// <summary>
    /// POSTs a GraphQL query + variables, retrying 429 / transient 5xx with exponential backoff,
    /// and returns the deserialized <c>data</c>. Throws <see cref="TarkovApiException"/> on GraphQL
    /// errors or after retries are exhausted.
    /// </summary>
    private async Task<TData> PostAsync<TData>(
        string query, Dictionary<string, object?> variables, CancellationToken ct)
    {
        var body = new { query, variables };

        for (int attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync(Endpoint, body, JsonOptions, ct);
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await BackoffAsync(attempt, retryAfter: null, ct);
                continue;
            }

            if (IsTransient(response.StatusCode) && attempt < MaxRetries)
            {
                TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
                response.Dispose();
                await BackoffAsync(attempt, retryAfter, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();

            GqlResponse<TData>? parsed =
                await response.Content.ReadFromJsonAsync<GqlResponse<TData>>(JsonOptions, ct);

            if (parsed?.Errors is { Count: > 0 } errors)
                throw new TarkovApiException(string.Join("; ", errors.Select(e => e.Message)));

            return parsed is { Data: not null }
                ? parsed.Data
                : throw new TarkovApiException("The API returned no data.");
        }
    }

    private static bool IsTransient(HttpStatusCode code) =>
        code == HttpStatusCode.TooManyRequests || (int)code >= 500;

    private async Task BackoffAsync(int attempt, TimeSpan? retryAfter, CancellationToken ct)
    {
        // Honour a server-provided Retry-After, else exponential: 0.5s, 1s, 2s, 4s.
        TimeSpan delay = retryAfter ?? TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt));
        await Task.Delay(delay, _time, ct);
    }

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}

/// <summary>Raised when the tarkov.dev API returns GraphQL errors or an unusable response.</summary>
public sealed class TarkovApiException(string message) : Exception(message);
