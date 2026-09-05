using Notrelix.Application.Events.Workspaces;

namespace Notrelix.Application.EventMappers.Workspaces;

public sealed class WorkspaceEventMapper :
    IntegrationEventMapperBase<WorkspaceCreatedDomainEvent, WorkspaceCreatedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberAddedDomainEvent, WorkspaceMemberAddedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceMemberRemovedDomainEvent, WorkspaceMemberRemovedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceArchivedDomainEvent, WorkspaceArchivedIntegrationEvent>,
    IIntegrationEventMapper<WorkspaceUnarchivedDomainEvent, WorkspaceUnarchivedIntegrationEvent>,
    IIntegrationEventMapper<SpaceCreatedDomainEvent, SpaceCreatedIntegrationEvent>,
    IIntegrationEventMapper<TeamCreatedDomainEvent, TeamCreatedIntegrationEvent>
{
    public override WorkspaceCreatedIntegrationEvent? Map(WorkspaceCreatedDomainEvent domainEvent)
    {
        return new WorkspaceCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            Name: domainEvent.Name,
            Slug: domainEvent.Slug,
            OwnerId: domainEvent.CreatedBy,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberAddedIntegrationEvent? Map(WorkspaceMemberAddedDomainEvent domainEvent)
    {
        return new WorkspaceMemberAddedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            UserId: domainEvent.UserId,
            Role: domainEvent.Role.ToString(),
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceMemberRemovedIntegrationEvent? Map(WorkspaceMemberRemovedDomainEvent domainEvent)
    {
        return new WorkspaceMemberRemovedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            UserId: domainEvent.UserId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ActorId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceArchivedIntegrationEvent? Map(WorkspaceArchivedDomainEvent domainEvent)
    {
        return new WorkspaceArchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ArchivedBy,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public WorkspaceUnarchivedIntegrationEvent? Map(WorkspaceUnarchivedDomainEvent domainEvent)
    {
        return new WorkspaceUnarchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UnarchivedBy,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public SpaceCreatedIntegrationEvent? Map(SpaceCreatedDomainEvent domainEvent)
    {
        return new SpaceCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            SpaceId: domainEvent.SpaceId,
            Name: domainEvent.Name,
            Visibility: "Workspace",
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public TeamCreatedIntegrationEvent? Map(TeamCreatedDomainEvent domainEvent)
    {
        return new TeamCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            TeamId: domainEvent.TeamId,
            Name: domainEvent.Name,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
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
        if (domainEvent is WorkspaceArchivedDomainEvent e4)
        {
            var mapped = Map(e4);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is WorkspaceUnarchivedDomainEvent e5)
        {
            var mapped = Map(e5);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is SpaceCreatedDomainEvent e6)
        {
            var mapped = Map(e6);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is TeamCreatedDomainEvent e7)
        {
            var mapped = Map(e7);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
