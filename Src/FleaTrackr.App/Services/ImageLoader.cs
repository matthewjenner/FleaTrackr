using System.Collections.Concurrent;
using Avalonia.Media.Imaging;

namespace FleaTrackr.App.Services;

/// <summary>
/// Loads and caches remote item icons (the API's <c>iconLink</c> URLs) as Avalonia bitmaps.
/// Avalonia has no built-in async loader for <c>http(s)</c> image sources, so views ask this for
/// a <see cref="Bitmap"/> and bind the result. Decoded bitmaps are cached by URL for the app's
/// lifetime - icon art is small and effectively immutable, so this avoids repeat downloads.
/// </summary>
public static class ImageLoader
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> Cache = new();

    /// <summary>Returns the decoded icon for <paramref name="url"/>, or null on any failure.</summary>
    public static Task<Bitmap?> LoadAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return Task.FromResult<Bitmap?>(null);
        return Cache.GetOrAdd(url, DownloadAsync);
    }

    private static async Task<Bitmap?> DownloadAsync(string url)
    {
        try
        {
            await using Stream stream = await Http.GetStreamAsync(url);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;
            return new Bitmap(buffer);
        }
        catch
        {
            // A missing/unreachable icon must never break the UI - just show no image.
            return null;
        }
    }
}
