using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// App-wide, non-secret settings. Persisted to <c>settings.json</c> as plain JSON. FleaTrackr
/// stores no credentials (the tarkov.dev API needs none), so everything here is safe to read and
/// edit in a text editor.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Which economy to query by default (PVP "regular" or PVE). Toggleable in the UI.</summary>
    public GameMode GameMode { get; init; } = GameMode.Pvp;

    /// <summary>Default per-item watchlist refresh interval, in seconds, for newly pinned items.</summary>
    public int DefaultRefreshSeconds { get; init; } = 60;

    /// <summary>
    /// The semver of a release the user explicitly clicked "skip" on. While that version is the
    /// latest, the update banner stays hidden; a newer release re-arms it.
    /// </summary>
    public string? SkippedUpdateVersion { get; init; }
}
