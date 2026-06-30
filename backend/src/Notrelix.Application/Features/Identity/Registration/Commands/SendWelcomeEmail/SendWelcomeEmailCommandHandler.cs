using System.Net;
using System.Threading.Tasks;
using MediatR;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Application.Features.Identity.Registration.Commands.SendWelcomeEmail;

public sealed class SendWelcomeEmailCommandHandler : IRequestHandler<SendWelcomeEmailCommand, SendWelcomeEmailResult>
{
    private readonly IMessageDeduplicationStore _deduplicationStore;
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendWelcomeEmailCommandHandler(
        IMessageDeduplicationStore deduplicationStore,
        IEmailOutboxWriter emailOutboxWriter,
        IDateTimeProvider dateTimeProvider)
    {
        _deduplicationStore = deduplicationStore;
        _emailOutboxWriter = emailOutboxWriter;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<SendWelcomeEmailResult> Handle(
        SendWelcomeEmailCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        if (await _deduplicationStore.IsProcessedAsync(
            request.MessageId,
            request.ConsumerName,
            cancellationToken))
        {
            return new SendWelcomeEmailResult(
                UserId: request.UserId,
                Email: request.Email,
                AlreadySent: true);
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.Email
            : request.DisplayName.Trim();

        var safeDisplayName = WebUtility.HtmlEncode(displayName);

        var subject = "Welcome to Notrelix!";

        var htmlBody = $"""
            <p>Hi {safeDisplayName},</p>
            <p>Welcome to Notrelix! We're excited to have you on board.</p>
            <p>Get started by exploring our features and creating your first project.</p>
            <p>Best regards,<br/>The Notrelix Team</p>
            """;

        await _emailOutboxWriter.QueueEmailAsync(
            recipientEmail: request.Email,
            recipientName: displayName,
            subject: subject,
            htmlBody: htmlBody,
            workspaceId: request.WorkspaceId,
            recipientUserId: request.UserId,
            sourceContext: "identity",
            templateName: "welcome-email",
            templateVersion: 1,
            deduplicationKey: $"welcome-email:{request.UserId}",
            sourceEventId: request.SourceEventId,
            sourceMessageId: request.MessageId,
            cancellationToken: cancellationToken);

        _deduplicationStore.MarkProcessed(
            request.MessageId,
            request.ConsumerName,
            request.SourceMessageName,
            request.SourceMessageVersion,
            request.SourceEventId,
            request.WorkspaceId,
            now);

        return new SendWelcomeEmailResult(
            UserId: request.UserId,
            Email: request.Email,
            AlreadySent: false);
    }
}