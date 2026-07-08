using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleaTrackr.App.Services;
using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the Search tab: a debounced item search against the tarkov.dev API, a results list, and a
/// detail pane (price breakdown, icon, and a price-history sparkline) for the selected item. Re-runs
/// the active search and reloads the open item whenever the app-wide PVP/PVE mode changes.
/// </summary>
public sealed partial class SearchViewModel : ViewModelBase
{
    private const int HistoryDays = 7;
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly ITarkovApi _api;
    private readonly Func<GameMode> _mode;
    private CancellationTokenSource? _searchCts;

    /// <summary>Production constructor: wires to the app's API and reacts to the PVP/PVE toggle.</summary>
    public SearchViewModel(AppHost host) : this(host.Api, () => host.GameMode)
    {
        host.GameModeChanged += _ => OnGameModeChanged();
    }

    /// <summary>Core constructor, injectable for tests (fake API + fixed/served game mode).</summary>
    public SearchViewModel(ITarkovApi api, Func<GameMode> mode)
    {
        _api = api;
        _mode = mode;
    }

    public ObservableCollection<ItemRowViewModel> Results { get; } = [];

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private ItemRowViewModel? _selectedResult;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private IReadOnlyList<int>? _sparklinePoints;

    [ObservableProperty]
    private Bitmap? _selectedIcon;

    /// <summary>True once a search has run and returned nothing, to show an empty-state hint.</summary>
    [ObservableProperty]
    private bool _isEmptyResult;

    partial void OnQueryChanged(string value) => _ = DebouncedSearchAsync(value);

    partial void OnSelectedResultChanged(ItemRowViewModel? value) => _ = LoadDetailAsync(value);

    private async Task DebouncedSearchAsync(string query)
    {
        // Cancel any in-flight/pending search; only the latest keystroke should hit the API.
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            IsEmptyResult = false;
            StatusMessage = null;
            return;
        }

        try
        {
            await Task.Delay(DebounceDelay, cts.Token);
            await RunSearchAsync(query, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer query - ignore.
        }
    }

    /// <summary>Runs the search for the current <see cref="Query"/> immediately (no debounce).</summary>
    public Task SearchAsync(CancellationToken ct = default) => RunSearchAsync(Query, ct);

    private async Task RunSearchAsync(string query, CancellationToken ct)
    {
        IsBusy = true;
        StatusMessage = null;
        try
        {
            IReadOnlyList<Item> items = await _api.SearchItemsAsync(query, _mode(), limit: 30, ct);
            ct.ThrowIfCancellationRequested();

            Results.Clear();
            foreach (Item item in items)
                Results.Add(new ItemRowViewModel(item));

            IsEmptyResult = Results.Count == 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            if (_searchCts?.IsCancellationRequested != true)
                IsBusy = false;
        }
    }

    private async Task LoadDetailAsync(ItemRowViewModel? row)
    {
        SparklinePoints = null;
        SelectedIcon = null;
        if (row is null) return;

        // Icon and history are independent; load both, tolerating failure of either.
        SelectedIcon = await ImageLoader.LoadAsync(row.IconLink);

        try
        {
            IReadOnlyList<HistoricalPricePoint> history =
                await _api.GetPriceHistoryAsync(row.Item.Id, HistoryDays, _mode());
            SparklinePoints = history.Count > 0 ? history.Select(p => p.Price).ToList() : null;
        }
        catch
        {
            // A missing history just means no sparkline - not an error worth surfacing.
            SparklinePoints = null;
        }
    }

    private void OnGameModeChanged()
    {
        // Reprice the current results and the open detail for the newly selected economy.
        if (!string.IsNullOrWhiteSpace(Query))
            _ = DebouncedSearchAsync(Query);
        if (SelectedResult is { } row)
            _ = LoadDetailAsync(row);
    }

    [RelayCommand]
    private void OpenWiki()
    {
        string? url = SelectedResult?.WikiLink;
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            StatusMessage = "Could not open the wiki link.";
        }
    }
}
