namespace FleaTrackr.App.ViewModels;

/// <summary>A selectable price-history window for the Search detail chart.</summary>
public sealed record HistoryRange(string Label, int Days)
{
    /// <summary>The ranges offered by the chart's selector.</summary>
    public static readonly IReadOnlyList<HistoryRange> All =
    [
        new("7d", 7),
        new("30d", 30),
        new("90d", 90),
    ];
}
