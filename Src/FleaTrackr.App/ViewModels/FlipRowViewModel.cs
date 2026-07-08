using FleaTrackr.Core.Pricing;

namespace FleaTrackr.App.ViewModels;

/// <summary>One ranked flip opportunity, formatted for the Flip Finder list.</summary>
public sealed class FlipRowViewModel(FlipOpportunity op)
{
    public string Name => op.Item.Name;
    public string ShortName => op.Item.ShortName;

    public string DirectionText =>
        op.Direction == FlipDirection.TraderToFlea ? "Trader -> Flea" : "Flea -> Trader";

    public string BuyText => $"{op.BuyFrom}  {PriceFormat.Rub(op.BuyPrice)}";
    public string SellText => $"{op.SellTo}  {PriceFormat.Rub(op.SellPrice)}";

    /// <summary>Net profit (already net of the estimated flea fee, where one applies).</summary>
    public string ProfitText => PriceFormat.Rub(op.Profit);
    public double ProfitValue => op.Profit;
    public string RoiText => $"{op.RoiPercent:0.#}%";

    /// <summary>Estimated flea sales fee already subtracted from profit, e.g. "less fee ~1,086".</summary>
    public string FeeText => $"less fee ~{PriceFormat.Rub(op.Fee)}";

    /// <summary>True for trader-to-flea flips, which incur the (already-subtracted) flea fee.</summary>
    public bool IsFleaSale => op.Direction == FlipDirection.TraderToFlea;
}
