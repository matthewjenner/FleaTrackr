namespace FleaTrackr.Core.Models;

/// <summary>
/// Which Escape from Tarkov economy to query. The tarkov.dev API serves separate flea-market
/// prices per mode via its <c>gameMode</c> argument. <see cref="ToApiValue"/> maps these to the
/// exact strings the API expects ("regular" / "pve").
/// </summary>
public enum GameMode
{
    /// <summary>Standard online PVP raids - the API's "regular" game mode.</summary>
    Pvp,

    /// <summary>PVE (co-op / offline progression) economy - the API's "pve" game mode.</summary>
    Pve,
}

/// <summary>Helpers for mapping <see cref="GameMode"/> to and from the tarkov.dev API wire value.</summary>
public static class GameModeExtensions
{
    /// <summary>The API <c>gameMode</c> argument value for this mode ("regular" or "pve").</summary>
    public static string ToApiValue(this GameMode mode) =>
        mode == GameMode.Pve ? "pve" : "regular";
}
