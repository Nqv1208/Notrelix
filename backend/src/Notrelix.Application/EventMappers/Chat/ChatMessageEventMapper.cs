using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>Maps the Message domain facts to the public chat.message.* integration events (outbox, same transaction).</summary>
public sealed class ChatMessageEventMapper :
    IntegrationEventMapperBase<MessageCreatedDomainEvent, ChatMessageCreatedIntegrationEvent>,
    IIntegrationEventMapper<MessageUpdatedDomainEvent, ChatMessageUpdatedIntegrationEvent>,
    IIntegrationEventMapper<MessageDeletedDomainEvent, ChatMessageDeletedIntegrationEvent>
{
    public override ChatMessageCreatedIntegrationEvent? Map(MessageCreatedDomainEvent domainEvent)
    {
        return new ChatMessageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            MessageId: domainEvent.MessageId,
            SpaceId: domainEvent.SpaceId,
            Sequence: domainEvent.Sequence,
            AuthorId: domainEvent.AuthorId,
            Kind: domainEvent.Kind.ToWire(),
            Content: domainEvent.Content,
            ClientMessageId: domainEvent.ClientMessageId,
            Version: domainEvent.Version,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.AuthorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt,
            ReplyToId: domainEvent.ReplyToId,
            ThreadRootId: domainEvent.ThreadRootId,
            ThreadSequence: domainEvent.ThreadSequence
        );
    }

    public ChatMessageUpdatedIntegrationEvent? Map(MessageUpdatedDomainEvent domainEvent)
    {
        return new ChatMessageUpdatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            MessageId: domainEvent.MessageId,
            SpaceId: domainEvent.SpaceId,
            Sequence: domainEvent.Sequence,
            AuthorId: domainEvent.AuthorId,
            Content: domainEvent.Content,
            Version: domainEvent.Version,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.AuthorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt,
            ThreadRootId: domainEvent.ThreadRootId
        );
    }

    public ChatMessageDeletedIntegrationEvent? Map(MessageDeletedDomainEvent domainEvent)
    {
        return new ChatMessageDeletedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            MessageId: domainEvent.MessageId,
            SpaceId: domainEvent.SpaceId,
            Sequence: domainEvent.Sequence,
            AuthorId: domainEvent.AuthorId,
            DeletedBy: domainEvent.DeletedBy,
            Version: domainEvent.Version,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.DeletedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt,
            ThreadRootId: domainEvent.ThreadRootId
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is MessageCreatedDomainEvent created)
        {
            var mapped = Map(created);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is MessageUpdatedDomainEvent updated)
        {
            var mapped = Map(updated);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is MessageDeletedDomainEvent deleted)
        {
            var mapped = Map(deleted);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
