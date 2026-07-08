using FleaTrackr.Core;
using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// Composition root. Owns the persisted settings, the restored session state, the app-wide
/// game-mode selection, the tarkov.dev API client, and the watchlist service. ViewModels take an
/// <see cref="AppHost"/> and read/update shared state through it - they never touch the stores or
/// the network directly.
/// </summary>
public sealed class AppHost : IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly SessionStore _sessionStore;
    private readonly TarkovApiClient _apiClient;

    public AppHost()
    {
        _settingsStore = new SettingsStore(AppPaths.SettingsFilePath);
        Settings = _settingsStore.Load();

        _sessionStore = new SessionStore(AppPaths.SessionFilePath);
        Session = _sessionStore.Load();

        _apiClient = new TarkovApiClient();

        // Restore the economy the user last used (session wins over the settings default).
        GameMode = Session.GameMode;

        Watchlist = new WatchlistService(Api, () => GameMode, new WatchlistStore(AppPaths.WatchlistFilePath));

        // A mode switch re-baselines watched prices and is part of the restorable session.
        GameModeChanged += mode =>
        {
            Watchlist.InvalidateForModeChange();
            UpdateSession(s => s with { GameMode = mode });
        };
    }

    /// <summary>The tarkov.dev market API. ViewModels query prices/barters/crafts through this.</summary>
    public ITarkovApi Api => _apiClient;

    /// <summary>The watchlist (persisted items + live refresh + alerts).</summary>
    public WatchlistService Watchlist { get; }

    /// <summary>App-wide, non-secret settings. Update via <see cref="UpdateSettings"/>.</summary>
    public AppSettings Settings { get; private set; }

    /// <summary>Restored UI/session state. Mutate via <see cref="UpdateSession"/> (debounced save).</summary>
    public SessionState Session { get; private set; }

    /// <summary>The economy currently being viewed (PVP/PVE). Change via <see cref="SetGameMode"/>.</summary>
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

    /// <summary>Applies a change to the session state and schedules a debounced, crash-safe save.</summary>
    public void UpdateSession(Func<SessionState, SessionState> update)
    {
        Session = update(Session);
        _sessionStore.SaveDebounced(Session);
    }

    public void Dispose()
    {
        Watchlist.Dispose();
        _sessionStore.Dispose(); // flushes any pending session write
        _apiClient.Dispose();
    }
}
