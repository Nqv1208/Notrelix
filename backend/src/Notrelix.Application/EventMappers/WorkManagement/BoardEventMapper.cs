using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

public sealed class BoardEventMapper :
    IntegrationEventMapperBase<BoardCreatedDomainEvent, BoardCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemCreatedDomainEvent, BoardItemCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemFieldValueChangedDomainEvent, BoardItemFieldValueChangedIntegrationEvent>
{
    public override BoardCreatedIntegrationEvent? Map(BoardCreatedDomainEvent domainEvent)
    {
        return new BoardCreatedIntegrationEvent(
            domainEvent.BoardId,
            domainEvent.WorkspaceId,
            domainEvent.Title,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public BoardItemCreatedIntegrationEvent? Map(BoardItemCreatedDomainEvent domainEvent)
    {
        return new BoardItemCreatedIntegrationEvent(
            domainEvent.ItemId,
            domainEvent.BoardId,
            domainEvent.WorkspaceId,
            domainEvent.Name,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public BoardItemFieldValueChangedIntegrationEvent? Map(BoardItemFieldValueChangedDomainEvent domainEvent)
    {
        return new BoardItemFieldValueChangedIntegrationEvent(
            domainEvent.ItemId,
            domainEvent.BoardId,
            domainEvent.FieldId,
            domainEvent.WorkspaceId,
            domainEvent.OldValue.ToString(),
            domainEvent.NewValue.ToString(),
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
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
