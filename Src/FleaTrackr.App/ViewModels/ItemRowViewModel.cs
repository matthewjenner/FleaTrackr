using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// A display wrapper over a Core <see cref="Item"/>: exposes the item's prices as preformatted,
/// bindable strings (grouped roubles, signed percent) plus the raw change value for colour
/// binding. Keeps price formatting out of XAML and the underlying model UI-free. Immutable - a new
/// row is created when the item's data is refetched.
/// </summary>
public sealed class ItemRowViewModel(Item item)
{
    public Item Item { get; } = item;

    public string Name => Item.Name;
    public string ShortName => Item.ShortName;
    public string? IconLink => Item.IconLink;
    public string? WikiLink => Item.WikiLink;

    /// <summary>Flea sell price, falling back to the last-seen low if no live sell offer.</summary>
    public string FleaSellText => PriceFormat.Rub(Item.FleaSell?.PriceRub ?? Item.LastLowPrice);
    public string FleaBuyText => PriceFormat.Rub(Item.FleaBuy?.PriceRub);
    public string Avg24hText => PriceFormat.Rub(Item.Avg24hPrice);
    public string Low24hText => PriceFormat.Rub(Item.Low24hPrice);
    public string High24hText => PriceFormat.Rub(Item.High24hPrice);
    public string BasePriceText => PriceFormat.Rub(Item.BasePrice);

    /// <summary>Signed 48h change, e.g. "+2.5%".</summary>
    public string ChangeText => PriceFormat.Percent(Item.ChangeLast48hPercent);

    /// <summary>Raw 48h change for colour binding (green up / red down); 0 when unknown.</summary>
    public double ChangeValue => Item.ChangeLast48hPercent ?? 0;

    public string BestTraderText => Item.BestTraderSell is { } t
        ? $"{t.Vendor}  {PriceFormat.Rub(t.PriceRub)}"
        : "-";

    public string MinFleaLevelText =>
        Item.MinLevelForFlea is { } lvl ? $"Flea level {lvl}" : "No flea level requirement";

    public string OfferCountText =>
        Item.LastOfferCount is { } n ? $"{n:N0} offers" : "-";

    public string UpdatedText =>
        Item.Updated is { } u ? $"Updated {u.LocalDateTime:g}" : "Update time unknown";
}
