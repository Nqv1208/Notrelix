using Notrelix.Application.Features.Integrations.N8n.Providers;
using Notrelix.Application.Features.Integrations.N8n.Configuration;
using Notrelix.Application.Features.Integrations.Public.Commands;

namespace Notrelix.Application.Features.Integrations.N8n.Services;

/// <summary>
/// Producer-owned implementation of the Integrations public webhook action.
/// Delegates to the Integrations provider port; the semantic outcome
/// classification is already normalized by the provider adapter.
/// </summary>
public sealed class N8nWebhookActions : IN8nWebhookActions
{
    private readonly IN8nClient _client;

    public N8nWebhookActions(IN8nClient client)
    {
        _client = client;
    }

    public Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken)
    {
        // Fail closed before the provider port: a path outside the allow-list
        // never reaches the provider adapter.
        if (!N8nWebhookPath.IsValid(webhookPath))
        {
            return Task.FromResult(new N8nWebhookDispatchResult(
                N8nWebhookOutcome.TerminalFailure,
                "n8n webhook path rejected: it is outside the allowed form."));
        }

        return _client.TriggerWebhookAsync(webhookPath, payload, cancellationToken);
    }
}
