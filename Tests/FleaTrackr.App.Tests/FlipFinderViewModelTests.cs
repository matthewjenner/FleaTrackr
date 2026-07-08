using FleaTrackr.App.Services;
using FleaTrackr.App.ViewModels;
using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.App.Tests;

/// <summary>Verifies the Flip Finder scan ranks results, honours the minimum, and reports progress.</summary>
public class FlipFinderViewModelTests
{
    private static Item Flip(string id, int traderBuy, int fleaSell) => new()
    {
        Id = id, Name = id,
        BuyFor = [new VendorPrice("Therapist", traderBuy, "RUB", traderBuy)],
        SellFor = [new VendorPrice(VendorPrice.FleaMarketVendorName, fleaSell, "RUB", fleaSell)],
    };

    [Fact]
    public async Task Scan_ranks_flips_and_applies_the_minimum_profit()
    {
        var api = new FakeApi()
            .SetItem(Flip("big", 10_000, 80_000))    // profit 70k
            .SetItem(Flip("small", 2_000, 10_000))   // profit 8k
            .SetItem(Flip("tiny", 1_000, 3_000))     // profit 2k -> below the 5k default
            .SetItem(new Item { Id = "flat", Name = "flat" }); // no prices -> no flip
        var vm = new FlipFinderViewModel(api, () => GameMode.Pvp);

        await vm.ScanCommand.ExecuteAsync(null);

        vm.IsScanning.Should().BeFalse();
        vm.Results.Should().HaveCount(2);
        vm.Results[0].Name.Should().Be("big", "highest profit first");
        vm.Results[1].Name.Should().Be("small");
        vm.StatusMessage.Should().Contain("Scanned 4 items").And.Contain("Found 2 flips");
    }

    [Fact]
    public async Task Scan_excludes_items_above_the_configured_player_flea_level()
    {
        Item reachable = Flip("reach", 1_000, 50_000) with { MinLevelForFlea = 15 };
        Item locked = Flip("locked", 1_000, 90_000) with { MinLevelForFlea = 40 };
        var api = new FakeApi().SetItem(reachable).SetItem(locked);
        var vm = new FlipFinderViewModel(api, () => GameMode.Pvp,
            () => new AppSettings { PlayerFleaLevel = 20, DefaultMinProfit = 1_000 });

        await vm.ScanCommand.ExecuteAsync(null);

        vm.Results.Should().ContainSingle().Which.Name.Should().Be("reach");
    }

    [Fact]
    public async Task Direction_and_fee_flag_are_set_for_trader_to_flea()
    {
        var api = new FakeApi().SetItem(Flip("gpu", 100_000, 150_000));
        var vm = new FlipFinderViewModel(api, () => GameMode.Pvp);

        await vm.ScanCommand.ExecuteAsync(null);

        FlipRowViewModel row = vm.Results.Should().ContainSingle().Subject;
        row.DirectionText.Should().Be("Trader -> Flea");
        row.IsFleaSale.Should().BeTrue("flea sale price is gross of the market fee");
        row.ProfitText.Should().Be("50,000");
    }
}
