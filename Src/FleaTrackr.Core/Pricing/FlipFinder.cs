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
/// For <see cref="FlipDirection.TraderToFlea"/>, <see cref="Fee"/> is the estimated flea sales fee
/// and <see cref="Profit"/> is net of it; selling to a trader carries no fee.
/// </summary>
public sealed record FlipOpportunity(
    Item Item, FlipDirection Direction, string BuyFrom, int BuyPrice, string SellTo, int SellPrice, int Fee = 0)
{
    /// <summary>Net profit: sale price minus the flea fee (if any) minus the buy price.</summary>
    public int Profit => SellPrice - Fee - BuyPrice;

    /// <summary>Profit before the flea fee.</summary>
    public int GrossProfit => SellPrice - BuyPrice;

    public double RoiPercent => BuyPrice > 0 ? 100.0 * Profit / BuyPrice : 0;
}

/// <summary>
/// Finds trader/flea arbitrage. Pure: it inspects the prices already on each <see cref="Item"/>
/// (cheapest trader buy vs flea sell, and flea buy vs best trader sell) and yields the profitable
/// directions. The Flip Finder feeds it a bounded, paged slice of the market and ranks the results.
/// </summary>
public static class FlipFinder
{
    /// <summary>
    /// Yields the profitable flip directions for one item (0, 1, or 2), with the trader-to-flea leg
    /// reported net of the estimated flea fee (discounted by <paramref name="feeReductionPercent"/>).
    /// </summary>
    public static IEnumerable<FlipOpportunity> Find(Item item, double feeReductionPercent = 0)
    {
        // Buy from the cheapest trader, resell on flea (net of the sales fee).
        if (item.BestTraderBuy is { } traderBuy && item.FleaSell is { } fleaSell)
        {
            int fee = FleaFee.Calculate(item.BasePrice, fleaSell.PriceRub, feeReductionPercent);
            if (fleaSell.PriceRub - fee > traderBuy.PriceRub)
                yield return new FlipOpportunity(item, FlipDirection.TraderToFlea,
                    traderBuy.Vendor, traderBuy.PriceRub, VendorPrice.FleaMarketVendorName, fleaSell.PriceRub, fee);
        }

        // Buy on flea, sell to the best-paying trader (no flea fee on a trader sale).
        if (item.FleaBuy is { } fleaBuy && item.BestTraderSell is { } traderSell
            && traderSell.PriceRub > fleaBuy.PriceRub)
        {
            yield return new FlipOpportunity(item, FlipDirection.FleaToTrader,
                VendorPrice.FleaMarketVendorName, fleaBuy.PriceRub, traderSell.Vendor, traderSell.PriceRub);
        }
    }

    /// <summary>
    /// Finds and ranks (highest net profit first) every opportunity across <paramref name="items"/>
    /// whose profit is at least <paramref name="minProfit"/> roubles. When
    /// <paramref name="playerFleaLevel"/> is given, items that require a higher flea-market level
    /// than the player has are skipped - a flip you cannot list yet is not actionable.
    /// </summary>
    public static IReadOnlyList<FlipOpportunity> FindAll(
        IEnumerable<Item> items, int minProfit = 1, double feeReductionPercent = 0, int? playerFleaLevel = null)
    {
        var list = new List<FlipOpportunity>();
        foreach (Item item in items)
        {
            if (playerFleaLevel is { } level && item.MinLevelForFlea is { } required && required > level)
                continue; // locked: can't trade this on the flea market yet

            foreach (FlipOpportunity op in Find(item, feeReductionPercent))
                if (op.Profit >= minProfit)
                    list.Add(op);
        }

        list.Sort((a, b) => b.Profit.CompareTo(a.Profit));
        return list;
    }
}
