namespace FleaTrackr.App.ViewModels;

/// <summary>How to order Search results.</summary>
public enum SearchSort { Relevance, PriceHighToLow, ChangeHighToLow, NameAToZ }

/// <summary>A labelled Search sort choice for the sort dropdown.</summary>
public sealed record SearchSortOption(string Label, SearchSort Sort)
{
    public static readonly IReadOnlyList<SearchSortOption> All =
    [
        new("Relevance", SearchSort.Relevance),
        new("Flea price (high to low)", SearchSort.PriceHighToLow),
        new("48h change (high to low)", SearchSort.ChangeHighToLow),
        new("Name (A to Z)", SearchSort.NameAToZ),
    ];
}

/// <summary>How to order Flip Finder results.</summary>
public enum FlipSort { Profit, Roi }

/// <summary>A labelled Flip Finder sort choice.</summary>
public sealed record FlipSortOption(string Label, FlipSort Sort)
{
    public static readonly IReadOnlyList<FlipSortOption> All =
    [
        new("Profit", FlipSort.Profit),
        new("ROI", FlipSort.Roi),
    ];
}

/// <summary>Which flip directions to show.</summary>
public enum FlipDirectionFilter { All, TraderToFlea, FleaToTrader }

/// <summary>A labelled Flip Finder direction filter.</summary>
public sealed record FlipDirectionOption(string Label, FlipDirectionFilter Filter)
{
    public static readonly IReadOnlyList<FlipDirectionOption> All =
    [
        new("All directions", FlipDirectionFilter.All),
        new("Trader -> Flea", FlipDirectionFilter.TraderToFlea),
        new("Flea -> Trader", FlipDirectionFilter.FleaToTrader),
    ];
}
