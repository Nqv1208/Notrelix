using Notrelix.Application.Events.Workspaces;
using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Workspaces.Events;

namespace Notrelix.Application.EventMappers.Workspaces;

public sealed class WorkspaceEventMapper :
    IntegrationEventMapperBase<WorkspaceCreatedDomainEvent, WorkspaceCreatedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberAddedDomainEvent, WorkspaceMemberAddedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberRemovedDomainEvent, WorkspaceMemberRemovedIntegrationEvent>
{
    public override WorkspaceCreatedIntegrationEvent? Map(WorkspaceCreatedDomainEvent domainEvent)
    {
        return new WorkspaceCreatedIntegrationEvent(
            domainEvent.WorkspaceId,
            domainEvent.Name,
            domainEvent.Slug,
            domainEvent.CreatedBy,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberAddedIntegrationEvent? Map(WorkspaceMemberAddedDomainEvent domainEvent)
    {
        return new WorkspaceMemberAddedIntegrationEvent(
            domainEvent.WorkspaceId,
            domainEvent.UserId,
            domainEvent.Role.ToString(),
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberRemovedIntegrationEvent? Map(WorkspaceMemberRemovedDomainEvent domainEvent)
    {
        return new WorkspaceMemberRemovedIntegrationEvent(
            domainEvent.WorkspaceId,
            domainEvent.UserId,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is WorkspaceCreatedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is WorkspaceMemberAddedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is WorkspaceMemberRemovedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
