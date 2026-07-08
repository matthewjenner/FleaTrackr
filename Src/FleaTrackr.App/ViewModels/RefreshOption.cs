using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.ViewModels;

/// <summary>A selectable auto-refresh cadence for a watchlist row. Seconds of 0 means manual only.</summary>
public sealed record RefreshOption(string Label, int Seconds)
{
    /// <summary>The cadences offered in the per-item dropdown, fastest first.</summary>
    public static readonly IReadOnlyList<RefreshOption> All =
    [
        new("30 seconds", 30),
        new("1 minute", 60),
        new("5 minutes", 300),
        new("15 minutes", 900),
        new("Manual only", WatchedItemConfig.ManualRefresh),
    ];

    /// <summary>The option matching <paramref name="seconds"/>, defaulting to 1 minute.</summary>
    public static RefreshOption ForSeconds(int seconds) =>
        All.FirstOrDefault(o => o.Seconds == seconds) ?? All[1];
}
