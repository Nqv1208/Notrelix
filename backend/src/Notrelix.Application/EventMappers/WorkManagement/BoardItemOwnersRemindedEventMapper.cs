using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the Work-owned "owners were reminded" Domain fact to its producer-owned outward contract;
/// Collaboration consumes it to tell each recipient in their Inbox.
/// </summary>
public sealed class BoardItemOwnersRemindedEventMapper
    : IntegrationEventMapperBase<Domain.WorkManagement.Items.Events.BoardItemOwnersRemindedDomainEvent, BoardItemOwnersRemindedIntegrationEvent>
{
    public override BoardItemOwnersRemindedIntegrationEvent Map(Domain.WorkManagement.Items.Events.BoardItemOwnersRemindedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            SenderUserId: domainEvent.SenderUserId,
            RecipientUserIds: domainEvent.RecipientUserIds,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);
}
