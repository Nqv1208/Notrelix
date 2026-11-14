using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>Maps the reaction fact to the chat.reaction.changed integration event (outbox, same transaction as the reaction change).</summary>
public sealed class ChatReactionEventMapper : IntegrationEventMapperBase<MessageReactionChangedDomainEvent, ChatReactionChangedIntegrationEvent>
{
    public const string AddedChange = "added";
    public const string RemovedChange = "removed";

    public override ChatReactionChangedIntegrationEvent? Map(MessageReactionChangedDomainEvent domainEvent)
    {
        return new ChatReactionChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: domainEvent.MessageId,
            Kind: domainEvent.Kind,
            ChangeKind: domainEvent.Added ? AddedChange : RemovedChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
