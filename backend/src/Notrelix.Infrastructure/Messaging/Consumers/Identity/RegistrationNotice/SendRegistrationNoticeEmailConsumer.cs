using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationNotice;

[OrderingNotApplicable("Queues one notice per registration attempt through a per-event deduplication key; the notices are independent and nothing supersedes an earlier one.")]
public sealed class SendRegistrationNoticeEmailConsumer
    : IConsumer<RegistrationAttemptedOnExistingAccountIntegrationEventV1>
{
    // The plain-text part: the development backend logs it, and mail clients without HTML show it.
    private const string NoticeText =
        "Someone tried to create a Notrelix account with this email address, but an account already exists. "
        + "Nothing was changed. If it was you, sign in with your existing account, or choose \"Forgot password\" on the sign-in page. "
        + "If it was not you, you can ignore this email.";

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
                BodyText: NoticeText,
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
