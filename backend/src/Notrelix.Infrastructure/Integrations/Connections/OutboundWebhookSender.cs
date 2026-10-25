using System.Net;
using System.Net.Sockets;
using Notrelix.Application.Features.Integrations.Connections;

namespace Notrelix.Infrastructure.Integrations.Connections;

public enum WebhookSendOutcome
{
    /// <summary>The receiver answered 2xx.</summary>
    Delivered,

    /// <summary>The receiver or the network failed in a way another attempt may fix (5xx, 408, 429, timeout, refused).</summary>
    Transient,

    /// <summary>Another attempt cannot help (4xx, a refused address).</summary>
    Permanent,
}

public sealed record WebhookSendResult(WebhookSendOutcome Outcome, string Description);

/// <summary>
/// Posts one signed delivery. The address is checked again when the connection is made, against the
/// addresses the name resolved to (so a name that later points inward is refused), redirects are not
/// followed, and the answer's body is never read.
/// </summary>
public sealed class OutboundWebhookSender
{
    public const string HttpClientName = "integrations-outbound-webhook";

    private readonly IHttpClientFactory _factory;

    public OutboundWebhookSender(IHttpClientFactory factory) => _factory = factory;

    public async Task<WebhookSendResult> SendAsync(
        string url, string secret, Guid deliveryId, string eventType, string body, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var timestamp = now.ToUnixTimeSeconds().ToString();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("User-Agent", "Notrelix-Webhooks/1");
        request.Headers.TryAddWithoutValidation(WebhookSignature.DeliveryHeader, deliveryId.ToString());
        request.Headers.TryAddWithoutValidation(WebhookSignature.EventHeader, eventType);
        request.Headers.TryAddWithoutValidation(WebhookSignature.TimestampHeader, timestamp);
        request.Headers.TryAddWithoutValidation(
            WebhookSignature.SignatureHeader, WebhookSignature.Compute(secret, timestamp, body));

        try
        {
            using var client = _factory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var status = (int)response.StatusCode;
            if (response.IsSuccessStatusCode) return new(WebhookSendOutcome.Delivered, $"HTTP {status}");
            var transient = status >= 500 || status is 408 or 425 or 429;
            return new(transient ? WebhookSendOutcome.Transient : WebhookSendOutcome.Permanent, $"HTTP {status}");
        }
        catch (BlockedWebhookAddressException)
        {
            return new(WebhookSendOutcome.Permanent, "The address points to a network the webhook may not reach.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(WebhookSendOutcome.Transient, "The receiver did not answer in time.");
        }
        catch (HttpRequestException ex) when (ex.InnerException is BlockedWebhookAddressException)
        {
            return new(WebhookSendOutcome.Permanent, "The address points to a network the webhook may not reach.");
        }
        catch (HttpRequestException)
        {
            return new(WebhookSendOutcome.Transient, "The receiver could not be reached.");
        }
    }

    /// <summary>The connect step: resolves the host and refuses any address the policy does not allow.</summary>
    internal static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context, bool allowPrivate, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !WebhookAddressPolicy.IsAllowed(address, allowPrivate)))
        {
            throw new BlockedWebhookAddressException();
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}

public sealed class BlockedWebhookAddressException : Exception
{
    public BlockedWebhookAddressException() : base("The webhook address resolves to a network it may not reach.") { }
}
