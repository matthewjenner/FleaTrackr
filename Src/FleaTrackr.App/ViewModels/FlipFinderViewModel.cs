using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleaTrackr.App.Services;
using FleaTrackr.Core;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Pricing;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the Flip Finder tab: a bounded, paged scan of the market that ranks trader/flea arbitrage
/// opportunities by profit. The scan is capped (<see cref="MaxItems"/>) and paged with a small
/// inter-page pause so it stays comfortably under the API rate limit, reports progress as it runs,
/// and is cancellable. Results are recomputed for the active PVP/PVE economy.
/// </summary>
public sealed partial class FlipFinderViewModel : ViewModelBase
{
    private const int PageSize = 200;
    private const int MaxItems = 2000;
    private const int MaxDisplay = 200;
    private static readonly TimeSpan InterPageDelay = TimeSpan.FromMilliseconds(150);

    private readonly ITarkovApi _api;
    private readonly Func<GameMode> _mode;
    private readonly Func<AppSettings> _settings;
    private List<FlipOpportunity> _allFlips = [];

    public FlipFinderViewModel(AppHost host) : this(host.Api, () => host.GameMode, () => host.Settings)
    {
        host.GameModeChanged += _ => OnGameModeChanged();
    }

    public FlipFinderViewModel(ITarkovApi api, Func<GameMode> mode, Func<AppSettings>? settings = null)
    {
        _api = api;
        _mode = mode;
        _settings = settings ?? (() => new AppSettings());
        _minProfit = _settings().DefaultMinProfit;
    }

    public ObservableCollection<FlipRowViewModel> Results { get; } = [];

    public IReadOnlyList<FlipSortOption> SortOptions => FlipSortOption.All;
    public IReadOnlyList<FlipDirectionOption> DirectionOptions => FlipDirectionOption.All;

    [ObservableProperty] private FlipSortOption _selectedSort = FlipSortOption.All[0];
    [ObservableProperty] private FlipDirectionOption _selectedDirection = FlipDirectionOption.All[0];

    partial void OnSelectedSortChanged(FlipSortOption value) => ApplyView();
    partial void OnSelectedDirectionChanged(FlipDirectionOption value) => ApplyView();

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _statusMessage = "Scan the market to find trader/flea flips.";
    [ObservableProperty] private int _minProfit;

    /// <summary>Scans the market (bounded/paged) and populates ranked flip opportunities.</summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Results.Clear();
        var items = new List<Item>();
        try
        {
            for (int offset = 0; offset < MaxItems; offset += PageSize)
            {
                IReadOnlyList<Item> page = await _api.GetItemsPageAsync(PageSize, offset, _mode(), ct);
                items.AddRange(page);
                StatusMessage = $"Scanning... {items.Count} items";

                if (page.Count < PageSize) break; // reached the end of the list
                await Task.Delay(InterPageDelay, ct);
            }

            AppSettings settings = _settings();
            _allFlips = FlipFinder.FindAll(
                items, MinProfit, settings.FleaFeeReductionPercent, settings.PlayerFleaLevel).ToList();
            ApplyView();

            StatusMessage = BuildSummary(items.Count, _allFlips.Count);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Scan cancelled. Showing {Results.Count} flips found so far.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>Applies the direction filter and sort to the last scan and repopulates the list.</summary>
    private void ApplyView()
    {
        IEnumerable<FlipOpportunity> view = SelectedDirection.Filter switch
        {
            FlipDirectionFilter.TraderToFlea => _allFlips.Where(o => o.Direction == FlipDirection.TraderToFlea),
            FlipDirectionFilter.FleaToTrader => _allFlips.Where(o => o.Direction == FlipDirection.FleaToTrader),
            _ => _allFlips,
        };

        view = SelectedSort.Sort == FlipSort.Roi
            ? view.OrderByDescending(o => o.RoiPercent)
            : view.OrderByDescending(o => o.Profit);

        Results.Clear();
        foreach (FlipOpportunity op in view.Take(MaxDisplay))
            Results.Add(new FlipRowViewModel(op));
    }

    private string BuildSummary(int scanned, int found)
    {
        string capped = scanned >= MaxItems ? $" (scan capped at {MaxItems})" : "";
        string shown = found > MaxDisplay ? $", showing top {MaxDisplay}" : "";
        return $"Scanned {scanned} items{capped}. Found {found} flips over {PriceFormat.Rub(MinProfit)} profit{shown}.";
    }

    private void OnGameModeChanged()
    {
        _allFlips.Clear();
        Results.Clear();
        StatusMessage = "Economy changed - scan again to refresh flips for the new prices.";
    }
}
