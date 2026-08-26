using Notrelix.Application.Events.Automation;

namespace Notrelix.Application.EventMappers.Automation;

public sealed class AutomationEventMapper
    : IntegrationEventMapperBase<BoardItemMemberAssignedDomainEvent, BoardItemMemberAssignedForAutomationIntegrationEvent>
{
    public override BoardItemMemberAssignedForAutomationIntegrationEvent Map(BoardItemMemberAssignedDomainEvent domainEvent) =>
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
