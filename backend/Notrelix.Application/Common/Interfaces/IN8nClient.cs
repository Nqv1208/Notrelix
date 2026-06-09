namespace Notrelix.Application.Common.Interfaces;

public interface IN8nClient
{
    Task<N8nTriggerResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken = default);
}

public sealed record N8nTriggerResult(bool Succeeded, int StatusCode, string? Response, string? Error);
