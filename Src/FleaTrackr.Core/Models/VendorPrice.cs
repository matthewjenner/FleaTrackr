namespace FleaTrackr.Core.Models;

/// <summary>
/// One buy-from or sell-to offer for an item, as returned in the tarkov.dev <c>buyFor</c> /
/// <c>sellFor</c> arrays. The vendor named <see cref="FleaMarketVendorName"/> represents the flea
/// market itself; every other vendor is a trader (or Fence). Compare offers by <see cref="PriceRub"/>,
/// which normalizes non-RUB trader prices (e.g. Peacekeeper's USD) to roubles.
/// </summary>
public sealed record VendorPrice(string Vendor, int Price, string Currency, int PriceRub)
{
    /// <summary>The exact vendor name the API uses for the flea market.</summary>
    public const string FleaMarketVendorName = "Flea Market";

    /// <summary>True when this offer is the flea market rather than a trader.</summary>
    public bool IsFlea => string.Equals(Vendor, FleaMarketVendorName, StringComparison.Ordinal);
}
