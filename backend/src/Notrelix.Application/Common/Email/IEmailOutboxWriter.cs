namespace Notrelix.Application.Common.Email;

public interface IEmailOutboxWriter
{
    ValueTask QueueEmailAsync(
        string recipientEmail,
        string? recipientName,
        string subject,
        string htmlBody,
        Guid? workspaceId,
        Guid? recipientUserId,
        string sourceContext,
        string templateName,
        int templateVersion = 1,
        int priority = 100,
        string? deduplicationKey = null,
        Guid? sourceEventId = null,
        Guid? sourceMessageId = null,
        CancellationToken cancellationToken = default);
}
