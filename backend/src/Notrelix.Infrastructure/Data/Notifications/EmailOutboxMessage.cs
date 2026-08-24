using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

public enum EmailContentMode
{
    Rendered = 0,
    Templated = 1,
    Purged = 2
}

public sealed class EmailOutboxMessage
{
    public Guid Id { get; private set; }
    public string DeduplicationKey { get; private set; } = null!;
    public string? SourceContext { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public Guid? SourceMessageId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? RecipientUserId { get; private set; }
    public string RecipientEmail { get; private set; } = null!;
    public string? RecipientName { get; private set; }
    public EmailContentMode ContentMode { get; private set; }
    public string TemplateName { get; private set; } = null!;
    public int TemplateVersion { get; private set; } = 1;
    public string? Subject { get; private set; }
    public string? BodyHtml { get; private set; }
    public string? BodyText { get; private set; }
    public JsonDocument? TemplateDataJson { get; private set; }
    public JsonDocument HeadersJson { get; private set; } = JsonDocument.Parse("{}");
    public int Priority { get; private set; } = 100;
    public string Status { get; private set; } = "Pending";
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 5;
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public string? LockedBy { get; private set; }
    public string? LockToken { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? SensitivePayloadExpiresAt { get; private set; }
    public DateTimeOffset? SensitivePayloadClearedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private EmailOutboxMessage()
    {
    }

    public static EmailOutboxMessage CreateRendered(
        QueueRenderedEmailRequest request,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DeduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RecipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TemplateKey);
        if (string.IsNullOrWhiteSpace(request.BodyHtml)
            && string.IsNullOrWhiteSpace(request.BodyText))
        {
            throw new ArgumentException(
                "Rendered email must have HTML or text body.",
                nameof(request));
        }

        return new EmailOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            DeduplicationKey = request.DeduplicationKey,
            SourceContext = request.SourceContext,
            SourceEventId = request.SourceEventId,
            SourceMessageId = request.SourceMessageId,
            WorkspaceId = request.WorkspaceId,
            RecipientUserId = request.RecipientUserId,
            RecipientEmail = request.RecipientEmail,
            RecipientName = request.RecipientName,
            ContentMode = EmailContentMode.Rendered,
            TemplateName = request.TemplateKey,
            TemplateVersion = request.TemplateVersion,
            Subject = request.Subject,
            BodyHtml = request.BodyHtml,
            BodyText = request.BodyText,
            Priority = request.Priority,
            NextAttemptAt = createdAt,
            CreatedAt = createdAt,
            SensitivePayloadExpiresAt = request.SensitivePayloadExpiresAt
        };
    }

    public static EmailOutboxMessage CreateTemplated<TPayload>(
        QueueTemplatedEmailRequest<TPayload> request,
        DateTimeOffset createdAt)
        where TPayload : IEmailTemplatePayload
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DeduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RecipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TemplateKey);
        ArgumentNullException.ThrowIfNull(request.Payload);

        return new EmailOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            DeduplicationKey = request.DeduplicationKey,
            SourceContext = request.SourceContext,
            SourceEventId = request.SourceEventId,
            SourceMessageId = request.SourceMessageId,
            WorkspaceId = request.WorkspaceId,
            RecipientUserId = request.RecipientUserId,
            RecipientEmail = request.RecipientEmail,
            RecipientName = request.RecipientName,
            ContentMode = EmailContentMode.Templated,
            TemplateName = request.TemplateKey,
            TemplateVersion = request.TemplateVersion,
            TemplateDataJson = JsonSerializer.SerializeToDocument(request.Payload),
            Priority = request.Priority,
            NextAttemptAt = createdAt,
            CreatedAt = createdAt,
            SensitivePayloadExpiresAt = request.SensitivePayloadExpiresAt
        };
    }

    public void MarkProcessing(
        string dispatcherId,
        string lockToken,
        DateTimeOffset now,
        int timeoutSeconds)
    {
        Status = "Sending";
        ProcessingStartedAt = now;
        LockedBy = dispatcherId;
        LockToken = lockToken;
        LockedUntil = now.AddSeconds(timeoutSeconds);
        UpdatedAt = now;
    }

    public void MarkSent(
        string provider,
        string? providerMessageId,
        DateTimeOffset sentAt,
        string lockToken)
    {
        EnsureLock(lockToken);
        Status = "Sent";
        SentAt = sentAt;
        Provider = provider;
        ProviderMessageId = providerMessageId;
        ProcessingStartedAt = null;
        LockedBy = null;
        LockToken = null;
        LockedUntil = null;
        UpdatedAt = sentAt;
        ClearSensitivePayload(sentAt);
    }

    /// <summary>
    /// Records a delivery failure and increments retry count.
    /// Does NOT change Status — callers must use <see cref="ScheduleRetry"/> or <see cref="MarkDeadLetter"/>
    /// to transition to the appropriate terminal/retry state.
    /// </summary>
    public void MarkFailed(
        string errorCode,
        string errorMessage,
        DateTimeOffset now,
        string lockToken)
    {
        EnsureLock(lockToken);
        RetryCount++;
        LastErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ProcessingStartedAt = null;
        LockedBy = null;
        LockToken = null;
        LockedUntil = null;
        UpdatedAt = now;
    }

    public void MarkDeadLetter(DateTimeOffset now)
    {
        // Terminal dead-letter state: Status stays 'Failed' with an exhausted
        // retry budget (retry_count >= max_retries) — no separate status exists.
        Status = "Failed";
        RetryCount = Math.Max(RetryCount, MaxRetries);
        UpdatedAt = now;
        ClearSensitivePayload(now);
    }

    public void MarkCancelled(string reason, DateTimeOffset now)
    {
        Status = "Cancelled";
        LastErrorCode = "stale_payload";
        ErrorMessage = reason;
        ProcessingStartedAt = null;
        LockedBy = null;
        LockToken = null;
        LockedUntil = null;
        UpdatedAt = now;
        ClearSensitivePayload(now);
    }

    public void ScheduleRetry(DateTimeOffset nextAttemptAt, DateTimeOffset now)
    {
        Status = "Failed";
        NextAttemptAt = nextAttemptAt;
        UpdatedAt = now;
    }

    public void ClearSensitivePayload(DateTimeOffset clearedAt)
    {
        if (TemplateDataJson is null)
            return;

        ContentMode = EmailContentMode.Purged;
        TemplateDataJson = null;
        SensitivePayloadClearedAt ??= clearedAt;
        UpdatedAt = clearedAt;
    }

    private void EnsureLock(string lockToken)
    {
        if (Status != "Sending"
            || !string.Equals(LockToken, lockToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email outbox lease is no longer valid.");
        }
    }
}
