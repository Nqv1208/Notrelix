using Notrelix.Application.Events.Documents;

namespace Notrelix.Application.EventMappers.Documents;

public sealed class PageEventMapper :
    IntegrationEventMapperBase<PageCreatedDomainEvent, PageCreatedIntegrationEvent>,
    IIntegrationEventMapper<PageArchivedDomainEvent, PageArchivedIntegrationEvent>
{
    public override PageCreatedIntegrationEvent? Map(PageCreatedDomainEvent domainEvent)
    {
        return new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
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
            PageId: domainEvent.PageId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ArchivedBy,
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
        return [];
    }
}
