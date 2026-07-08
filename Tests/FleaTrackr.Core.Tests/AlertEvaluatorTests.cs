using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class AlertEvaluatorTests
{
    private static PriceSnapshot Snap(int? price, double? change = null) => new()
    {
        ItemId = "1",
        FetchedAt = DateTimeOffset.UnixEpoch,
        FleaSell = price,
        ChangeLast48hPercent = change,
    };

    private static WatchedItemConfig Config(params AlertRule[] rules) =>
        new() { ItemId = "1", Name = "GPU", Rules = rules };

    [Fact]
    public void PriceDropsBelow_fires_on_the_crossing_not_every_tick()
    {
        WatchedItemConfig cfg = Config(new AlertRule(AlertKind.PriceDropsBelow, 100_000));

        // Above threshold -> nothing.
        AlertEvaluator.Evaluate(cfg, previous: null, Snap(120_000)).Should().BeEmpty();

        // Crosses below -> fires once.
        AlertEvaluator.Evaluate(cfg, Snap(120_000), Snap(90_000)).Should().ContainSingle()
            .Which.Rule.Kind.Should().Be(AlertKind.PriceDropsBelow);

        // Still below on the next tick -> does NOT fire again (edge-triggered).
        AlertEvaluator.Evaluate(cfg, Snap(90_000), Snap(85_000)).Should().BeEmpty();
    }

    [Fact]
    public void First_observation_already_met_fires_once()
    {
        WatchedItemConfig cfg = Config(new AlertRule(AlertKind.PriceRisesAbove, 100_000));

        AlertEvaluator.Evaluate(cfg, previous: null, Snap(150_000)).Should().ContainSingle();
    }

    [Fact]
    public void Percent_drop_rule_uses_the_48h_change()
    {
        WatchedItemConfig cfg = Config(new AlertRule(AlertKind.PercentDropExceeds, 10));

        // -12% exceeds a 10% drop threshold, crossing from a prior -5%.
        AlertEvaluator.Evaluate(cfg, Snap(100, -5), Snap(100, -12)).Should().ContainSingle();
        // -8% does not.
        AlertEvaluator.Evaluate(cfg, null, Snap(100, -8)).Should().BeEmpty();
    }

    [Fact]
    public void Disabled_rules_never_fire()
    {
        WatchedItemConfig cfg = Config(new AlertRule(AlertKind.PriceDropsBelow, 100_000, Enabled: false));

        AlertEvaluator.Evaluate(cfg, Snap(120_000), Snap(50_000)).Should().BeEmpty();
    }

    [Fact]
    public void Null_price_never_trips_a_price_rule()
    {
        WatchedItemConfig cfg = Config(new AlertRule(AlertKind.PriceDropsBelow, 100_000));

        AlertEvaluator.Evaluate(cfg, Snap(120_000), Snap(null)).Should().BeEmpty();
    }
}
