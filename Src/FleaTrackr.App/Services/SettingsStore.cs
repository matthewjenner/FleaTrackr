using System.Text.Json;
using System.Text.Json.Serialization;

namespace FleaTrackr.App.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as a single hand-editable JSON file. Writes go
/// through <see cref="AtomicJson"/> so a crash mid-write never corrupts the file.
/// </summary>
public sealed class SettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Reads the settings file, or returns defaults if it is missing or unreadable.</summary>
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(filePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath), Options)
                       ?? new AppSettings();
            }
        }
        catch
        {
            // A corrupt settings file must not block startup - fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings) =>
        AtomicJson.Write(filePath, settings, Options);
}
