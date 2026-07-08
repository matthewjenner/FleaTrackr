using CommunityToolkit.Mvvm.ComponentModel;
using FleaTrackr.App.Services;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.ViewModels;

/// <summary>
/// Backs the main window: the window title, the app-wide PVP/PVE toggle, and the update banner.
/// Per-tab content view models are added here as each phase lands (Search first in P2).
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppHost _host;

    public MainWindowViewModel(AppHost host)
    {
        _host = host;
        _isPve = host.GameMode == GameMode.Pve;
        _host.GameModeChanged += OnGameModeChanged;
        Search = new SearchViewModel(host);
    }

    /// <summary>Window title, e.g. "FleaTrackr v0.1.0".</summary>
    public string Title => $"FleaTrackr v{AppVersion.Display}";

    /// <summary>Backs the Search tab (item search + price detail).</summary>
    public SearchViewModel Search { get; }

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
