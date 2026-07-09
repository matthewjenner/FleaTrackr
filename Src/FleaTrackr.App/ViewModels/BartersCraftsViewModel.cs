using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FleaTrackr.App.Services;
using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the Barters &amp; Crafts tab: pick an item via a debounced search, then list every barter
/// and hideout craft that produces it with computed profit (input acquisition cost vs output flea
/// value), best profit first. Re-runs for the active PVP/PVE economy.
/// </summary>
public sealed partial class BartersCraftsViewModel : ViewModelBase
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly ITarkovApi _api;
    private readonly Func<GameMode> _mode;
    private readonly Func<AppSettings> _settings;
    private CancellationTokenSource? _searchCts;

    public BartersCraftsViewModel(AppHost host) : this(host.Api, () => host.GameMode, () => host.Settings)
    {
        host.GameModeChanged += _ => OnGameModeChanged();
    }

    public BartersCraftsViewModel(ITarkovApi api, Func<GameMode> mode, Func<AppSettings>? settings = null)
    {
        _api = api;
        _mode = mode;
        _settings = settings ?? (() => new AppSettings());
    }

    public ObservableCollection<ItemRowViewModel> Results { get; } = [];
    /// <summary>Barters that produce the selected item.</summary>
    public ObservableCollection<TradeRowViewModel> Barters { get; } = [];
    /// <summary>Crafts that produce the selected item.</summary>
    public ObservableCollection<TradeRowViewModel> Crafts { get; } = [];
    /// <summary>Barters that consume the selected item as an input.</summary>
    public ObservableCollection<TradeRowViewModel> UsingBarters { get; } = [];
    /// <summary>Crafts that consume the selected item as an input.</summary>
    public ObservableCollection<TradeRowViewModel> UsingCrafts { get; } = [];

    [ObservableProperty] private string _query = "";
    [ObservableProperty] private ItemRowViewModel? _selectedResult;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _statusMessage;

    /// <summary>True once an item is selected but it has no related trades at all.</summary>
    [ObservableProperty] private bool _noTrades;

    /// <summary>True until an item is selected, to show the initial hint.</summary>
    [ObservableProperty] private bool _hasSelection;

    [ObservableProperty] private bool _hasBarters;
    [ObservableProperty] private bool _hasCrafts;
    [ObservableProperty] private bool _hasUsingBarters;
    [ObservableProperty] private bool _hasUsingCrafts;

    partial void OnQueryChanged(string value) => _ = DebouncedSearchAsync(value);

    partial void OnSelectedResultChanged(ItemRowViewModel? value) => _ = LoadTradesAsync(value);

    private async Task DebouncedSearchAsync(string query)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            return;
        }

        try
        {
            await Task.Delay(DebounceDelay, cts.Token);
            IReadOnlyList<Item> items = await _api.SearchItemsAsync(query, _mode(), limit: 20, cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            Results.Clear();
            foreach (Item item in items)
                Results.Add(new ItemRowViewModel(item));
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer query.
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
    }

    /// <summary>Loads and profit-ranks the barters and crafts for the given item. Public for tests.</summary>
    public async Task LoadTradesAsync(ItemRowViewModel? row)
    {
        Barters.Clear();
        Crafts.Clear();
        UsingBarters.Clear();
        UsingCrafts.Clear();
        NoTrades = false;
        HasSelection = row is not null;
        if (row is null) return;

        IsBusy = true;
        StatusMessage = null;
        try
        {
            ItemTrades trades = await _api.GetItemTradesAsync(row.Item.Id, _mode());
            double feeReduction = _settings().FleaFeeReductionPercent;

            Fill(Barters, trades.BartersFor.Select(b => TradeRowViewModel.FromBarter(b, feeReduction)));
            Fill(Crafts, trades.CraftsFor.Select(c => TradeRowViewModel.FromCraft(c, feeReduction)));
            Fill(UsingBarters, trades.BartersUsing.Select(b => TradeRowViewModel.FromBarter(b, feeReduction)));
            Fill(UsingCrafts, trades.CraftsUsing.Select(c => TradeRowViewModel.FromCraft(c, feeReduction)));

            HasBarters = Barters.Count > 0;
            HasCrafts = Crafts.Count > 0;
            HasUsingBarters = UsingBarters.Count > 0;
            HasUsingCrafts = UsingCrafts.Count > 0;
            NoTrades = !(HasBarters || HasCrafts || HasUsingBarters || HasUsingCrafts);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load trades: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Adds trade rows to a collection, best profit first.
    private static void Fill(ObservableCollection<TradeRowViewModel> target, IEnumerable<TradeRowViewModel> rows)
    {
        foreach (TradeRowViewModel t in rows.OrderByDescending(t => t.SortKey ?? int.MinValue))
            target.Add(t);
    }

    private void OnGameModeChanged()
    {
        if (SelectedResult is { } row)
            _ = LoadTradesAsync(row);
    }
}
