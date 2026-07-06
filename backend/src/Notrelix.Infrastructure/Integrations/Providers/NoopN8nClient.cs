
namespace Notrelix.Infrastructure.Integrations.Providers;

public sealed class NoopN8nClient : IN8nClient
{
    private readonly ILogger<NoopN8nClient> _logger;

    public NoopN8nClient(ILogger<NoopN8nClient> logger)
    {
        _logger = logger;
    }

    public Task<N8nTriggerResult> TriggerWebhookAsync(
        string webhookPath, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("N8n dispatch skipped (integration disabled): {Path}", webhookPath);
        return Task.FromResult(new N8nTriggerResult(true, 204, null, null));
    }
}
