using System.Net.Http.Headers;
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
        catch (TaskCanceledException)
        {
            // A timeout is not proof the provider did not process the call.
            return new N8nWebhookDispatchResult(
                N8nWebhookOutcome.UnknownOutcome,
                "n8n webhook call timed out before a response arrived.");
        }
        catch (HttpRequestException ex)
        {
            return new N8nWebhookDispatchResult(
                N8nWebhookOutcome.RetryableFailure,
                $"n8n webhook call failed: {ex.Message}");
        }
    }

    private static N8nWebhookOutcome ClassifyHttpFailure(int statusCode) =>
        statusCode switch
        {
            // Request timeout / too many requests / provider-side failures.
            408 or 429 or >= 500 => N8nWebhookOutcome.RetryableFailure,
            _ => N8nWebhookOutcome.TerminalFailure,
        };
}
