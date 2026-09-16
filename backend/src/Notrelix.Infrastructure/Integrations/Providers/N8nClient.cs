using System.Net.Http.Headers;
using System.Net.Sockets;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Integrations.Providers;

/// <summary>
/// Infrastructure n8n webhook adapter. Implements the Integrations provider
/// port and translates transport results/exceptions into the provider-semantic
/// outcome classification — business callers never see transport types.
/// </summary>
public sealed class N8nClient : IN8nClient
{
    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;

    public N8nClient(HttpClient httpClient, IOptions<N8nOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = webhookPath.Trim().TrimStart('/');
        var basePath = _options.WebhookBasePath.TrimEnd('/');
        var requestUri = $"{basePath}/{normalizedPath}";

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        try
        {
            using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
                return new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null);

            var error = $"n8n returned HTTP {(int)response.StatusCode}: {responseBody}";
            return new N8nWebhookDispatchResult(ClassifyHttpFailure((int)response.StatusCode), error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            // Network timeout / provider did not respond: indeterminate whether
            // the provider processed the call. The durable delivery mechanism
            // must NOT auto re-fire; reconciliation is required.
            return new N8nWebhookDispatchResult(
                N8nWebhookOutcome.UnknownOutcome,
                "n8n webhook call timed out before a response arrived.");
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException se
            && se.SocketErrorCode is SocketError.ConnectionRefused
                or SocketError.ConnectionAborted
                or SocketError.HostNotFound)
        {
            // Unambiguous connection-phase failure: the request never reached
            // the provider. Safe to retry.
            return new N8nWebhookDispatchResult(
                N8nWebhookOutcome.RetryableFailure,
                $"n8n webhook call failed: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            // Ambiguous failure: post-send / connection-reset / unknown HTTP
            // error. Indeterminate whether the provider processed the call.
            return new N8nWebhookDispatchResult(
                N8nWebhookOutcome.UnknownOutcome,
                $"n8n webhook call failed (indeterminate): {ex.Message}");
        }
    }

    private static N8nWebhookOutcome ClassifyHttpFailure(int statusCode) =>
        statusCode switch
        {
            // 429 rate-limit: retryable ONLY when the provider/gateway contract
            // guarantees rejection before execution. Without such a guarantee
            // (the common case), treat as unknown.
            429 => N8nWebhookOutcome.RetryableFailure,
            // 408 request timeout / 5xx provider failure: the provider may or
            // may not have processed the call. Indeterminate.
            408 or >= 500 => N8nWebhookOutcome.UnknownOutcome,
            _ => N8nWebhookOutcome.TerminalFailure,
        };
}
