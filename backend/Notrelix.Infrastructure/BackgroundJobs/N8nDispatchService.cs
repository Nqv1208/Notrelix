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
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private readonly ApplicationDbContext _context;
    private readonly IN8nClient _n8nClient;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<N8nDispatchService> _logger;

    public N8nDispatchService(
        ApplicationDbContext context,
        IN8nClient n8nClient,
        IJobQueue jobQueue,
        ILogger<N8nDispatchService> logger)
    {
        _context = context;
        _n8nClient = n8nClient;
        _jobQueue = jobQueue;
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
            execution.MarkFailed("Automation rule is missing or disabled.");
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!N8nAutomationConfiguration.TryGetWebhookPath(rule.Configuration, out var webhookPath))
        {
            execution.MarkFailed("Automation rule is missing configuration.webhookPath.");
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var result = await _n8nClient.TriggerWebhookAsync(webhookPath, execution.Payload, cancellationToken);

            if (result.Succeeded)
            {
                execution.MarkDelivered(result.Response);
            }
            else
            {
                await RetryOrFailAsync(job, execution, $"n8n returned HTTP {result.StatusCode}: {result.Error}", cancellationToken);
                return;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            await RetryOrFailAsync(job, execution, ex.Message, cancellationToken);
            return;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RetryOrFailAsync(
        N8nDispatchJob job,
        AutomationExecution execution,
        string error,
        CancellationToken cancellationToken)
    {
        if (execution.AttemptCount + 1 >= MaxAttempts)
        {
            execution.MarkFailed(error);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        execution.MarkRetried(error);
        await _context.SaveChangesAsync(cancellationToken);
        await _jobQueue.EnqueueAsync(job, RetryDelay, cancellationToken);
    }
}
