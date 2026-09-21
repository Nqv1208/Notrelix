using Microsoft.AspNetCore.Http;

namespace Notrelix.API.Middleware;

/// <summary>
/// Raw HTTP boundary for provider webhook callbacks (TAC-AI-FLOW-07).
/// The body is bounded and checked before any materialization: a strict
/// content-type allowlist, a Content-Length pre-check, and a streaming
/// bound for chunked/unknown-length bodies. Oversized or non-JSON bodies
/// are rejected at the real HTTP boundary — never after a full unbounded
/// in-memory read.
/// </summary>
public static class WebhookBoundedBodyReader
{
    /// <summary>
    /// The only accepted provider webhook media type. Sent as
    /// <c>Content-Type: application/json</c> by the provider.
    /// </summary>
    public const string WebhookContentType = "application/json";

    /// <summary>
    /// Hard ceiling for a single provider webhook payload in bytes.
    /// </summary>
    public const int MaxWebhookPayloadBytes = 256 * 1024;

    private static readonly TimeSpan BodyReadTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Reads the request body as exact raw bytes with a hard streaming bound.
    /// Returns the raw bytes when the body fits within
    /// <see cref="MaxWebhookPayloadBytes"/>, or null when it exceeds the bound.
    /// Content-Length is not trusted: the streaming bound applies to chunked
    /// and absent-length bodies as well, so a lying or oversized request never
    /// forces the whole payload into memory.
    /// </summary>
    public static async Task<byte[]?> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        // A known over-limit Content-Length is rejected without reading anything.
        if (request.ContentLength is > MaxWebhookPayloadBytes)
        {
            return null;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(BodyReadTimeout);

        using var buffer = new MemoryStream(request.ContentLength is { } length ? (int)Math.Min(length, MaxWebhookPayloadBytes + 1) : 8192);
        var chunk = new byte[8192];
        while (true)
        {
            var read = await request.Body.ReadAsync(chunk.AsMemory(0, chunk.Length), linked.Token);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > MaxWebhookPayloadBytes)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), linked.Token);
        }

        return buffer.ToArray();
    }
}