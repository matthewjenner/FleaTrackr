using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;

namespace FleaTrackr.Core.Watchlist;

/// <summary>An alert that fired: the rule, the snapshot that tripped it, and a display message.</summary>
public sealed record TriggeredAlert(
    string ItemId, string ItemName, AlertRule Rule, PriceSnapshot Snapshot, string Message);

/// <summary>
/// Pure, edge-triggered alert evaluation. Given the previous and current price snapshots for a
/// watched item, it fires a rule only on the transition from "not met" to "met" - so an item that
/// sits below a price threshold for hours alerts once, not on every refresh. On the very first
/// observation (no previous snapshot) a rule fires if it is already met. Kept pure so the exact
/// firing semantics are unit-testable with no scheduler or clock.
/// </summary>
public static class AlertEvaluator
{
    public static IReadOnlyList<TriggeredAlert> Evaluate(
        WatchedItemConfig config, PriceSnapshot? previous, PriceSnapshot current)
    {
        var fired = new List<TriggeredAlert>();
        foreach (AlertRule rule in config.Rules)
        {
            if (!rule.Enabled) continue;

            bool metNow = IsMet(rule, current);
            bool metBefore = previous is not null && IsMet(rule, previous);

            if (metNow && !metBefore)
                fired.Add(new TriggeredAlert(config.ItemId, config.Name, rule, current, Describe(config, rule, current)));
        }
        return fired;
    }

    private static bool IsMet(AlertRule rule, PriceSnapshot snap) => rule.Kind switch
    {
        AlertKind.PriceDropsBelow => snap.ReferencePrice is int p && p < rule.Threshold,
        AlertKind.PriceRisesAbove => snap.ReferencePrice is int p && p > rule.Threshold,
        AlertKind.PercentDropExceeds => snap.ChangeLast48hPercent is double c && c <= -rule.Threshold,
        AlertKind.PercentRiseExceeds => snap.ChangeLast48hPercent is double c && c >= rule.Threshold,
        _ => false,
    };

    private static string Describe(WatchedItemConfig config, AlertRule rule, PriceSnapshot snap)
    {
        string price = PriceFormat.Rub(snap.ReferencePrice);
        string change = PriceFormat.Percent(snap.ChangeLast48hPercent);
        return rule.Kind switch
        {
            AlertKind.PriceDropsBelow =>
                $"{config.Name}: {price} dropped below {PriceFormat.Rub((int)rule.Threshold)}",
            AlertKind.PriceRisesAbove =>
                $"{config.Name}: {price} rose above {PriceFormat.Rub((int)rule.Threshold)}",
            AlertKind.PercentDropExceeds =>
                $"{config.Name}: down {change} over 48h (>= {rule.Threshold:0.#}% drop)",
            AlertKind.PercentRiseExceeds =>
                $"{config.Name}: up {change} over 48h (>= {rule.Threshold:0.#}% rise)",
            _ => config.Name,
        };
    }
}
