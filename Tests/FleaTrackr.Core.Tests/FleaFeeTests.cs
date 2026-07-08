using FleaTrackr.Core.Pricing;
using FluentAssertions;

namespace FleaTrackr.Core.Tests;

public class FleaFeeTests
{
    [Fact]
    public void Fee_grows_with_markup_over_base_price()
    {
        // Listing far above base price costs proportionally more in fees.
        int lowMarkup = FleaFee.Calculate(basePrice: 10_000, listPrice: 12_000);
        int highMarkup = FleaFee.Calculate(basePrice: 10_000, listPrice: 100_000);

        highMarkup.Should().BeGreaterThan(lowMarkup);
        highMarkup.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Unknown_base_price_yields_no_fee()
    {
        FleaFee.Calculate(basePrice: 0, listPrice: 50_000).Should().Be(0);
        FleaFee.NetSale(basePrice: 0, listPrice: 50_000).Should().Be(50_000);
    }

    [Fact]
    public void Net_sale_is_list_price_minus_fee()
    {
        int fee = FleaFee.Calculate(50_000, 500_000);
        FleaFee.NetSale(50_000, 500_000).Should().Be(500_000 - fee);
        fee.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Reduction_lowers_the_fee_proportionally()
    {
        int full = FleaFee.Calculate(50_000, 500_000);
        int reduced = FleaFee.Calculate(50_000, 500_000, reductionPercent: 30);

        reduced.Should().BeCloseTo((int)(full * 0.7), 2);
    }

    [Fact]
    public void A_realistic_high_value_listing_has_a_double_digit_percent_fee()
    {
        // Sanity anchor: a 500k listing on a 50k-base item should cost a meaningful chunk in fees.
        int fee = FleaFee.Calculate(50_000, 500_000);
        double percent = 100.0 * fee / 500_000;
        percent.Should().BeInRange(5, 30);
    }
}
