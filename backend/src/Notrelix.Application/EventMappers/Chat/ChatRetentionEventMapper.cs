using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Spaces.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>
/// Maps the retention fact to the chat.space.changed integration event (outbox, same transaction as the batch), so clients of the channel
/// refetch the history they hold (UC-CHT-H05). The frame names no message (the id is empty) and carries no content or count; the job has no
/// actor, so the frame has none either.
/// </summary>
public sealed class ChatRetentionEventMapper : IntegrationEventMapperBase<ChannelRetentionAppliedDomainEvent, ChatSpaceChangedIntegrationEvent>
{
    public const string RetentionChange = "retention";

    public override ChatSpaceChangedIntegrationEvent? Map(ChannelRetentionAppliedDomainEvent domainEvent)
    {
        return new ChatSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: Guid.Empty,
            ChangeKind: RetentionChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: null,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
