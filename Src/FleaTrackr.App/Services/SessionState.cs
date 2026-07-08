using FleaTrackr.Core.Models;

namespace FleaTrackr.App.Services;

/// <summary>
/// The UI/session state FleaTrackr restores on reopen: which tab was active, the window geometry,
/// the selected economy, and the last search query/selection. Persisted to <c>session.json</c>,
/// debounced so a crash still leaves the most recent state on disk. Every field has a sane default
/// so a missing or partial file degrades gracefully.
/// </summary>
public sealed record SessionState
{
    public int ActiveTabIndex { get; init; }

    public double WindowWidth { get; init; } = 1040;
    public double WindowHeight { get; init; } = 680;

    /// <summary>Window top-left, or null to center on first run / when maximized.</summary>
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }
    public bool Maximized { get; init; }

    public GameMode GameMode { get; init; } = GameMode.Pvp;

    public string LastSearchQuery { get; init; } = "";
    public string? SelectedItemId { get; init; }
    public string? SelectedWatchlistItemId { get; init; }
}
