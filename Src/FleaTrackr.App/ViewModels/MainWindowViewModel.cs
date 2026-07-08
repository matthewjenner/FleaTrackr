using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    /// <summary>Number of tabs in the shell; keep in step with the TabControl in MainWindow.axaml.</summary>
    private const int TabCount = 4;

    private readonly AppHost _host;

    public MainWindowViewModel(AppHost host)
    {
        _host = host;
        _isPve = host.GameMode == GameMode.Pve;
        // Clamp the restored tab index so a stale value (e.g. a removed tab) never selects nothing.
        _activeTabIndex = Math.Clamp(host.Session.ActiveTabIndex, 0, TabCount - 1);
        _host.GameModeChanged += OnGameModeChanged;

        Search = new SearchViewModel(host);
        Watchlist = new WatchlistViewModel(host);
        BartersCrafts = new BartersCraftsViewModel(host);
        FlipFinder = new FlipFinderViewModel(host);

        // Restore and then persist search state (query + selection) across restarts.
        Search.RestoreSession(host.Session.LastSearchQuery, host.Session.SelectedItemId);
        Search.PropertyChanged += OnSearchChanged;

        _host.Updates.UpdateAvailableChanged += OnUpdateAvailableChanged;
        _availableUpdateVersion = _host.Updates.AvailableVersion;
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

    // ---- Update banner ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateBannerVisible))]
    [NotifyPropertyChangedFor(nameof(UpdateBannerText))]
    private string? _availableUpdateVersion;

    /// <summary>Whether the "an update is available" banner shows.</summary>
    public bool IsUpdateBannerVisible => AvailableUpdateVersion is not null;

    /// <summary>Banner copy naming the available version.</summary>
    public string UpdateBannerText =>
        AvailableUpdateVersion is null ? "" : $"FleaTrackr {AvailableUpdateVersion} is available.";

    /// <summary>
    /// The Install button is enabled only for installed (Velopack) builds. Under <c>dotnet run</c>
    /// the banner still shows for UI testing, but installing is a no-op so the button is disabled.
    /// </summary>
    public bool CanInstallUpdate => _host.Updates.CanInstall;

    [RelayCommand]
    private async Task InstallUpdateAsync() => await _host.Updates.InstallAndRestartAsync();

    [RelayCommand]
    private void SkipUpdate() => _host.Updates.SkipCurrentVersion();

    [RelayCommand]
    private void DismissUpdate() => _host.Updates.DismissForNow();

    private void OnUpdateAvailableChanged(string? version)
    {
        AvailableUpdateVersion = version;
        OnPropertyChanged(nameof(CanInstallUpdate));
    }
}
