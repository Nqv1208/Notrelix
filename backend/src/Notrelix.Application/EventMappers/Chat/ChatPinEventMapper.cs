using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>Maps the pin fact to the chat.space.changed integration event (outbox, same transaction as the pin change).</summary>
public sealed class ChatPinEventMapper : IntegrationEventMapperBase<MessagePinChangedDomainEvent, ChatSpaceChangedIntegrationEvent>
{
    public const string PinnedChange = "pinned";
    public const string UnpinnedChange = "unpinned";

    public override ChatSpaceChangedIntegrationEvent? Map(MessagePinChangedDomainEvent domainEvent)
    {
        return new ChatSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: domainEvent.MessageId,
            ChangeKind: domainEvent.Pinned ? PinnedChange : UnpinnedChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
