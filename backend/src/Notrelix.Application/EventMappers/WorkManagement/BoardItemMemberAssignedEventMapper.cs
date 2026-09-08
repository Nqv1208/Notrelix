using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the WorkManagement-owned member-assignment Domain fact to its
/// producer-owned outward contract. The mapper lives with the producer's
/// event-mapping family; Automation consumes the contract as a subscriber.
/// </summary>
public sealed class BoardItemMemberAssignedEventMapper
    : IntegrationEventMapperBase<Domain.WorkManagement.Items.Events.BoardItemMemberAssignedDomainEvent, BoardItemMemberAssignedIntegrationEvent>
{
    public override BoardItemMemberAssignedIntegrationEvent Map(Domain.WorkManagement.Items.Events.BoardItemMemberAssignedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            AssignedUserId: domainEvent.UserId,
            AssignedBy: domainEvent.AssignedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt);
}
