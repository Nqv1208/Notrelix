using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;

namespace Notrelix.Infrastructure.Services;

public sealed class N8nClient : IN8nClient
{
    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;

    public N8nClient(HttpClient httpClient, IOptions<N8nOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<N8nTriggerResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = webhookPath.Trim().TrimStart('/');
        var basePath = _options.WebhookBasePath.TrimEnd('/');
        var requestUri = $"{basePath}/{normalizedPath}";

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return new N8nTriggerResult(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            response.IsSuccessStatusCode ? responseBody : null,
            response.IsSuccessStatusCode ? null : responseBody);
    }
}
