using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Automation;
using Notrelix.Application.Features.Automation.Jobs;
using Notrelix.Application.Features.Integrations;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.BackgroundJobs;

public sealed class N8nDispatchService
{
    private readonly ApplicationDbContext _context;
    private readonly IN8nClient _n8nClient;
    private readonly ILogger<N8nDispatchService> _logger;

    public N8nDispatchService(
        ApplicationDbContext context,
        IN8nClient n8nClient,
        ILogger<N8nDispatchService> logger)
    {
        _context = context;
        _n8nClient = n8nClient;
        _logger = logger;
    }

    public async Task DispatchAsync(N8nDispatchJob job, CancellationToken cancellationToken = default)
    {
        var execution = await _context.AutomationExecutions
            .FirstOrDefaultAsync(x => x.Id == job.ExecutionId, cancellationToken);

        if (execution is null)
        {
            _logger.LogWarning("Skipping n8n dispatch because execution {ExecutionId} was not found", job.ExecutionId);
            return;
        }

        var rule = await _context.AutomationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == job.AutomationRuleId, cancellationToken);

        if (rule is null || !rule.IsEnabled)
        {
            execution.Fail("Automation rule is missing or disabled.", DateTimeOffset.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!TryGetWebhookPath(rule.Configuration, out var webhookPath))
        {
            execution.Fail("Automation rule is missing configuration.webhookPath.", DateTimeOffset.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var result = await _n8nClient.TriggerWebhookAsync(webhookPath, execution.Payload, cancellationToken);

            if (result.Succeeded)
            {
                execution.Succeed(DateTimeOffset.UtcNow);
            }
            else
            {
                execution.Fail($"n8n returned HTTP {result.StatusCode}: {result.Error}", DateTimeOffset.UtcNow);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            execution.Fail(ex.Message, DateTimeOffset.UtcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static bool TryGetWebhookPath(Domain.Automation.RulesEngine.AutomationConfiguration config, out string webhookPath)
    {
        if (config.Action.Type != "Webhook" || string.IsNullOrWhiteSpace(config.Action.Configuration))
        {
            webhookPath = string.Empty;
            return false;
        }

        return N8nAutomationConfiguration.TryGetWebhookPath(config.Action.Configuration, out webhookPath);
    }
}
