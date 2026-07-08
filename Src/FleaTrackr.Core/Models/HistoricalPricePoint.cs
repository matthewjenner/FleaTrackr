namespace FleaTrackr.Core.Models;

/// <summary>
/// One sample of an item's flea price over time, from tarkov.dev <c>historicalItemPrices</c>.
/// Used to draw the detail-pane sparkline.
/// </summary>
public sealed record HistoricalPricePoint(DateTimeOffset Timestamp, int Price, int PriceMin, int OfferCount);
