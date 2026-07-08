using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class ProfitCalculatorTests
{
    private static Item ItemWith(string id, int? fleaBuy = null, int? fleaSell = null, int? traderSell = null)
    {
        var buy = new List<VendorPrice>();
        var sell = new List<VendorPrice>();
        if (fleaBuy is { } b) buy.Add(new VendorPrice(VendorPrice.FleaMarketVendorName, b, "RUB", b));
        if (fleaSell is { } s) sell.Add(new VendorPrice(VendorPrice.FleaMarketVendorName, s, "RUB", s));
        if (traderSell is { } t) sell.Add(new VendorPrice("Therapist", t, "RUB", t));
        return new Item { Id = id, Name = id, BuyFor = buy, SellFor = sell };
    }

    [Fact]
    public void Barter_profit_is_reward_sale_value_minus_input_acquisition_cost()
    {
        var barter = new Barter
        {
            TraderName = "Therapist", TraderLevel = 2,
            RequiredItems =
            [
                new ItemStack(ItemWith("A", fleaBuy: 10_000), 2), // 20,000
                new ItemStack(ItemWith("B", fleaBuy: 5_000), 3),  // 15,000
            ],
            RewardItems = [new ItemStack(ItemWith("LEDX", fleaSell: 500_000), 1)],
        };

        TradeCost cost = ProfitCalculator.ForBarter(barter);

        cost.InputCost.Should().Be(35_000);
        cost.OutputValue.Should().Be(500_000);
        cost.Profit.Should().Be(465_000);
        cost.RoiPercent.Should().BeApproximately(1328.57, 0.01);
    }

    [Fact]
    public void Acquisition_uses_the_cheapest_buy_source()
    {
        var item = new Item
        {
            Id = "X", Name = "X",
            BuyFor =
            [
                new VendorPrice(VendorPrice.FleaMarketVendorName, 12_000, "RUB", 12_000),
                new VendorPrice("Skier", 9_000, "RUB", 9_000),
            ],
        };

        ProfitCalculator.AcquisitionCost(item).Should().Be(9_000);
    }

    [Fact]
    public void Sale_value_prefers_flea_then_falls_back_to_trader()
    {
        ProfitCalculator.SaleValue(ItemWith("A", fleaSell: 100, traderSell: 60)).Should().Be(100);
        ProfitCalculator.SaleValue(ItemWith("B", traderSell: 60)).Should().Be(60);
    }

    [Fact]
    public void Unknown_input_price_makes_profit_null_not_a_wrong_number()
    {
        var craft = new Craft
        {
            StationName = "Medstation", StationLevel = 1,
            RequiredItems =
            [
                new ItemStack(ItemWith("priced", fleaBuy: 1_000), 1),
                new ItemStack(ItemWith("unpriced"), 1), // no buyFor -> unknown
            ],
            RewardItems = [new ItemStack(ItemWith("out", fleaSell: 50_000), 1)],
        };

        TradeCost cost = ProfitCalculator.ForCraft(craft);

        cost.InputCost.Should().BeNull();
        cost.Profit.Should().BeNull();
        cost.OutputValue.Should().Be(50_000);
    }

    [Fact]
    public void Reward_sold_on_flea_is_valued_net_of_the_fee()
    {
        var reward = new Item
        {
            Id = "LEDX", Name = "LEDX", BasePrice = 40_000,
            SellFor = [new VendorPrice(VendorPrice.FleaMarketVendorName, 500_000, "RUB", 500_000)],
        };
        var barter = new Barter
        {
            TraderName = "Therapist", TraderLevel = 3,
            RequiredItems = [new ItemStack(ItemWith("in", fleaBuy: 100_000), 1)],
            RewardItems = [new ItemStack(reward, 1)],
        };

        TradeCost cost = ProfitCalculator.ForBarter(barter);
        int fee = FleaFee.Calculate(40_000, 500_000);

        cost.OutputValueGross.Should().Be(500_000);
        cost.FleaFeeTotal.Should().Be(fee).And.NotBe(0);
        cost.OutputValue.Should().Be(500_000 - fee, "output is net of the flea fee");
        cost.Profit.Should().Be(500_000 - fee - 100_000);
        cost.GrossProfit.Should().Be(400_000);
    }

    [Fact]
    public void Count_scales_the_cost()
    {
        var barter = new Barter
        {
            TraderName = "Prapor", TraderLevel = 1,
            RequiredItems = [new ItemStack(ItemWith("bullet", fleaBuy: 500), 60)], // 30,000
            RewardItems = [new ItemStack(ItemWith("gun", fleaSell: 45_000), 1)],
        };

        ProfitCalculator.ForBarter(barter).Profit.Should().Be(15_000);
    }
}
