using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationNotice;

[OrderingNotApplicable("Queues one notice per registration attempt through a per-event deduplication key; the notices are independent and nothing supersedes an earlier one.")]
public sealed class SendRegistrationNoticeEmailConsumer
    : IConsumer<RegistrationAttemptedOnExistingAccountIntegrationEventV1>
{
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly ILogger<SendRegistrationNoticeEmailConsumer> _logger;

    public SendRegistrationNoticeEmailConsumer(
        IEmailOutboxWriter emailOutboxWriter,
        ILogger<SendRegistrationNoticeEmailConsumer> logger)
    {
        _emailOutboxWriter = emailOutboxWriter;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<RegistrationAttemptedOnExistingAccountIntegrationEventV1> context)
    {
        var msg = context.Message;

        await _emailOutboxWriter.QueueRenderedEmailAsync(
            new QueueRenderedEmailRequest(
                DeduplicationKey: $"registration-notice:{msg.EventId}",
                RecipientEmail: msg.Email,
                RecipientName: null,
                Subject: "Someone tried to register with your email — Notrelix",
                BodyHtml: EmailTemplateService.RegistrationAttemptOnExistingAccount(),
                BodyText: null,
                WorkspaceId: null,
                RecipientUserId: null,
                SourceContext: "identity",
                TemplateKey: "registration-attempt-notice",
                TemplateVersion: 1,
                SourceEventId: msg.EventId,
                SourceMessageId: context.MessageId),
            context.CancellationToken);

        // The address is a delivery target, not a log field.
        _logger.LogInformation("Registration notice queued. EventId={EventId}", msg.EventId);
    }
}
