using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.BackgroundJobs;

internal sealed class EmailDispatcher : BackgroundService
{
    private const int BatchSize = 10;
    private const int PollIntervalMs = 10_000;
    private const int ProcessingTimeoutSeconds = 120;
    private const string DispatcherId = "EmailDispatcher";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailDispatcher> _logger;

    public EmailDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailDispatcher batch failed");
            }

            await Task.Delay(PollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var processingCutoff = now.AddSeconds(-ProcessingTimeoutSeconds);

        var messages = await context.EmailOutboxMessages
            .FromSqlRaw("""
                SELECT * FROM notifications.email_outbox
                WHERE (
                    ("Status" = 'Pending' AND "NextAttemptAt" <= {0})
                    OR
                    ("Status" = 'Sending' AND "ProcessingStartedAt" <= {1})
                    OR
                    ("Status" = 'Failed' AND "NextAttemptAt" <= {0})
                )
                ORDER BY "Priority", "CreatedAt"
                LIMIT {2}
                FOR UPDATE SKIP LOCKED
                """,
                now, processingCutoff, BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("EmailDispatcher claimed {Count} messages", messages.Count);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, emailService, now, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        EmailOutboxMessage message,
        IEmailService emailService,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var attemptNo = message.RetryCount + 1;

        message.MarkProcessing(DispatcherId, now, ProcessingTimeoutSeconds);

        var attempt = new EmailDeliveryAttempt(
            message.Id,
            attemptNo,
            provider: null,
            providerMessageId: null,
            status: "InProgress",
            startedAt: now);

        try
        {
            await emailService.SendAsync(
                message.RecipientEmail,
                message.Subject,
                message.BodyHtml ?? message.BodyText ?? string.Empty,
                cancellationToken);

            var completedAt = DateTimeOffset.UtcNow;
            attempt.MarkSent(providerMessageId: null, completedAt);
            message.MarkSent("default", completedAt);

            _logger.LogInformation("Email sent to {Email} for message {MessageId}", message.RecipientEmail, message.Id);
        }
        catch (Exception ex)
        {
            var failedAt = DateTimeOffset.UtcNow;
            attempt.MarkFailed(ex.GetType().Name, ex.Message, failedAt);
            message.MarkFailed(ex.GetType().Name, ex.Message, failedAt);

            if (message.RetryCount >= message.MaxRetries)
            {
                message.MarkDeadLetter();
                _logger.LogWarning("Email dead-lettered after {RetryCount} attempts for message {MessageId}: {Error}",
                    message.RetryCount, message.Id, ex.Message);
            }
            else
            {
                var backoffSeconds = Math.Min(Math.Pow(2, message.RetryCount), 60);
                message.ScheduleRetry(failedAt.AddSeconds(backoffSeconds));
                _logger.LogWarning("Email attempt {AttemptNo} failed for message {MessageId}: {Error}. Retry at {NextAttempt}",
                    attemptNo, message.Id, ex.Message, message.NextAttemptAt);
            }
        }
    }
}
