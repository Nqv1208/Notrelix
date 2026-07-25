using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Infrastructure.Messaging.Consumers.Workspaces.InvitationDelivery;

public sealed class SendInvitationEmailConsumer : IConsumer<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>
{
    private readonly IEmailOutboxWriter _emailOutboxWriter;
    private readonly ILogger<SendInvitationEmailConsumer> _logger;

    public SendInvitationEmailConsumer(
        IEmailOutboxWriter emailOutboxWriter,
        ILogger<SendInvitationEmailConsumer> logger)
    {
        _emailOutboxWriter = emailOutboxWriter;
        _logger = logger;
    }

    public async Task Consume(
        ConsumeContext<WorkspaceInvitationDeliveryRequestedIntegrationEventV1> context)
    {
        var msg = context.Message;

        await _emailOutboxWriter.QueueTemplatedEmailAsync(
            new QueueTemplatedEmailRequest<WorkspaceInvitationEmailPayload>(
                DeduplicationKey: $"workspace-invitation:{msg.InvitationId}:generation:{msg.TokenGeneration}",
                RecipientEmail: msg.RecipientEmail,
                RecipientName: null,
                WorkspaceId: msg.WorkspaceId,
                RecipientUserId: null,
                SourceContext: "workspaces",
                TemplateKey: "workspace-invitation",
                TemplateVersion: 1,
                Payload: new WorkspaceInvitationEmailPayload(
                    msg.InvitationId,
                    msg.TokenGeneration,
                    new ProtectedSecretEnvelope(msg.ProtectedToken),
                    msg.ExpiresAt),
                SourceEventId: msg.EventId,
                SourceMessageId: context.MessageId,
                SensitivePayloadExpiresAt: msg.ExpiresAt),
            context.CancellationToken);

        _logger.LogInformation(
            "Workspace invitation delivery queued. InvitationId={InvitationId}",
            msg.InvitationId);
    }
}
