using FleaTrackr.Core.Models;

namespace FleaTrackr.Core.Pricing;

/// <summary>
/// The cost/value/profit of a trade (barter or craft), in roubles. Output value is tracked both
/// gross and after the estimated flea-market fee; <see cref="Profit"/> is net of that fee (the
/// headline number), with <see cref="GrossProfit"/> available too. Any leg can be null when a
/// required or reward item has no known price - then <see cref="Profit"/> is null, so the UI shows
/// "-" rather than a misleading number.
/// </summary>
public sealed record TradeCost(int? InputCost, int? OutputValueGross, int? FleaFeeTotal)
{
    /// <summary>Output value net of the estimated flea fee (falls back to gross when no fee applies).</summary>
    public int? OutputValue =>
        OutputValueGross is { } g ? g - (FleaFeeTotal ?? 0) : null;

    /// <summary>Net profit: net output value minus input cost, or null if either side is unknown.</summary>
    public int? Profit => InputCost is { } c && OutputValue is { } v ? v - c : null;

    /// <summary>Profit before the flea fee, for comparison.</summary>
    public int? GrossProfit => InputCost is { } c && OutputValueGross is { } v ? v - c : null;

    /// <summary>Return on the input cost as a percent (net), or null if unknown / zero cost.</summary>
    public double? RoiPercent =>
        InputCost is > 0 and { } c && Profit is { } p ? 100.0 * p / c : null;
}

/// <summary>
/// Computes barter/craft profit against current market prices. Inputs are valued at the cheapest
/// way to acquire each required item (lowest <c>buyFor</c> across flea and traders); outputs at the
/// price you would realise selling the reward (flea sell net of the estimated fee, falling back to
/// the best trader). Pure and prices-in, so it is fully unit-testable and independent of how the
/// items were fetched.
/// </summary>
public static class ProfitCalculator
{
    /// <summary>Cheapest rouble cost to acquire one of <paramref name="item"/>, or null if unknown.</summary>
    public static int? AcquisitionCost(Item item) =>
        item.BuyFor.Count == 0 ? null : item.BuyFor.Min(v => v.PriceRub);

    /// <summary>Gross rouble value realised selling one of <paramref name="item"/> (flea, then best trader).</summary>
    public static int? SaleValue(Item item) =>
        item.FleaSell?.PriceRub ?? item.BestTraderSell?.PriceRub;

    /// <summary>Costs the inputs and values the outputs of a barter, net of the flea fee.</summary>
    public static TradeCost ForBarter(Barter barter, double feeReductionPercent = 0) =>
        Compute(barter.RequiredItems, barter.RewardItems, feeReductionPercent);

    /// <summary>Costs the inputs and values the outputs of a craft, net of the flea fee.</summary>
    public static TradeCost ForCraft(Craft craft, double feeReductionPercent = 0) =>
        Compute(craft.RequiredItems, craft.RewardItems, feeReductionPercent);

    private static TradeCost Compute(
        IReadOnlyList<ItemStack> required, IReadOnlyList<ItemStack> reward, double feeReductionPercent)
    {
        int? input = SumOrNull(required, AcquisitionCost);

        long gross = 0, fee = 0;
        foreach (ItemStack stack in reward)
        {
            Item item = stack.Item;
            if (item.FleaSell is { } fleaSell)
            {
                // Sold on flea: gross list value, and the estimated fee to list it.
                gross += (long)Math.Round(fleaSell.PriceRub * stack.Count);
                fee += (long)Math.Round(FleaFee.Calculate(item.BasePrice, fleaSell.PriceRub, feeReductionPercent) * stack.Count);
            }
            else if (item.BestTraderSell is { } traderSell)
            {
                // Sold to a trader: no flea fee.
                gross += (long)Math.Round(traderSell.PriceRub * stack.Count);
            }
            else
            {
                // A reward with no known sale price -> the whole output value is unknown.
                return new TradeCost(input, null, null);
            }
        }

        return new TradeCost(input, (int)gross, (int)fee);
    }

    // Sums price*count over the stacks; returns null if ANY stack's price is unknown, so a partial
    // total never masquerades as the real cost.
    private static int? SumOrNull(IReadOnlyList<ItemStack> stacks, Func<Item, int?> price)
    {
        long total = 0;
        foreach (ItemStack stack in stacks)
        {
            if (price(stack.Item) is not { } unit) return null;
            total += (long)Math.Round(unit * stack.Count);
        }
        return (int)total;
    }
}
