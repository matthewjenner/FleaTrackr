using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the main window: the window title, the app-wide PVP/PVE toggle, the update banner, the tab
/// view models, and the restore/persist of session state (active tab, search query/selection - and,
/// via the window code-behind, the window geometry). Per-tab content view models are added here as
/// each phase lands.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppHost _host;

    public MainWindowViewModel(AppHost host)
    {
        _host = host;
        _isPve = host.GameMode == GameMode.Pve;
        _activeTabIndex = host.Session.ActiveTabIndex;
        _host.GameModeChanged += OnGameModeChanged;

        Search = new SearchViewModel(host);
        Watchlist = new WatchlistViewModel(host);
        BartersCrafts = new BartersCraftsViewModel(host);
        FlipFinder = new FlipFinderViewModel(host);

        // Restore and then persist search state (query + selection) across restarts.
        Search.RestoreSession(host.Session.LastSearchQuery, host.Session.SelectedItemId);
        Search.PropertyChanged += OnSearchChanged;
    }

    /// <summary>Window title, e.g. "FleaTrackr v0.1.0".</summary>
    public string Title => $"FleaTrackr v{AppVersion.Display}";

    /// <summary>Backs the Search tab (item search + price detail).</summary>
    public SearchViewModel Search { get; }

    /// <summary>Backs the Watchlist tab (tracked items, per-item refresh, alerts).</summary>
    public WatchlistViewModel Watchlist { get; }

    /// <summary>Backs the Barters &amp; Crafts tab (trade profit vs current flea prices).</summary>
    public BartersCraftsViewModel BartersCrafts { get; }

    /// <summary>Backs the Flip Finder tab (trader/flea arbitrage scan).</summary>
    public FlipFinderViewModel FlipFinder { get; }

    /// <summary>Initial session state, used by the window code-behind to restore geometry.</summary>
    public SessionState Session => _host.Session;

    /// <summary>Persists window geometry into the (debounced, crash-safe) session store.</summary>
    public void PersistWindow(double width, double height, int? x, int? y, bool maximized) =>
        _host.UpdateSession(s => s with
        {
            WindowWidth = width,
            WindowHeight = height,
            WindowX = x,
            WindowY = y,
            Maximized = maximized,
        });

    // ---- Active tab (restored/persisted) ----

    [ObservableProperty]
    private int _activeTabIndex;

    partial void OnActiveTabIndexChanged(int value) =>
        _host.UpdateSession(s => s with { ActiveTabIndex = value });

    private void OnSearchChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SearchViewModel.Query):
                _host.UpdateSession(s => s with { LastSearchQuery = Search.Query });
                break;
            case nameof(SearchViewModel.SelectedResult):
                _host.UpdateSession(s => s with { SelectedItemId = Search.SelectedResult?.Item.Id });
                break;
        }
    }

    // ---- PVP / PVE toggle ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPvp))]
    private bool _isPve;

    /// <summary>Bound to the "PVP" radio button; the inverse of <see cref="IsPve"/>.</summary>
    public bool IsPvp
    {
        get => !IsPve;
        set { if (value) IsPve = false; }
    }

    partial void OnIsPveChanged(bool value) =>
        _host.SetGameMode(value ? GameMode.Pve : GameMode.Pvp);

    private void OnGameModeChanged(GameMode mode) => IsPve = mode == GameMode.Pve;

    // ---- Update banner (wired to the real UpdateService in P6; hidden until then) ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateBannerVisible))]
    [NotifyPropertyChangedFor(nameof(UpdateBannerText))]
    private string? _availableUpdateVersion;

    /// <summary>Whether the "an update is available" banner shows.</summary>
    public bool IsUpdateBannerVisible => AvailableUpdateVersion is not null;

    /// <summary>Banner copy naming the available version.</summary>
    public string UpdateBannerText =>
        AvailableUpdateVersion is null ? "" : $"FleaTrackr {AvailableUpdateVersion} is available.";
}
