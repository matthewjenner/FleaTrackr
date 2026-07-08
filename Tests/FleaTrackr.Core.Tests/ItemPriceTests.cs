using FleaTrackr.Core.Models;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class ItemPriceTests
{
    private static VendorPrice Flea(int rub) => new(VendorPrice.FleaMarketVendorName, rub, "RUB", rub);
    private static VendorPrice Trader(string name, int rub) => new(name, rub, "RUB", rub);

    [Fact]
    public void FleaSell_and_FleaBuy_pick_the_flea_market_vendor_entry()
    {
        var item = new Item
        {
            Id = "1", Name = "GPU",
            SellFor = [Trader("Mechanic", 100_000), Flea(150_000), Trader("Fence", 90_000)],
            BuyFor = [Flea(160_000), Trader("Mechanic", 200_000)],
        };

        item.FleaSell!.PriceRub.Should().Be(150_000);
        item.FleaBuy!.PriceRub.Should().Be(160_000);
    }

    [Fact]
    public void BestTraderSell_takes_the_highest_rouble_trader_ignoring_flea()
    {
        var item = new Item
        {
            Id = "1", Name = "GPU",
            // Peacekeeper's price is in USD but priceRUB normalizes it - compare on that.
            SellFor = [Trader("Therapist", 120_000), Flea(150_000), Trader("Peacekeeper", 135_000)],
        };

        item.BestTraderSell!.Vendor.Should().Be("Peacekeeper");
        item.BestTraderSell!.PriceRub.Should().Be(135_000);
    }

    [Fact]
    public void Missing_flea_listing_yields_null_prices_not_an_exception()
    {
        var item = new Item { Id = "1", Name = "Rare", SellFor = [Trader("Fence", 5_000)] };

        item.FleaSell.Should().BeNull();
        item.FleaBuy.Should().BeNull();
        item.BestTraderSell!.PriceRub.Should().Be(5_000);
    }

    [Fact]
    public void PriceSnapshot_FromItem_flattens_the_reference_price()
    {
        var when = DateTimeOffset.UnixEpoch;
        var listed = new Item
        {
            Id = "1", Name = "GPU", Avg24hPrice = 148_000, ChangeLast48hPercent = -2.5,
            SellFor = [Flea(150_000), Trader("Mechanic", 100_000)],
            BuyFor = [Flea(160_000)],
        };

        PriceSnapshot snap = PriceSnapshot.FromItem(listed, when);
        snap.FleaSell.Should().Be(150_000);
        snap.BestTraderSell.Should().Be(100_000);
        snap.BestTraderName.Should().Be("Mechanic");
        snap.ReferencePrice.Should().Be(150_000, "flea sell wins when listed");

        // When not on flea, the reference price falls back to the best trader.
        var unlisted = new Item { Id = "2", Name = "Rare", SellFor = [Trader("Fence", 5_000)] };
        PriceSnapshot.FromItem(unlisted, when).ReferencePrice.Should().Be(5_000);
    }
}
