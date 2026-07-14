using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.EmailVerificationDelivery;

public sealed class SendEmailVerificationEmailConsumer : IConsumer<EmailVerificationDeliveryRequestedIntegrationEventV1>
{
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly ILogger<SendEmailVerificationEmailConsumer> _logger;

    public SendEmailVerificationEmailConsumer(
        IEmailOutboxWriter emailOutboxWriter,
        ILogger<SendEmailVerificationEmailConsumer> logger)
    {
        _emailOutboxWriter = emailOutboxWriter;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<EmailVerificationDeliveryRequestedIntegrationEventV1> context)
    {
        var msg = context.Message;

        await _emailOutboxWriter.QueueTemplatedEmailAsync(
            new QueueTemplatedEmailRequest<EmailVerificationEmailPayload>(
                DeduplicationKey: $"email-verification:{msg.VerificationTokenId}",
                RecipientEmail: msg.Email,
                RecipientName: null,
                WorkspaceId: null,
                RecipientUserId: msg.UserId,
                SourceContext: "identity",
                TemplateKey: "email-verification",
                TemplateVersion: 1,
                Payload: new EmailVerificationEmailPayload(
                    msg.VerificationTokenId,
                    msg.UserId,
                    new ProtectedSecretEnvelope(msg.ProtectedToken),
                    msg.ExpiresAt),
                SourceEventId: msg.EventId,
                SourceMessageId: context.MessageId,
                SensitivePayloadExpiresAt: msg.ExpiresAt),
            context.CancellationToken);

        _logger.LogInformation(
            "Email verification delivery queued. UserId={UserId}",
            msg.UserId);
    }
}
