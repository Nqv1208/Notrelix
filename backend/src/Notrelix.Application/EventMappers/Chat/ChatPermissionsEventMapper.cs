using Notrelix.Application.Events.Chat;
using Notrelix.Domain.Chat.Permissions.Events;

namespace Notrelix.Application.EventMappers.Chat;

/// <summary>
/// Maps the override fact to the chat.space.changed integration event (outbox, same transaction as the change), so clients of the channel
/// refresh what they may do (UC-CHT-H01). The frame names no message (the id is empty) and carries no permission data: readers refetch.
/// </summary>
public sealed class ChatPermissionsEventMapper : IntegrationEventMapperBase<ChannelPermissionsChangedDomainEvent, ChatSpaceChangedIntegrationEvent>
{
    public const string PermissionsChange = "permissions";

    public override ChatSpaceChangedIntegrationEvent? Map(ChannelPermissionsChangedDomainEvent domainEvent)
    {
        return new ChatSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            MessageId: Guid.Empty,
            ChangeKind: PermissionsChange,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }
}
