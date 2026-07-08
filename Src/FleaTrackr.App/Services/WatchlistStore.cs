using System.Text.Json;
using System.Text.Json.Serialization;
using FleaTrackr.Core.Watchlist;

namespace FleaTrackr.App.Services;

/// <summary>
/// Loads and saves the watchlist (a list of <see cref="WatchedItemConfig"/>) as
/// <c>watchlist.json</c>, written atomically via <see cref="AtomicJson"/> so a crash mid-save
/// never corrupts it. A missing or unreadable file yields an empty watchlist rather than failing.
/// </summary>
public sealed class WatchlistStore(string filePath)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public IReadOnlyList<WatchedItemConfig> Load()
    {
        try
        {
            if (File.Exists(filePath))
            {
                return JsonSerializer.Deserialize<List<WatchedItemConfig>>(File.ReadAllText(filePath), Options)
                       ?? [];
            }
        }
        catch
        {
            // A corrupt watchlist must not block startup - start empty.
        }

        return [];
    }

    public void Save(IReadOnlyList<WatchedItemConfig> items) =>
        AtomicJson.Write(filePath, items, Options);
}
