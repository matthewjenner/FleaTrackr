namespace FleaTrackr.Core.Models;

/// <summary>
/// All the trades related to one item: those that <b>produce</b> it (barters/crafts you run to get
/// it) and those that <b>consume</b> it (where the item is an input). Fetched in a single query.
/// </summary>
public sealed record ItemTrades(
    IReadOnlyList<Barter> BartersFor,
    IReadOnlyList<Craft> CraftsFor,
    IReadOnlyList<Barter> BartersUsing,
    IReadOnlyList<Craft> CraftsUsing)
{
    public static readonly ItemTrades Empty = new([], [], [], []);
}
