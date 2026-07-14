using System.Net;
using Notrelix.Application.Events.Identity;

namespace Notrelix.Infrastructure.Messaging.Consumers.Identity.RegistrationCompleted;

public sealed class SendWelcomeEmailConsumer : IConsumer<IdentityRegistrationCompletedIntegrationEventV1>
{
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly ILogger<SendWelcomeEmailConsumer> _logger;

    public SendWelcomeEmailConsumer(
        IEmailOutboxWriter emailOutboxWriter,
        ILogger<SendWelcomeEmailConsumer> logger)
    {
        _emailOutboxWriter = emailOutboxWriter;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<IdentityRegistrationCompletedIntegrationEventV1> context)
    {
        var msg = context.Message;

        var displayName = string.IsNullOrWhiteSpace(msg.DisplayName)
            ? msg.Email
            : msg.DisplayName.Trim();
        var safeDisplayName = WebUtility.HtmlEncode(displayName);

        await _emailOutboxWriter.QueueRenderedEmailAsync(
            new QueueRenderedEmailRequest(
                DeduplicationKey: $"welcome-email:{msg.UserId}",
                RecipientEmail: msg.Email,
                RecipientName: displayName,
                Subject: "Welcome to Notrelix!",
                BodyHtml: $"""
                    <p>Hi {safeDisplayName},</p>
                    <p>Welcome to Notrelix! We're excited to have you on board.</p>
                    <p>Get started by exploring our features and creating your first project.</p>
                    <p>Best regards,<br/>The Notrelix Team</p>
                    """,
                BodyText: null,
                WorkspaceId: null,
                RecipientUserId: msg.UserId,
                SourceContext: "identity",
                TemplateKey: "welcome-email",
                TemplateVersion: 1,
                SourceEventId: msg.SourceEventId,
                SourceMessageId: context.MessageId),
            context.CancellationToken);

        _logger.LogInformation(
            "[Identity] RegistrationCompleted: UserId={UserId}, Email={Email}",
            msg.UserId, msg.Email);
    }
}
