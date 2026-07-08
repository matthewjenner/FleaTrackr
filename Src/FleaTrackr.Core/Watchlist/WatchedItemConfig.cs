namespace FleaTrackr.Core.Watchlist;

/// <summary>
/// The persisted configuration for one watchlist entry: which item, how often to refresh it, and
/// its alert rules. Serialized to <c>watchlist.json</c>. Identity/display fields (name, icon) are
/// stored so the watchlist renders instantly on startup before the first live refresh completes.
/// </summary>
public sealed record WatchedItemConfig
{
    public required string ItemId { get; init; }
    public required string Name { get; init; }
    public string ShortName { get; init; } = "";
    public string? IconLink { get; init; }

    /// <summary>
    /// Seconds between automatic refreshes. A value of <see cref="ManualRefresh"/> (or less) means
    /// manual-only: the item still fetches once when added, but never auto-refreshes after that.
    /// </summary>
    public int RefreshSeconds { get; init; } = 60;

    public IReadOnlyList<AlertRule> Rules { get; init; } = [];

    /// <summary>Sentinel <see cref="RefreshSeconds"/> value meaning "manual refresh only".</summary>
    public const int ManualRefresh = 0;
}
