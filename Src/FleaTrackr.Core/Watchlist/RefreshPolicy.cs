namespace FleaTrackr.Core.Watchlist;

/// <summary>The refresh timing state of one watched item, fed to <see cref="RefreshPolicy"/>.</summary>
public readonly record struct RefreshState(string ItemId, int RefreshSeconds, DateTimeOffset? LastRefreshed);

/// <summary>
/// Pure policy deciding which watched items are due for a refresh at a given moment. The scheduler
/// ticks frequently and asks this which ids to fetch, then coalesces them into one batched API call
/// - so items sharing a cadence refresh together and the app stays under the rate limit. Kept pure
/// (no timers, no clock of its own) so the timing rules are exhaustively unit-testable.
/// </summary>
public static class RefreshPolicy
{
    /// <summary>
    /// Returns the ids due at <paramref name="now"/>. An item that has never been refreshed is
    /// always due (initial fetch). A manual item (<see cref="WatchedItemConfig.ManualRefresh"/>)
    /// is due only for that initial fetch, never again. Otherwise it is due once its interval has
    /// elapsed since the last refresh.
    /// </summary>
    public static IReadOnlyList<string> SelectDue(IEnumerable<RefreshState> items, DateTimeOffset now)
    {
        var due = new List<string>();
        foreach (RefreshState s in items)
        {
            if (s.LastRefreshed is null)
            {
                due.Add(s.ItemId); // Never fetched - always do the initial load.
                continue;
            }

            if (s.RefreshSeconds <= WatchedItemConfig.ManualRefresh)
                continue; // Manual-only and already fetched once.

            if (now - s.LastRefreshed.Value >= TimeSpan.FromSeconds(s.RefreshSeconds))
                due.Add(s.ItemId);
        }
        return due;
    }
}
