namespace FleaTrackr.Core.Models;

/// <summary>
/// A trader barter: hand over <see cref="RequiredItems"/> to receive <see cref="RewardItems"/>,
/// available once the trader is at <see cref="TraderLevel"/> loyalty. Mapped from the tarkov.dev
/// <c>Barter</c> type. Profit is computed later by comparing input vs output value at flea prices.
/// </summary>
public sealed record Barter
{
    public required string TraderName { get; init; }
    public int TraderLevel { get; init; }
    public IReadOnlyList<ItemStack> RequiredItems { get; init; } = [];
    public IReadOnlyList<ItemStack> RewardItems { get; init; } = [];
}
