using System.Text.Json;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Notifications;

namespace Notrelix.Infrastructure.Email;

internal sealed class EmailOutboxWriter : IEmailOutboxWriter
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EmailOutboxWriter(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public ValueTask QueueEmailAsync(
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
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        var message = new EmailOutboxMessage(
            deduplicationKey: deduplicationKey ?? $"{sourceContext}:{recipientEmail}:{templateName}:{now:yyyyMMddHHmmss}",
            sourceContext: sourceContext,
            sourceEventId: sourceEventId,
            sourceMessageId: sourceMessageId,
            workspaceId: workspaceId,
            recipientUserId: recipientUserId,
            recipientEmail: recipientEmail,
            recipientName: recipientName,
            templateName: templateName,
            templateVersion: templateVersion,
            subject: subject,
            bodyHtml: htmlBody,
            bodyText: null,
            templateDataJson: null,
            headersJson: null,
            priority: priority,
            createdAt: now);

        _context.EmailOutboxMessages.Add(message);
        return ValueTask.CompletedTask;
    }
}
