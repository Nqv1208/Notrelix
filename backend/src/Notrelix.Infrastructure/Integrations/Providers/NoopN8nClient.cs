
using Notrelix.Application.Features.Integrations.N8n.Configuration;
using Notrelix.Application.Features.Integrations.Public.Commands;

namespace Notrelix.Infrastructure.Integrations.Providers;

public sealed class NoopN8nClient : IN8nClient
{
    private readonly ILogger<NoopN8nClient> _logger;

    public NoopN8nClient(ILogger<NoopN8nClient> logger)
    {
        _logger = logger;
    }

    public Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
        string webhookPath, string payload, CancellationToken cancellationToken = default)
    {
        if (!N8nWebhookPath.IsValid(webhookPath))
        {
            return Task.FromResult(new N8nWebhookDispatchResult(
                N8nWebhookOutcome.TerminalFailure,
                "n8n webhook path rejected: it is outside the allowed form."));
        }

        _logger.LogInformation("N8n dispatch skipped (integration disabled): {Path}", webhookPath);
        return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
    }
}
