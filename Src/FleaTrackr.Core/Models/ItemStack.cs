namespace FleaTrackr.Core.Models;

/// <summary>
/// A quantity of an item - one entry in a barter's or craft's required/reward list (the API's
/// <c>ContainedItem</c>). <see cref="Count"/> is a double because the API types it as a float,
/// though in practice it is a whole number of items.
/// </summary>
public sealed record ItemStack(Item Item, double Count);
