using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Spaces.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>
/// Maps the channel link fact to the chat.space.changed integration event (outbox, same transaction as the change), so clients of the
/// channel refresh the link chip and the Discuss entry points (UC-CHT-H10). The frame names no message (the id is empty) and carries no
/// value: readers refetch the channel.
/// </summary>
public sealed class ChatLinkEventMapper : IntegrationEventMapperBase<ChannelLinkChangedDomainEvent, ChatSpaceChangedIntegrationEvent>
{
    public const string LinkChange = "link";

    public override ChatSpaceChangedIntegrationEvent? Map(ChannelLinkChangedDomainEvent domainEvent)
    {
        return new ChatSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: Guid.Empty,
            ChangeKind: LinkChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
