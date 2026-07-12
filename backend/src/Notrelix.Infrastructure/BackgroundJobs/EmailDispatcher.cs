using System.Text.Json;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Notifications.Email;

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
        await CancelExpiredSensitivePayloadsAsync(cancellationToken);
        var claims = await ClaimBatchAsync(cancellationToken);
        foreach (var claim in claims)
        {
            await ProcessClaimAsync(claim, cancellationToken);
        }
    }

    private async Task CancelExpiredSensitivePayloadsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var now = clock.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var expiredMessages = await context.EmailOutboxMessages
            .Where(x => x.TemplateDataJson != null
                && x.SensitivePayloadExpiresAt != null
                && x.SensitivePayloadExpiresAt <= now
                && (x.Status == "Pending"
                    || x.Status == "Failed"
                    || (x.Status == "Sending" && x.LockedUntil <= now)))
            .OrderBy(x => x.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in expiredMessages)
        {
            message.MarkCancelled("sensitive-payload-expired", now);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<EmailClaim>> ClaimBatchAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var now = clock.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var messages = await context.EmailOutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM notifications.email_outbox
                WHERE (
                    (status IN ('Pending', 'Failed') AND next_attempt_at <= {now})
                    OR (status = 'Sending' AND locked_until <= {now})
                )
                ORDER BY priority, created_at
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var claims = new List<EmailClaim>(messages.Count);
        foreach (var message in messages)
        {
            var lockToken = Guid.NewGuid().ToString("N");
            var attemptNo = message.RetryCount + 1;
            var attempt = await context.EmailDeliveryAttempts
                .FirstOrDefaultAsync(
                    x => x.EmailOutboxId == message.Id && x.AttemptNo == attemptNo,
                    cancellationToken);

            if (attempt is null)
            {
                attempt = new EmailDeliveryAttempt(
                    message.Id,
                    attemptNo,
                    provider: null,
                    providerMessageId: null,
                    status: "InProgress",
                    startedAt: now);
                context.EmailDeliveryAttempts.Add(attempt);
            }
            else
            {
                attempt.Restart(now);
            }

            message.MarkProcessing(
                DispatcherId,
                lockToken,
                now,
                ProcessingTimeoutSeconds);

            claims.Add(new EmailClaim(message.Id, lockToken, attempt.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claims;
    }

    private async Task ProcessClaimAsync(
        EmailClaim claim,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var materializerRegistry = services.GetRequiredService<IEmailTemplateMaterializerRegistry>();
        var emailService = services.GetRequiredService<IEmailService>();
        var clock = services.GetRequiredService<IDateTimeProvider>();

        var message = await context.EmailOutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == claim.MessageId
                    && x.Status == "Sending"
                    && x.LockToken == claim.LockToken,
                cancellationToken);
        if (message is null)
            return;

        RenderedEmail? rendered;
        try
        {
            rendered = await MaterializeAsync(
                message,
                materializerRegistry,
                cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or CryptographicException)
        {
            await CancelClaimAsync(claim, "template-materialization-failed", cancellationToken);
            _logger.LogWarning(
                "Email message {MessageId} was cancelled because its template could not be materialized.",
                claim.MessageId);
            return;
        }

        if (rendered is null)
        {
            await CancelClaimAsync(claim, "template-payload-is-stale", cancellationToken);
            _logger.LogDebug("Email message {MessageId} was cancelled because its payload is stale.", claim.MessageId);
            return;
        }

        EmailDeliveryResult delivery;
        try
        {
            delivery = await emailService.SendAsync(
                new EmailDeliveryRequest(
                    message.RecipientEmail,
                    message.RecipientName,
                    rendered.Subject,
                    rendered.BodyHtml,
                    rendered.BodyText,
                    message.Id.ToString("N")),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await FinalizeFailureAsync(claim, ex, clock.UtcNow, cancellationToken);
            return;
        }

        await FinalizeSuccessAsync(
            claim,
            delivery,
            clock.UtcNow,
            cancellationToken);
    }

    private static Task<RenderedEmail?> MaterializeAsync(
        EmailOutboxMessage message,
        IEmailTemplateMaterializerRegistry registry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message.ContentMode == EmailContentMode.Rendered)
        {
            return Task.FromResult<RenderedEmail?>(
                message.Subject is null
                    || (message.BodyHtml is null && message.BodyText is null)
                    ? null
                    : new RenderedEmail(
                        message.Subject,
                        message.BodyHtml ?? message.BodyText!,
                        message.BodyText));
        }

        var materializer = registry.Find(message.TemplateName, message.TemplateVersion);
        return materializer is null
            ? Task.FromResult<RenderedEmail?>(null)
            : materializer.MaterializeAsync(message, cancellationToken);
    }

    private async Task CancelClaimAsync(
        EmailClaim claim,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var message = await context.EmailOutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == claim.MessageId
                    && x.Status == "Sending"
                    && x.LockToken == claim.LockToken,
                cancellationToken);
        if (message is null)
            return;

        message.MarkCancelled(reason, clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task FinalizeSuccessAsync(
        EmailClaim claim,
        EmailDeliveryResult delivery,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var message = await context.EmailOutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == claim.MessageId
                    && x.Status == "Sending"
                    && x.LockToken == claim.LockToken,
                cancellationToken);
        if (message is null)
            return;

        var attempt = await context.EmailDeliveryAttempts
            .FirstOrDefaultAsync(x => x.Id == claim.AttemptId, cancellationToken);
        if (attempt is null)
            return;

        attempt.MarkSent(delivery.ProviderMessageId, completedAt);
        message.MarkSent(delivery.Provider, delivery.ProviderMessageId, completedAt, claim.LockToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Email {MessageId} sent via {Provider} (providerMsgId={ProviderMessageId})",
            claim.MessageId, delivery.Provider, delivery.ProviderMessageId);
    }

    private async Task FinalizeFailureAsync(
        EmailClaim claim,
        Exception exception,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var message = await context.EmailOutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == claim.MessageId
                    && x.Status == "Sending"
                    && x.LockToken == claim.LockToken,
                cancellationToken);
        if (message is null)
            return;

        var attempt = await context.EmailDeliveryAttempts
            .FirstOrDefaultAsync(x => x.Id == claim.AttemptId, cancellationToken);
        if (attempt is null)
            return;

        var errorCode = exception.GetType().Name;
        var errorMessage = exception.Message;
        attempt.MarkFailed(errorCode, errorMessage, failedAt);
        message.MarkFailed(errorCode, errorMessage, failedAt, claim.LockToken);

        if (message.RetryCount >= message.MaxRetries)
        {
            message.MarkDeadLetter(failedAt);
            _logger.LogWarning(
                "Email {MessageId} dead-lettered after {RetryCount} retries: {ErrorCode}",
                claim.MessageId, message.RetryCount, errorCode);
        }
        else
        {
            var backoffSeconds = Math.Min(Math.Pow(2, message.RetryCount), 60);
            message.ScheduleRetry(failedAt.AddSeconds(backoffSeconds), failedAt);
            _logger.LogInformation(
                "Email {MessageId} scheduled retry {RetryCount} in {BackoffSeconds}s: {ErrorCode}",
                claim.MessageId, message.RetryCount, backoffSeconds, errorCode);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private sealed record EmailClaim(Guid MessageId, string LockToken, Guid AttemptId);
}
