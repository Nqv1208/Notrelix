using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>Maps the Item's start and due change to its outward fact, so Activity and the other consumers see it through the outbox.</summary>
public sealed class BoardItemDatesEventMapper :
    IIntegrationEventMapper<BoardItemTimelineSetDomainEvent, BoardItemDatesChangedIntegrationEvent>
{
    public BoardItemDatesChangedIntegrationEvent? Map(BoardItemTimelineSetDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            StartedAt: domainEvent.StartedAt,
            DueAt: domainEvent.DueAt,
            PreviousStartedAt: domainEvent.PreviousStartedAt,
            PreviousDueAt: domainEvent.PreviousDueAt,
            ChangedBy: domainEvent.UpdatedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent) =>
        domainEvent is BoardItemTimelineSetDomainEvent e && Map(e) is { } mapped
            ? [new IntegrationEventMapping(mapped)]
            : [];
}
