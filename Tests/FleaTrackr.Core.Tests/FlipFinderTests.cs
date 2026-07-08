using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class FlipFinderTests
{
    private static VendorPrice Flea(int rub) => new(VendorPrice.FleaMarketVendorName, rub, "RUB", rub);
    private static VendorPrice Trader(string name, int rub) => new(name, rub, "RUB", rub);

    [Fact]
    public void Finds_a_trader_to_flea_flip_when_flea_sell_beats_trader_buy()
    {
        var item = new Item
        {
            Id = "1", Name = "Salewa",
            BuyFor = [Trader("Therapist", 12_000)],  // buy from trader at 12k
            SellFor = [Flea(20_000), Trader("Fence", 8_000)], // sell on flea at 20k
        };

        FlipOpportunity op = FlipFinder.Find(item).Single();
        op.Direction.Should().Be(FlipDirection.TraderToFlea);
        op.BuyFrom.Should().Be("Therapist");
        op.SellTo.Should().Be(VendorPrice.FleaMarketVendorName);
        op.Profit.Should().Be(8_000);
        op.RoiPercent.Should().BeApproximately(66.67, 0.01);
    }

    [Fact]
    public void Finds_a_flea_to_trader_flip_when_a_trader_pays_more_than_flea_costs()
    {
        var item = new Item
        {
            Id = "2", Name = "Gas analyzer",
            BuyFor = [Flea(15_000)],                 // buy on flea at 15k
            SellFor = [Trader("Mechanic", 22_000), Flea(14_000)], // trader pays 22k
        };

        FlipOpportunity op = FlipFinder.Find(item).Single();
        op.Direction.Should().Be(FlipDirection.FleaToTrader);
        op.BuyFrom.Should().Be(VendorPrice.FleaMarketVendorName);
        op.SellTo.Should().Be("Mechanic");
        op.Profit.Should().Be(7_000);
    }

    [Fact]
    public void No_flip_when_neither_direction_is_profitable()
    {
        var item = new Item
        {
            Id = "3", Name = "Bolts",
            // Cheapest trader buy (480) is above the flea sell (450) -> no trader->flea flip.
            // Best trader sell (300) is below the flea buy (500) -> no flea->trader flip.
            BuyFor = [Flea(500), Trader("Jaeger", 480)],
            SellFor = [Flea(450), Trader("Fence", 300)],
        };

        FlipFinder.Find(item).Should().BeEmpty();
    }

    [Fact]
    public void Trader_to_flea_profit_is_net_of_the_flea_fee()
    {
        var item = new Item
        {
            Id = "gpu", Name = "GPU", BasePrice = 40_000,
            BuyFor = [Trader("Mechanic", 100_000)],
            SellFor = [Flea(150_000)],
        };

        FlipOpportunity op = FlipFinder.Find(item).Single();
        int expectedFee = FleaFee.Calculate(40_000, 150_000);
        op.Fee.Should().Be(expectedFee).And.BeGreaterThan(0);
        op.Profit.Should().Be(150_000 - expectedFee - 100_000);
        op.GrossProfit.Should().Be(50_000);
    }

    [Fact]
    public void FindAll_skips_items_above_the_players_flea_level()
    {
        var reachable = new Item { Id = "a", Name = "a", MinLevelForFlea = 15, BuyFor = [Trader("T", 1_000)], SellFor = [Flea(50_000)] };
        var locked = new Item { Id = "b", Name = "b", MinLevelForFlea = 40, BuyFor = [Trader("T", 1_000)], SellFor = [Flea(90_000)] };

        IReadOnlyList<FlipOpportunity> result = FlipFinder.FindAll([reachable, locked], minProfit: 1_000, playerFleaLevel: 20);

        result.Should().ContainSingle().Which.Item.Id.Should().Be("a", "the level-40 item is locked at player level 20");
    }

    [Fact]
    public void FindAll_ranks_by_profit_and_applies_the_minimum()
    {
        var small = new Item { Id = "s", Name = "small", BuyFor = [Trader("T", 1_000)], SellFor = [Flea(3_000)] };
        var big = new Item { Id = "b", Name = "big", BuyFor = [Trader("T", 10_000)], SellFor = [Flea(80_000)] };
        var tiny = new Item { Id = "t", Name = "tiny", BuyFor = [Trader("T", 100)], SellFor = [Flea(150)] };

        IReadOnlyList<FlipOpportunity> result = FlipFinder.FindAll([small, big, tiny], minProfit: 1_000);

        result.Should().HaveCount(2, "the tiny 50-rouble flip is below the minimum");
        result[0].Item.Id.Should().Be("b", "highest profit first");
        result[1].Item.Id.Should().Be("s");
    }
}
