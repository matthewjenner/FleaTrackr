using System.Globalization;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// One barter or craft row with its computed profit. Unifies both trade types behind the same
/// display shape (source, requirements, reward, cost/value/profit/ROI) so the view can list them
/// identically; craft-only fields (duration, profit/hour) are null for barters.
/// </summary>
public sealed class TradeRowViewModel
{
    private TradeRowViewModel(
        string source, IReadOnlyList<ItemStack> required, string rewardText, TradeCost cost,
        bool isCraft, TimeSpan? duration)
    {
        Source = source;
        RequirementsText = DescribeStacks(required);
        RequirementsBreakdown = DescribeStacksWithCost(required);
        RewardText = rewardText;
        IsCraft = isCraft;

        InputCostText = PriceFormat.Rub(cost.InputCost);
        OutputValueText = PriceFormat.Rub(cost.OutputValue);
        ProfitText = PriceFormat.Rub(cost.Profit);
        ProfitValue = cost.Profit ?? 0;
        RoiText = cost.RoiPercent is { } roi
            ? roi.ToString("0.#", CultureInfo.InvariantCulture) + "%"
            : "-";

        if (isCraft && duration is { } d)
        {
            DurationText = FormatDuration(d);
            if (cost.Profit is { } profit && d.TotalHours > 0)
                ProfitPerHourText = PriceFormat.Rub((int)Math.Round(profit / d.TotalHours)) + "/h";
        }
    }

    public string Source { get; }
    public string RequirementsText { get; }

    /// <summary>Requirements with each input's estimated acquisition cost, for the "Give" tooltip.</summary>
    public string RequirementsBreakdown { get; }
    public string RewardText { get; }
    public string InputCostText { get; }
    public string OutputValueText { get; }
    public string ProfitText { get; }

    /// <summary>Raw profit for colour binding (green/red); 0 when unknown.</summary>
    public double ProfitValue { get; }
    public string RoiText { get; }

    public bool IsCraft { get; }
    public string? DurationText { get; }
    public string? ProfitPerHourText { get; }

    /// <summary>Profit for sorting; unknown profits sort last.</summary>
    public int? SortKey { get; private init; }

    public static TradeRowViewModel FromBarter(Barter barter, double feeReductionPercent = 0)
    {
        TradeCost cost = ProfitCalculator.ForBarter(barter, feeReductionPercent);
        return new TradeRowViewModel(
            $"{barter.TraderName}  (Lvl {barter.TraderLevel})",
            barter.RequiredItems,
            DescribeStacks(barter.RewardItems),
            cost, isCraft: false, duration: null)
        { SortKey = cost.Profit };
    }

    public static TradeRowViewModel FromCraft(Craft craft, double feeReductionPercent = 0)
    {
        TradeCost cost = ProfitCalculator.ForCraft(craft, feeReductionPercent);
        return new TradeRowViewModel(
            $"{craft.StationName}  (Lvl {craft.StationLevel})",
            craft.RequiredItems,
            DescribeStacks(craft.RewardItems),
            cost, isCraft: true, duration: craft.Duration)
        { SortKey = cost.Profit };
    }

    private static string DescribeStacks(IReadOnlyList<ItemStack> stacks) =>
        stacks.Count == 0 ? "-" : string.Join(", ", stacks.Select(s =>
        {
            string count = s.Count.ToString("0.#", CultureInfo.InvariantCulture);
            string name = string.IsNullOrEmpty(s.Item.ShortName) ? s.Item.Name : s.Item.ShortName;
            return $"{count}x {name}";
        }));

    // One line per input with its estimated acquisition cost, for the "Give" tooltip.
    private static string DescribeStacksWithCost(IReadOnlyList<ItemStack> stacks)
    {
        if (stacks.Count == 0) return "-";
        return string.Join("\n", stacks.Select(s =>
        {
            string count = s.Count.ToString("0.#", CultureInfo.InvariantCulture);
            int? unit = ProfitCalculator.AcquisitionCost(s.Item);
            string cost = unit is { } u ? PriceFormat.Rub((int)Math.Round(u * s.Count)) : "?";
            return $"{count}x {s.Item.Name}  ({cost})";
        }));
    }

    private static string FormatDuration(TimeSpan d) =>
        d.TotalHours >= 1
            ? $"{(int)d.TotalHours}h {d.Minutes}m"
            : $"{d.Minutes}m {d.Seconds}s";
}
