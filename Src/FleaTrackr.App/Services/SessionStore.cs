using System.Text.Json;
using System.Text.Json.Serialization;

namespace FleaTrackr.App.Services;

/// <summary>
/// Persists <see cref="SessionState"/> to <c>session.json</c> for crash-safe restore. Writes are
/// atomic (via <see cref="AtomicJson"/>) and <see cref="SaveDebounced"/> coalesces the flurry of
/// changes from typing/clicking into a single write ~1s after activity settles - so the app is not
/// rewriting the file on every keystroke, yet a hard crash still loses at most that last second of
/// state. <see cref="Flush"/> forces any pending write immediately (called on clean shutdown).
/// </summary>
public sealed class SessionStore(string filePath, TimeSpan? debounce = null) : IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly TimeSpan _debounce = debounce ?? TimeSpan.FromSeconds(1);
    private readonly Lock _gate = new();
    private Timer? _timer;
    private SessionState? _pending;

    /// <summary>Reads the session file, or returns defaults if missing/unreadable.</summary>
    public SessionState Load()
    {
        try
        {
            if (File.Exists(filePath))
            {
                return JsonSerializer.Deserialize<SessionState>(File.ReadAllText(filePath), Options)
                       ?? new SessionState();
            }
        }
        catch
        {
            // A corrupt session file must never block startup - just start fresh.
        }

        return new SessionState();
    }

    /// <summary>Writes immediately and atomically.</summary>
    public void Save(SessionState state) => AtomicJson.Write(filePath, state, Options);

    /// <summary>Schedules a write ~<see cref="_debounce"/> after the last call; later calls win.</summary>
    public void SaveDebounced(SessionState state)
    {
        lock (_gate)
        {
            _pending = state;
            _timer ??= new Timer(_ => FlushPending());
            _timer.Change(_debounce, Timeout.InfiniteTimeSpan);
        }
    }

    /// <summary>Writes any pending debounced state right away.</summary>
    public void Flush()
    {
        SessionState? toWrite;
        lock (_gate)
        {
            toWrite = _pending;
            _pending = null;
        }
        if (toWrite is not null) Save(toWrite);
    }

    private void FlushPending() => Flush();

    public void Dispose()
    {
        Flush();
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
