using Notrelix.Application.Events.Chat;
using Notrelix.Application.Features.Chat.Calls;

namespace Notrelix.Infrastructure.Messaging.Consumers.Chat;

/// <remarks>
/// Drops a released person from the media room. A failure is not swallowed: the exception sends the message back through the endpoint's
/// retry policy, so the removal is at-least-once. Removing a person who is already out of the room is harmless, so a redelivery is safe.
/// </remarks>
[OrderingNotApplicable("Removing a person from a media room is idempotent and independent per message; a later message for the same room cannot be undone by an earlier one.")]
public sealed class ChatCallParticipantRevokedConsumer(IChatCallMediaGateway media) : IConsumer<ChatCallParticipantRevokedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ChatCallParticipantRevokedIntegrationEvent> context) =>
        media.RemoveParticipantAsync(context.Message.RoomKey, context.Message.PersonId, context.CancellationToken);
}

public sealed class ChatCallParticipantRevokedConsumerDefinition : ConsumerDefinition<ChatCallParticipantRevokedConsumer>
{
    public ChatCallParticipantRevokedConsumerDefinition() => EndpointName = "notrelix-chat-call-participant-revoked-v1";

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatCallParticipantRevokedConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        endpointConfigurator.UseMessageRetry(r => r.Exponential(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2)));
}

/// <remarks>
/// Applies a changed right to publish to a person's live connection. Applying the same value twice is harmless, and a failure is not
/// swallowed: the exception sends the message back through the endpoint's retry policy, so the update is at-least-once.
/// </remarks>
[OrderingNotApplicable("Each message carries the full publish right of one person and applying it is idempotent; two permission changes in the same instant are rare, and a reordered pair leaves the older value only until the next change.")]
public sealed class ChatCallParticipantPermissionChangedConsumer(IChatCallMediaGateway media) : IConsumer<ChatCallParticipantPermissionChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ChatCallParticipantPermissionChangedIntegrationEvent> context) =>
        media.UpdateParticipantPermissionAsync(context.Message.RoomKey, context.Message.PersonId, context.Message.CanPublish, context.Message.CanPublishVideo, context.CancellationToken);
}

public sealed class ChatCallParticipantPermissionChangedConsumerDefinition : ConsumerDefinition<ChatCallParticipantPermissionChangedConsumer>
{
    public ChatCallParticipantPermissionChangedConsumerDefinition() => EndpointName = "notrelix-chat-call-participant-permission-changed-v1";

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ChatCallParticipantPermissionChangedConsumer> consumerConfigurator,
        IRegistrationContext context) =>
        endpointConfigurator.UseMessageRetry(r => r.Exponential(8, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(2)));
}
