using System.Globalization;

namespace FleaTrackr.Core.Pricing;

/// <summary>
/// Formats prices and price changes for display. Uses invariant grouping (e.g. "150,000") and an
/// ASCII "RUB" suffix rather than the rouble glyph, per the project's ASCII-only UI-text rule.
/// A null price - the item is not currently traded - renders as a dash.
/// </summary>
public static class PriceFormat
{
    private const string Dash = "-";

    /// <summary>Grouped roubles, e.g. "150,000", or "-" if null.</summary>
    public static string Rub(int? value) =>
        value is null ? Dash : value.Value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>Grouped roubles with a unit, e.g. "150,000 RUB", or "-" if null.</summary>
    public static string RubWithUnit(int? value) =>
        value is null ? Dash : $"{Rub(value)} RUB";

    /// <summary>Signed percent to one decimal, e.g. "+2.5%" / "-3.1%", or "-" if null.</summary>
    public static string Percent(double? value)
    {
        if (value is null) return Dash;
        string sign = value.Value >= 0 ? "+" : "";
        return $"{sign}{value.Value.ToString("0.0", CultureInfo.InvariantCulture)}%";
    }
}
