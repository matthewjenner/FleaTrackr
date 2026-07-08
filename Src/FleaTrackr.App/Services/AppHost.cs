using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// Composition root. Owns the persisted app settings, the app-wide game-mode selection, and the
/// tarkov.dev API client. ViewModels take an <see cref="AppHost"/> and read/update shared state
/// through it - they never touch the stores or the network directly. Services added in later
/// phases (watchlist, refresh scheduler, update poller, session store) hang off here too.
/// </summary>
public sealed class AppHost : IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly TarkovApiClient _apiClient;

    public AppHost()
    {
        _settingsStore = new SettingsStore(AppPaths.SettingsFilePath);
        Settings = _settingsStore.Load();
        GameMode = Settings.GameMode;
        _apiClient = new TarkovApiClient();
    }

    /// <summary>The tarkov.dev market API. ViewModels query prices/barters/crafts through this.</summary>
    public ITarkovApi Api => _apiClient;

    /// <summary>App-wide, non-secret settings. Update via <see cref="UpdateSettings"/>.</summary>
    public AppSettings Settings { get; private set; }

    /// <summary>
    /// The economy currently being viewed (PVP/PVE). Distinct from the persisted default in
    /// <see cref="AppSettings.GameMode"/> so a session can toggle without rewriting settings on
    /// every flip; call <see cref="SetGameMode"/> to change it and notify listeners.
    /// </summary>
    public GameMode GameMode { get; private set; }

    /// <summary>Raised after <see cref="GameMode"/> changes - fired on the caller's thread.</summary>
    public event Action<GameMode>? GameModeChanged;

    /// <summary>Raised after <see cref="Settings"/> changes - fired on the caller's thread.</summary>
    public event Action<AppSettings>? SettingsChanged;

    /// <summary>Switches the active economy and notifies listeners (a no-op if unchanged).</summary>
    public void SetGameMode(GameMode mode)
    {
        if (mode == GameMode) return;
        GameMode = mode;
        GameModeChanged?.Invoke(mode);
    }

    /// <summary>Persists new non-secret settings and notifies listeners.</summary>
    public void UpdateSettings(AppSettings settings)
    {
        Settings = settings;
        _settingsStore.Save(settings);
        SettingsChanged?.Invoke(settings);
    }

    public void Dispose()
    {
        _apiClient.Dispose();
        // Later phases add the refresh scheduler and update poller to dispose here too.
    }
}
