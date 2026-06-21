using Notrelix.Application.Events.Documents;
using Notrelix.Domain.Common;
using Notrelix.Domain.Documents.Pages.Events;

namespace Notrelix.Application.EventMappers.Documents;

public sealed class PageEventMapper :
    IntegrationEventMapperBase<PageCreatedDomainEvent, PageCreatedIntegrationEvent>,
    IIntegrationEventMapper<PageArchivedDomainEvent, PageArchivedIntegrationEvent>
{
    public override PageCreatedIntegrationEvent? Map(PageCreatedDomainEvent domainEvent)
    {
        return new PageCreatedIntegrationEvent(
            domainEvent.PageId,
            domainEvent.WorkspaceId,
            domainEvent.Title,
            null,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public PageArchivedIntegrationEvent? Map(PageArchivedDomainEvent domainEvent)
    {
        return new PageArchivedIntegrationEvent(
            domainEvent.PageId,
            domainEvent.WorkspaceId,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
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
