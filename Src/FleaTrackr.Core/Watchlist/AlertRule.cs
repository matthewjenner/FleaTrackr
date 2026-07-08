namespace FleaTrackr.Core.Watchlist;

/// <summary>What condition an <see cref="AlertRule"/> watches for.</summary>
public enum AlertKind
{
    /// <summary>The item's reference price falls below the threshold (roubles).</summary>
    PriceDropsBelow,

    /// <summary>The item's reference price rises above the threshold (roubles).</summary>
    PriceRisesAbove,

    /// <summary>The 48h change drops by at least the threshold percent (e.g. threshold 10 = -10%).</summary>
    PercentDropExceeds,

    /// <summary>The 48h change rises by at least the threshold percent (e.g. threshold 10 = +10%).</summary>
    PercentRiseExceeds,
}

/// <summary>
/// One alert condition on a watched item. Price kinds compare against the snapshot's reference
/// price (flea sell, or best trader when unlisted); percent kinds compare the 48h change. Rules
/// are edge-triggered by <see cref="AlertEvaluator"/> - they fire when the condition becomes true,
/// not on every refresh while it stays true.
/// </summary>
public sealed record AlertRule(AlertKind Kind, double Threshold, bool Enabled = true)
{
    /// <summary>True for the two rouble-price kinds (vs the two percent kinds).</summary>
    public bool IsPriceKind => Kind is AlertKind.PriceDropsBelow or AlertKind.PriceRisesAbove;
}
