namespace Notrelix.Application.Common.Integrations.N8n;

public interface IN8nClient
{
    Task<N8nTriggerResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken = default);
}

public sealed record N8nTriggerResult(bool Succeeded, int StatusCode, string? Response, string? Error);
