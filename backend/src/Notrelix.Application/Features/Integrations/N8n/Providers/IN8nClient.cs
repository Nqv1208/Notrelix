using Notrelix.Application.Features.Integrations.Public.Commands;

namespace Notrelix.Application.Features.Integrations.N8n.Providers;

/// <summary>
/// Integrations-owned provider port for the n8n HTTP webhook API. Transport
/// and provider protocol mechanics live behind this port in Infrastructure;
/// the result is provider-semantic (no transport types).
/// </summary>
public interface IN8nClient
{
    Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken = default);
}
