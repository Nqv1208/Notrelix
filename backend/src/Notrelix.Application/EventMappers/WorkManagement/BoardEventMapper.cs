using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

public sealed class BoardEventMapper :
    IntegrationEventMapperBase<BoardCreatedDomainEvent, BoardCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemCreatedDomainEvent, BoardItemCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemFieldValueChangedDomainEvent, BoardItemFieldValueChangedIntegrationEvent>
{
    public override BoardCreatedIntegrationEvent? Map(BoardCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new BoardCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            Name: domainEvent.Title,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemCreatedIntegrationEvent? Map(BoardItemCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new BoardItemCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            Title: domainEvent.Name,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemFieldValueChangedIntegrationEvent? Map(BoardItemFieldValueChangedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new BoardItemFieldValueChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            FieldId: domainEvent.FieldId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldValue: domainEvent.OldValue.ToString(),
            NewValue: domainEvent.NewValue.ToString(),
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is BoardCreatedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemCreatedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemFieldValueChangedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
