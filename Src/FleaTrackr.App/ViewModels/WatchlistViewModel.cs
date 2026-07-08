using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the Watchlist tab: the list of watched rows, a running feed of fired alerts, and the 1s
/// timer that keeps each row's "refreshed Ns ago" label current. Live snapshots and alerts arrive
/// from the <see cref="WatchlistService"/> off the UI thread and are marshalled here via the
/// dispatcher before touching bound state, so the fast per-item refresh never blocks the UI.
/// </summary>
public sealed partial class WatchlistViewModel : ViewModelBase
{
    private const int MaxRecentAlerts = 30;

    private readonly WatchlistService _service;
    private readonly Dictionary<string, WatchlistItemViewModel> _rows = new();
    private readonly DispatcherTimer _relativeTimer;

    public WatchlistViewModel(AppHost host)
    {
        _service = host.Watchlist;

        foreach (WatchedItemConfig cfg in _service.Items)
            AddRow(cfg);

        _service.Added += cfg => Post(() => AddRow(cfg));
        _service.Removed += id => Post(() => RemoveRow(id));
        _service.Changed += cfg => Post(() => { if (_rows.TryGetValue(cfg.ItemId, out var r)) r.UpdateConfig(cfg); });
        _service.Updated += (id, snap) => Post(() => OnUpdated(id, snap));
        _service.AlertTriggered += alert => Post(() => OnAlert(alert));

        _relativeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _relativeTimer.Tick += (_, _) => TickRows();
        _relativeTimer.Start();
    }

    public ObservableCollection<WatchlistItemViewModel> Items { get; } = [];

    /// <summary>Recent fired alerts, newest first, for the alert feed panel.</summary>
    public ObservableCollection<string> RecentAlerts { get; } = [];

    /// <summary>True when the watchlist has no items, to show an empty-state hint.</summary>
    [ObservableProperty]
    private bool _isEmpty = true;

    private void AddRow(WatchedItemConfig cfg)
    {
        if (_rows.ContainsKey(cfg.ItemId)) return;
        var row = new WatchlistItemViewModel(cfg, _service);
        _rows[cfg.ItemId] = row;
        Items.Add(row);
        IsEmpty = Items.Count == 0;
    }

    private void RemoveRow(string id)
    {
        if (!_rows.Remove(id, out WatchlistItemViewModel? row)) return;
        Items.Remove(row);
        IsEmpty = Items.Count == 0;
    }

    private void OnUpdated(string id, PriceSnapshot snapshot)
    {
        if (_rows.TryGetValue(id, out WatchlistItemViewModel? row))
            row.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);
    }

    private void OnAlert(TriggeredAlert alert)
    {
        if (_rows.TryGetValue(alert.ItemId, out WatchlistItemViewModel? row))
            row.MarkAlerting();

        RecentAlerts.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss}  {alert.Message}");
        while (RecentAlerts.Count > MaxRecentAlerts)
            RecentAlerts.RemoveAt(RecentAlerts.Count - 1);
    }

    private void TickRows()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (WatchlistItemViewModel row in Items)
            row.Tick(now);
    }

    [RelayCommand]
    private void ClearAlertFeed() => RecentAlerts.Clear();

    private static void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }
}
