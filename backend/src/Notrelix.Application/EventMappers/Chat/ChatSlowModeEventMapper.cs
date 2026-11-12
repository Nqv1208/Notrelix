using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Spaces.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>
/// Maps the slow mode fact to the chat.space.changed integration event (outbox, same transaction as the change), so clients of the
/// channel refresh the slow mode shown on the composer (UC-CHT-H08). The frame names no message (the id is empty) and carries no
/// value: readers refetch the channel.
/// </summary>
public sealed class ChatSlowModeEventMapper : IntegrationEventMapperBase<ChannelSlowModeChangedDomainEvent, ChatSpaceChangedIntegrationEvent>
{
    public const string SlowModeChange = "slow-mode";

    public override ChatSpaceChangedIntegrationEvent? Map(ChannelSlowModeChangedDomainEvent domainEvent)
    {
        return new ChatSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: Guid.Empty,
            ChangeKind: SlowModeChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
