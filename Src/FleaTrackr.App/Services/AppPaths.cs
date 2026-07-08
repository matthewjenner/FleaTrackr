namespace FleaTrackr.App.Services;

/// <summary>
/// Resolves the per-user directory and file paths FleaTrackr persists to. Everything lives under
/// <c>%APPDATA%\FleaTrackr</c> - an ACL'd, per-user location outside the repo. FleaTrackr stores
/// no credentials (the tarkov.dev API is unauthenticated), so all of these files are plain JSON.
/// </summary>
public static class AppPaths
{
    /// <summary>Base config directory: <c>%APPDATA%\FleaTrackr</c>.</summary>
    public static string BaseDirectory
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "FleaTrackr");
        }
    }

    /// <summary>Non-secret app settings (game mode, refresh/alert defaults): <c>settings.json</c>.</summary>
    public static string SettingsFilePath => Path.Combine(BaseDirectory, "settings.json");

    /// <summary>Pinned watchlist items with per-item intervals and alert rules: <c>watchlist.json</c>.</summary>
    public static string WatchlistFilePath => Path.Combine(BaseDirectory, "watchlist.json");

    /// <summary>UI/session state for crash-safe restore-on-reopen: <c>session.json</c>.</summary>
    public static string SessionFilePath => Path.Combine(BaseDirectory, "session.json");
}
