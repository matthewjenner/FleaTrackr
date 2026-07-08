using System.Net;

namespace FleaTrackr.App.Tests;

/// <summary>
/// A test HttpMessageHandler that returns a queued sequence of canned responses and records each
/// request body. Lets us exercise <c>TarkovApiClient</c> - GraphQL wiring, JSON mapping, and the
/// 429 retry path - with no network.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    /// <summary>Bodies of every request received, in order (for asserting the query + variables).</summary>
    public List<string> RequestBodies { get; } = [];

    public StubHttpMessageHandler Enqueue(HttpStatusCode status, string body)
    {
        _responses.Enqueue((status, body));
        return this;
    }

    /// <summary>Convenience: enqueue a 200 OK with a GraphQL <c>{"data": ...}</c> envelope.</summary>
    public StubHttpMessageHandler EnqueueData(string dataJson) =>
        Enqueue(HttpStatusCode.OK, $"{{\"data\":{dataJson}}}");

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestBodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));

        (HttpStatusCode status, string body) = _responses.Count > 0
            ? _responses.Dequeue()
            : (HttpStatusCode.OK, "{\"data\":{}}");

        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }
}
