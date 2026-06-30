using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

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
    public string TemplateName { get; private set; } = null!;
    public int TemplateVersion { get; private set; } = 1;
    public string Subject { get; private set; } = null!;
    public string? BodyHtml { get; private set; }
    public string? BodyText { get; private set; }
    public JsonDocument TemplateDataJson { get; private set; } = JsonDocument.Parse("{}");
    public JsonDocument HeadersJson { get; private set; } = JsonDocument.Parse("{}");
    public int Priority { get; private set; } = 100;
    public string Status { get; private set; } = "Pending";
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 5;
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private EmailOutboxMessage() { }

    public void MarkProcessing(string dispatcherId, DateTimeOffset now, int timeoutSeconds)
    {
        Status = "Sending";
        ProcessingStartedAt = now;
        LockedBy = dispatcherId;
        LockedUntil = now.AddSeconds(timeoutSeconds);
    }

    public void MarkSent(string provider, DateTimeOffset sentAt)
    {
        Status = "Sent";
        SentAt = sentAt;
        Provider = provider;
        ProcessingStartedAt = null;
        LockedBy = null;
        LockedUntil = null;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset now)
    {
        RetryCount++;
        LastErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ProcessingStartedAt = null;
        LockedBy = null;
        LockedUntil = null;
    }

    public void MarkDeadLetter()
    {
        Status = "DeadLetter";
    }

    public void ScheduleRetry(DateTimeOffset nextAttemptAt)
    {
        Status = "Failed";
        NextAttemptAt = nextAttemptAt;
    }

    public EmailOutboxMessage(
        string deduplicationKey,
        string? sourceContext,
        Guid? sourceEventId,
        Guid? sourceMessageId,
        Guid? workspaceId,
        Guid? recipientUserId,
        string recipientEmail,
        string? recipientName,
        string templateName,
        int templateVersion,
        string subject,
        string? bodyHtml,
        string? bodyText,
        JsonDocument? templateDataJson,
        JsonDocument? headersJson,
        int priority,
        DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        DeduplicationKey = deduplicationKey;
        SourceContext = sourceContext;
        SourceEventId = sourceEventId;
        SourceMessageId = sourceMessageId;
        WorkspaceId = workspaceId;
        RecipientUserId = recipientUserId;
        RecipientEmail = recipientEmail;
        RecipientName = recipientName;
        TemplateName = templateName;
        TemplateVersion = templateVersion;
        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        TemplateDataJson = templateDataJson ?? JsonDocument.Parse("{}");
        HeadersJson = headersJson ?? JsonDocument.Parse("{}");
        Priority = priority;
        NextAttemptAt = createdAt;
        CreatedAt = createdAt;
        Status = "Pending";
    }
}
