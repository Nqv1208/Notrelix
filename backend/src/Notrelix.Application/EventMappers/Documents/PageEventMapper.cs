using Notrelix.Application.Events.Documents;

namespace Notrelix.Application.EventMappers.Documents;

public sealed class PageEventMapper :
    IntegrationEventMapperBase<PageCreatedDomainEvent, PageCreatedIntegrationEvent>,
    IIntegrationEventMapper<PageArchivedDomainEvent, PageArchivedIntegrationEvent>,
    IIntegrationEventMapper<PageBlocksSavedDomainEvent, PageBlocksSavedIntegrationEvent>,
    IIntegrationEventMapper<PageRenamedDomainEvent, PageRenamedIntegrationEvent>,
    IIntegrationEventMapper<PageMovedDomainEvent, PageMovedIntegrationEvent>,
    IIntegrationEventMapper<PageSpaceChangedDomainEvent, PageSpaceChangedIntegrationEvent>,
    IIntegrationEventMapper<PageRestoredDomainEvent, PageRestoredIntegrationEvent>,
    IIntegrationEventMapper<PageDeletedDomainEvent, PageDeletedIntegrationEvent>
{
    public override PageCreatedIntegrationEvent? Map(PageCreatedDomainEvent domainEvent)
    {
        return new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            Title: domainEvent.Title,
            ParentId: null,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageArchivedIntegrationEvent? Map(PageArchivedDomainEvent domainEvent)
    {
        return new PageArchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ArchivedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageBlocksSavedIntegrationEvent? Map(PageBlocksSavedDomainEvent domainEvent)
    {
        return new PageBlocksSavedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            PageTitle: domainEvent.PageTitle,
            Mentions: domainEvent.Mentions.Select(m => new PageBlockMention(m.BlockId, m.UserId)).ToList(),
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.SavedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageRenamedIntegrationEvent? Map(PageRenamedDomainEvent domainEvent)
    {
        return new PageRenamedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldTitle: domainEvent.OldTitle,
            NewTitle: domainEvent.NewTitle,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageMovedIntegrationEvent? Map(PageMovedDomainEvent domainEvent)
    {
        return new PageMovedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldParentId: domainEvent.OldParentId,
            NewParentId: domainEvent.NewParentId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageSpaceChangedIntegrationEvent? Map(PageSpaceChangedDomainEvent domainEvent)
    {
        return new PageSpaceChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldSpaceId: domainEvent.OldSpaceId,
            NewSpaceId: domainEvent.NewSpaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageRestoredIntegrationEvent? Map(PageRestoredDomainEvent domainEvent)
    {
        return new PageRestoredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.RestoredBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public PageDeletedIntegrationEvent? Map(PageDeletedDomainEvent domainEvent)
    {
        return new PageDeletedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.DeletedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is PageCreatedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is PageArchivedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is PageBlocksSavedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is PageRenamedDomainEvent x0)
            return [new IntegrationEventMapping(Map(x0)!)];
        if (domainEvent is PageMovedDomainEvent x1)
            return [new IntegrationEventMapping(Map(x1)!)];
        if (domainEvent is PageSpaceChangedDomainEvent x2)
            return [new IntegrationEventMapping(Map(x2)!)];
        if (domainEvent is PageRestoredDomainEvent x3)
            return [new IntegrationEventMapping(Map(x3)!)];
        if (domainEvent is PageDeletedDomainEvent x4)
            return [new IntegrationEventMapping(Map(x4)!)];
        return [];
    }
}
