using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>Maps the Work-owned Item label facts to their producer-owned integration events for Item history.</summary>
public sealed class BoardItemLabelEventMapper :
    IIntegrationEventMapper<BoardItemLabelAddedDomainEvent, BoardItemLabelAddedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemLabelRemovedDomainEvent, BoardItemLabelRemovedIntegrationEvent>
{
    public BoardItemLabelAddedIntegrationEvent? Map(BoardItemLabelAddedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            LabelId: domainEvent.LabelId,
            LabelName: domainEvent.LabelName,
            AddedBy: domainEvent.AddedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public BoardItemLabelRemovedIntegrationEvent? Map(BoardItemLabelRemovedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            LabelId: domainEvent.LabelId,
            LabelName: domainEvent.LabelName,
            RemovedBy: domainEvent.RemovedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        IIntegrationEvent? mapped = domainEvent switch
        {
            BoardItemLabelAddedDomainEvent e => Map(e),
            BoardItemLabelRemovedDomainEvent e => Map(e),
            _ => null,
        };

        return mapped is null ? [] : [new IntegrationEventMapping(mapped)];
    }
}
