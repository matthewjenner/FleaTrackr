using FleaTrackr.Core.Watchlist;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class RefreshPolicyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddHours(1);

    [Fact]
    public void Never_refreshed_items_are_always_due()
    {
        var states = new[] { new RefreshState("a", 60, null), new RefreshState("b", 0, null) };

        RefreshPolicy.SelectDue(states, Now).Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public void Item_is_due_once_its_interval_has_elapsed()
    {
        var states = new[]
        {
            new RefreshState("fresh", 60, Now.AddSeconds(-30)), // 30s ago, 60s interval -> not due
            new RefreshState("stale", 60, Now.AddSeconds(-90)), // 90s ago -> due
        };

        RefreshPolicy.SelectDue(states, Now).Should().Equal("stale");
    }

    [Fact]
    public void Manual_items_are_not_due_after_their_initial_fetch()
    {
        var states = new[] { new RefreshState("m", WatchedItemConfig.ManualRefresh, Now.AddDays(-1)) };

        RefreshPolicy.SelectDue(states, Now).Should().BeEmpty();
    }

    [Fact]
    public void Boundary_exactly_at_the_interval_is_due()
    {
        var states = new[] { new RefreshState("edge", 60, Now.AddSeconds(-60)) };

        RefreshPolicy.SelectDue(states, Now).Should().Equal("edge");
    }
}
