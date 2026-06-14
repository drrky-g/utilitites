using System.Net;
using System.Text;

namespace Utilities.Auth.Tests;

/// <summary>
/// Test <see cref="HttpMessageHandler"/> that records outgoing requests (and their bodies) and returns a
/// canned response, so the OAuth providers can be tested without real network calls.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

    /// <summary>The requests received, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>The request bodies received, in order (null when the request had no content).</summary>
    public List<string?> RequestBodies { get; } = new();

    /// <summary>Creates a handler that always returns the given status and JSON body.</summary>
    public static StubHttpMessageHandler Json(HttpStatusCode status, string json) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken));
        return _responder(request);
    }
}
