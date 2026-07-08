namespace FleaTrackr.Core.Models;

/// <summary>
/// An Escape from Tarkov item with its current market prices, mapped from the tarkov.dev
/// <c>Item</c> type. Flea-market prices are exposed both as the summary fields the API precomputes
/// (<see cref="Avg24hPrice"/> etc.) and as the raw <see cref="BuyFor"/>/<see cref="SellFor"/>
/// offers; the computed helpers below pull out the specific numbers the UI cares about.
///
/// Any price can be null - the item may not currently be listed on the flea market, or a fresh
/// wipe may have reset the economy. Callers must handle nulls.
/// </summary>
public sealed record Item
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string ShortName { get; init; } = "";

    /// <summary>Vendor/handbook base price in roubles (always present).</summary>
    public int BasePrice { get; init; }

    /// <summary>24h average flea price in roubles, or null if not currently traded.</summary>
    public int? Avg24hPrice { get; init; }

    /// <summary>The most recent flea "low" listing price seen, in roubles.</summary>
    public int? LastLowPrice { get; init; }

    /// <summary>Lowest flea price over the last 24h, in roubles.</summary>
    public int? Low24hPrice { get; init; }

    /// <summary>Highest flea price over the last 24h, in roubles.</summary>
    public int? High24hPrice { get; init; }

    /// <summary>Percent change in flea price over the last 48h (e.g. -3.2), or null.</summary>
    public double? ChangeLast48hPercent { get; init; }

    /// <summary>Number of active flea offers at last sample, or null.</summary>
    public int? LastOfferCount { get; init; }

    /// <summary>Minimum player level required to trade this on the flea market, if any.</summary>
    public int? MinLevelForFlea { get; init; }

    /// <summary>When the flea data was last updated server-side.</summary>
    public DateTimeOffset? Updated { get; init; }

    public string? IconLink { get; init; }
    public string? WikiLink { get; init; }

    /// <summary>Offers to BUY this item (flea + traders that sell it). May be empty.</summary>
    public IReadOnlyList<VendorPrice> BuyFor { get; init; } = [];

    /// <summary>Offers to SELL this item (flea + traders/Fence that buy it). May be empty.</summary>
    public IReadOnlyList<VendorPrice> SellFor { get; init; } = [];

    // ---- Computed convenience accessors (no allocation of new state) ----

    /// <summary>The flea-market buy offer (what it costs to buy on flea), or null if not listed.</summary>
    public VendorPrice? FleaBuy => BuyFor.FirstOrDefault(v => v.IsFlea);

    /// <summary>The flea-market sell offer (what you get selling on flea), or null if not listed.</summary>
    public VendorPrice? FleaSell => SellFor.FirstOrDefault(v => v.IsFlea);

    /// <summary>The best (highest rouble) trader you can sell this to, excluding the flea market.</summary>
    public VendorPrice? BestTraderSell =>
        SellFor.Where(v => !v.IsFlea).OrderByDescending(v => v.PriceRub).FirstOrDefault();

    /// <summary>The cheapest (lowest rouble) trader you can buy this from, excluding the flea market.</summary>
    public VendorPrice? BestTraderBuy =>
        BuyFor.Where(v => !v.IsFlea).OrderBy(v => v.PriceRub).FirstOrDefault();
}
