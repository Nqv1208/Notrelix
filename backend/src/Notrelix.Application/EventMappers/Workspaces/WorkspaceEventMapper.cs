using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Application.EventMappers.Workspaces;

public sealed class WorkspaceEventMapper :
    IntegrationEventMapperBase<WorkspaceCreatedDomainEvent, WorkspaceCreatedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberAddedDomainEvent, WorkspaceMemberAddedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberRemovedDomainEvent, WorkspaceMemberRemovedIntegrationEvent>
{
    public override WorkspaceCreatedIntegrationEvent? Map(WorkspaceCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new WorkspaceCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            WorkspaceId: domainEvent.WorkspaceId,
            Name: domainEvent.Name,
            Slug: domainEvent.Slug,
            OwnerId: domainEvent.CreatedBy,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberAddedIntegrationEvent? Map(WorkspaceMemberAddedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new WorkspaceMemberAddedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            WorkspaceId: domainEvent.WorkspaceId,
            UserId: domainEvent.UserId,
            Role: domainEvent.Role.ToString(),
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberRemovedIntegrationEvent? Map(WorkspaceMemberRemovedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new WorkspaceMemberRemovedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            WorkspaceId: domainEvent.WorkspaceId,
            UserId: domainEvent.UserId,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
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
