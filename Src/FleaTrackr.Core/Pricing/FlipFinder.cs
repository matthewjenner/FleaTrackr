using FleaTrackr.Core.Models;

namespace FleaTrackr.Core.Pricing;

/// <summary>Which way a flip runs.</summary>
public enum FlipDirection
{
    /// <summary>Buy from a trader, resell on the flea market for more.</summary>
    TraderToFlea,

    /// <summary>Buy on the flea market, sell to a trader for more.</summary>
    FleaToTrader,
}

/// <summary>
/// A single arbitrage opportunity on one item: buy at <see cref="BuyPrice"/> from
/// <see cref="BuyFrom"/>, sell at <see cref="SellPrice"/> to <see cref="SellTo"/>. Rouble figures.
/// Note: for <see cref="FlipDirection.TraderToFlea"/> the sell price is the gross flea price, before
/// the flea market sales fee - the UI flags this so the number is not read as net.
/// </summary>
public sealed record FlipOpportunity(
    Item Item, FlipDirection Direction, string BuyFrom, int BuyPrice, string SellTo, int SellPrice)
{
    public int Profit => SellPrice - BuyPrice;

    public double RoiPercent => BuyPrice > 0 ? 100.0 * Profit / BuyPrice : 0;
}

/// <summary>
/// Finds trader/flea arbitrage. Pure: it inspects the prices already on each <see cref="Item"/>
/// (cheapest trader buy vs flea sell, and flea buy vs best trader sell) and yields the profitable
/// directions. The Flip Finder feeds it a bounded, paged slice of the market and ranks the results.
/// </summary>
public static class FlipFinder
{
    /// <summary>Yields the profitable flip directions for one item (0, 1, or 2).</summary>
    public static IEnumerable<FlipOpportunity> Find(Item item)
    {
        // Buy from the cheapest trader, resell on flea (gross of fee).
        if (item.BestTraderBuy is { } traderBuy && item.FleaSell is { } fleaSell
            && fleaSell.PriceRub > traderBuy.PriceRub)
        {
            yield return new FlipOpportunity(item, FlipDirection.TraderToFlea,
                traderBuy.Vendor, traderBuy.PriceRub, VendorPrice.FleaMarketVendorName, fleaSell.PriceRub);
        }

        // Buy on flea, sell to the best-paying trader.
        if (item.FleaBuy is { } fleaBuy && item.BestTraderSell is { } traderSell
            && traderSell.PriceRub > fleaBuy.PriceRub)
        {
            yield return new FlipOpportunity(item, FlipDirection.FleaToTrader,
                VendorPrice.FleaMarketVendorName, fleaBuy.PriceRub, traderSell.Vendor, traderSell.PriceRub);
        }
    }

    /// <summary>
    /// Finds and ranks (highest profit first) every opportunity across <paramref name="items"/>
    /// whose profit is at least <paramref name="minProfit"/> roubles.
    /// </summary>
    public static IReadOnlyList<FlipOpportunity> FindAll(IEnumerable<Item> items, int minProfit = 1)
    {
        var list = new List<FlipOpportunity>();
        foreach (Item item in items)
            foreach (FlipOpportunity op in Find(item))
                if (op.Profit >= minProfit)
                    list.Add(op);

        list.Sort((a, b) => b.Profit.CompareTo(a.Profit));
        return list;
    }
}
