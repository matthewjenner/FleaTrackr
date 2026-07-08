namespace FleaTrackr.Core.Models;

/// <summary>
/// A flattened, point-in-time view of the prices FleaTrackr tracks for one item, captured each
/// time the watchlist refreshes it. Kept deliberately small and numeric so alert evaluation and
/// trend display do not need the full <see cref="Item"/>. Build one with <see cref="FromItem"/>.
/// </summary>
public sealed record PriceSnapshot
{
    public required string ItemId { get; init; }
    public required DateTimeOffset FetchedAt { get; init; }

    /// <summary>Flea sell price in roubles (what you get selling on flea), or null if unlisted.</summary>
    public int? FleaSell { get; init; }

    /// <summary>Flea buy price in roubles (what it costs to buy on flea), or null if unlisted.</summary>
    public int? FleaBuy { get; init; }

    /// <summary>24h average flea price in roubles, or null.</summary>
    public int? Avg24hPrice { get; init; }

    /// <summary>Percent change over the last 48h, or null.</summary>
    public double? ChangeLast48hPercent { get; init; }

    /// <summary>Best trader sell price in roubles, or null if no trader buys it.</summary>
    public int? BestTraderSell { get; init; }

    /// <summary>Name of the best-paying trader, or null.</summary>
    public string? BestTraderName { get; init; }

    /// <summary>
    /// The single price FleaTrackr treats as "the item's value" for alerts and trend arrows:
    /// the flea sell price when listed, otherwise the best trader sell, otherwise null.
    /// </summary>
    public int? ReferencePrice => FleaSell ?? BestTraderSell;

    /// <summary>Captures the current prices of <paramref name="item"/> at <paramref name="fetchedAt"/>.</summary>
    public static PriceSnapshot FromItem(Item item, DateTimeOffset fetchedAt) => new()
    {
        ItemId = item.Id,
        FetchedAt = fetchedAt,
        FleaSell = item.FleaSell?.PriceRub ?? item.LastLowPrice,
        FleaBuy = item.FleaBuy?.PriceRub,
        Avg24hPrice = item.Avg24hPrice,
        ChangeLast48hPercent = item.ChangeLast48hPercent,
        BestTraderSell = item.BestTraderSell?.PriceRub,
        BestTraderName = item.BestTraderSell?.Vendor,
    };
}
