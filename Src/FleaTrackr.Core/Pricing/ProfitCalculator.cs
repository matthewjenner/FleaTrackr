using FleaTrackr.Core.Models;

namespace FleaTrackr.Core.Pricing;

/// <summary>
/// The cost/value/profit of a trade (barter or craft), in roubles. Any leg can be null when a
/// required or reward item has no known price - in that case <see cref="Profit"/> is null too, so
/// the UI shows "-" rather than a misleading number.
/// </summary>
public sealed record TradeCost(int? InputCost, int? OutputValue)
{
    /// <summary>Output value minus input cost, or null if either side is unknown.</summary>
    public int? Profit => InputCost is { } c && OutputValue is { } v ? v - c : null;

    /// <summary>Return on the input cost as a percent, or null if unknown / zero cost.</summary>
    public double? RoiPercent =>
        InputCost is > 0 and { } c && Profit is { } p ? 100.0 * p / c : null;
}

/// <summary>
/// Computes barter/craft profit against current market prices. Inputs are valued at the cheapest
/// way to acquire each required item (lowest <c>buyFor</c> across flea and traders); outputs at the
/// price you would realise selling the reward (flea sell, falling back to the best trader). Pure
/// and prices-in, so it is fully unit-testable and independent of how the items were fetched.
/// </summary>
public static class ProfitCalculator
{
    /// <summary>Cheapest rouble cost to acquire one of <paramref name="item"/>, or null if unknown.</summary>
    public static int? AcquisitionCost(Item item) =>
        item.BuyFor.Count == 0 ? null : item.BuyFor.Min(v => v.PriceRub);

    /// <summary>Rouble value realised selling one of <paramref name="item"/> (flea, then best trader).</summary>
    public static int? SaleValue(Item item) =>
        item.FleaSell?.PriceRub ?? item.BestTraderSell?.PriceRub;

    /// <summary>Costs the inputs and values the outputs of a barter.</summary>
    public static TradeCost ForBarter(Barter barter) =>
        Compute(barter.RequiredItems, barter.RewardItems);

    /// <summary>Costs the inputs and values the outputs of a craft.</summary>
    public static TradeCost ForCraft(Craft craft) =>
        Compute(craft.RequiredItems, craft.RewardItems);

    private static TradeCost Compute(IReadOnlyList<ItemStack> required, IReadOnlyList<ItemStack> reward) =>
        new(SumOrNull(required, AcquisitionCost), SumOrNull(reward, SaleValue));

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
