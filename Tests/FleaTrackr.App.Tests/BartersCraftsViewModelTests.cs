using FleaTrackr.App.ViewModels;
using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>Verifies the Barters &amp; Crafts tab loads trades and ranks them by profit.</summary>
public class BartersCraftsViewModelTests
{
    private static Item Priced(string id, int? buy = null, int? sell = null)
    {
        var buyFor = buy is { } b ? new List<VendorPrice> { new(VendorPrice.FleaMarketVendorName, b, "RUB", b) } : [];
        var sellFor = sell is { } s ? new List<VendorPrice> { new(VendorPrice.FleaMarketVendorName, s, "RUB", s) } : new List<VendorPrice>();
        return new Item { Id = id, Name = id, BuyFor = buyFor, SellFor = sellFor };
    }

    [Fact]
    public async Task Loads_barters_and_crafts_ranked_by_profit()
    {
        Item ledx = Priced("ledx", sell: 500_000);
        var lowProfit = new Barter
        {
            TraderName = "Therapist", TraderLevel = 1,
            RequiredItems = [new ItemStack(Priced("a", buy: 400_000), 1)],
            RewardItems = [new ItemStack(ledx, 1)],
        };
        var highProfit = new Barter
        {
            TraderName = "Therapist", TraderLevel = 3,
            RequiredItems = [new ItemStack(Priced("b", buy: 50_000), 1)],
            RewardItems = [new ItemStack(ledx, 1)],
        };
        var craft = new Craft
        {
            StationName = "Medstation", StationLevel = 1, Duration = TimeSpan.FromHours(2),
            RequiredItems = [new ItemStack(Priced("c", buy: 100_000), 1)],
            RewardItems = [new ItemStack(ledx, 1)],
        };

        var api = new FakeApi()
            .SetItem(ledx)
            .SetBarters("ledx", lowProfit, highProfit)
            .SetCrafts("ledx", craft);
        var vm = new BartersCraftsViewModel(api, () => GameMode.Pvp);

        await vm.LoadTradesAsync(new ItemRowViewModel(ledx));

        vm.HasBarters.Should().BeTrue();
        vm.HasCrafts.Should().BeTrue();
        vm.NoTrades.Should().BeFalse();

        // Highest-profit barter first (450k profit before 100k).
        vm.Barters.Should().HaveCount(2);
        vm.Barters[0].ProfitText.Should().Be("450,000");
        vm.Barters[1].ProfitText.Should().Be("100,000");

        // Craft carries duration + profit/hour (400k profit over 2h = 200,000/h).
        vm.Crafts.Should().ContainSingle();
        vm.Crafts[0].IsCraft.Should().BeTrue();
        vm.Crafts[0].ProfitPerHourText.Should().Be("200,000/h");
    }

    [Fact]
    public async Task Item_with_no_trades_sets_the_empty_state()
    {
        Item plain = Priced("plain", sell: 1000);
        var vm = new BartersCraftsViewModel(new FakeApi().SetItem(plain), () => GameMode.Pvp);

        await vm.LoadTradesAsync(new ItemRowViewModel(plain));

        vm.NoTrades.Should().BeTrue();
        vm.HasBarters.Should().BeFalse();
        vm.HasCrafts.Should().BeFalse();
    }
}
