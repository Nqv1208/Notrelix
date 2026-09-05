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
            Guid.CreateVersion7(),
            domainEvent.AccountId,
            domainEvent.WorkspaceId,
            domainEvent.ItemId,
            domainEvent.UserId,
            domainEvent.AssignedBy,
            domainEvent.EventId,
            domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);
}
