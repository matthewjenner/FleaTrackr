namespace FleaTrackr.Core.Models;

/// <summary>
/// A hideout craft: consume <see cref="RequiredItems"/> at a station of <see cref="StationLevel"/>
/// over <see cref="Duration"/> to produce <see cref="RewardItems"/>. Mapped from the tarkov.dev
/// <c>Craft</c> type (its <c>duration</c> is in seconds).
/// </summary>
public sealed record Craft
{
    public required string StationName { get; init; }
    public int StationLevel { get; init; }

    /// <summary>How long the craft takes to complete.</summary>
    public TimeSpan Duration { get; init; }

    public IReadOnlyList<ItemStack> RequiredItems { get; init; } = [];
    public IReadOnlyList<ItemStack> RewardItems { get; init; } = [];
}
