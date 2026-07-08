using System.Text.Json;

namespace FleaTrackr.App.Services;

/// <summary>
/// Writes JSON to disk atomically: serialize to a sibling <c>*.tmp</c> file, flush it, then
/// replace the target in a single filesystem move. A crash (or a pulled-power event) mid-write
/// therefore never leaves a half-written config - the old file survives intact, or the new one
/// fully lands. Shared by every store (settings, watchlist, session).
/// </summary>
public static class AtomicJson
{
    /// <summary>Serializes <paramref name="value"/> and atomically replaces <paramref name="filePath"/>.</summary>
    public static void Write<T>(string filePath, T value, JsonSerializerOptions options)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(value, options));

        // File.Move with overwrite is atomic on the same volume; the temp file always sits beside
        // the target, so this never crosses volumes.
        File.Move(tempPath, filePath, overwrite: true);
    }
}
