using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.WorkManagement.Checklists.Events;
using Notrelix.Domain.WorkManagement.Fields.Events;
using Notrelix.Domain.WorkManagement.Labels.Events;
using Notrelix.Domain.WorkManagement.Views.Events;

namespace Notrelix.Application.EventMappers.WorkManagement;

public sealed class BoardEventMapper :
    IntegrationEventMapperBase<BoardCreatedDomainEvent, BoardCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardRenamedDomainEvent, BoardRenamedIntegrationEvent>,
    IIntegrationEventMapper<BoardArchivedDomainEvent, BoardArchivedIntegrationEvent>,
    IIntegrationEventMapper<BoardUnarchivedDomainEvent, BoardUnarchivedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemCreatedDomainEvent, BoardItemCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemFieldValueChangedDomainEvent, BoardItemFieldValueChangedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemRenamedDomainEvent, BoardItemRenamedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemMovedDomainEvent, BoardItemMovedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemArchivedDomainEvent, BoardItemArchivedIntegrationEvent>,
    IIntegrationEventMapper<BoardFieldCreatedDomainEvent, BoardFieldCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardFieldUpdatedDomainEvent, BoardFieldUpdatedIntegrationEvent>,
    IIntegrationEventMapper<BoardFieldDeletedDomainEvent, BoardFieldDeletedIntegrationEvent>,
    IIntegrationEventMapper<BoardViewCreatedDomainEvent, BoardViewCreatedIntegrationEvent>,
    IIntegrationEventMapper<BoardViewDeletedDomainEvent, BoardViewDeletedIntegrationEvent>,
    IIntegrationEventMapper<LabelCreatedDomainEvent, LabelCreatedIntegrationEvent>,
    IIntegrationEventMapper<LabelUpdatedDomainEvent, LabelUpdatedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistCreatedDomainEvent, ChecklistCreatedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistItemToggledDomainEvent, ChecklistItemToggledIntegrationEvent>
{
    public override BoardCreatedIntegrationEvent? Map(BoardCreatedDomainEvent domainEvent)
    {
        return new BoardCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            Name: domainEvent.Title,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardRenamedIntegrationEvent? Map(BoardRenamedDomainEvent domainEvent)
    {
        return new BoardRenamedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldName: domainEvent.OldTitle,
            NewName: domainEvent.NewTitle,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardArchivedIntegrationEvent? Map(BoardArchivedDomainEvent domainEvent)
    {
        return new BoardArchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ArchivedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardUnarchivedIntegrationEvent? Map(BoardUnarchivedDomainEvent domainEvent)
    {
        return new BoardUnarchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UnarchivedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemCreatedIntegrationEvent? Map(BoardItemCreatedDomainEvent domainEvent)
    {
        return new BoardItemCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            Title: domainEvent.Name,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemFieldValueChangedIntegrationEvent? Map(BoardItemFieldValueChangedDomainEvent domainEvent)
    {
        return new BoardItemFieldValueChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            FieldId: domainEvent.FieldId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldValue: domainEvent.OldValue.ToString(),
            NewValue: domainEvent.NewValue.ToString(),
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemRenamedIntegrationEvent? Map(BoardItemRenamedDomainEvent domainEvent)
    {
        return new BoardItemRenamedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldName: domainEvent.OldName,
            NewName: domainEvent.NewName,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemMovedIntegrationEvent? Map(BoardItemMovedDomainEvent domainEvent)
    {
        return new BoardItemMovedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            OldGroupId: domainEvent.OldGroupId,
            NewGroupId: domainEvent.NewGroupId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardItemArchivedIntegrationEvent? Map(BoardItemArchivedDomainEvent domainEvent)
    {
        return new BoardItemArchivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.ArchivedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardFieldCreatedIntegrationEvent? Map(BoardFieldCreatedDomainEvent domainEvent)
    {
        return new BoardFieldCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            FieldId: domainEvent.FieldId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            FieldName: domainEvent.Name,
            FieldType: domainEvent.Type.ToString(),
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardFieldUpdatedIntegrationEvent? Map(BoardFieldUpdatedDomainEvent domainEvent)
    {
        return new BoardFieldUpdatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            FieldId: domainEvent.FieldId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardFieldDeletedIntegrationEvent? Map(BoardFieldDeletedDomainEvent domainEvent)
    {
        return new BoardFieldDeletedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            FieldId: domainEvent.FieldId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.DeletedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardViewCreatedIntegrationEvent? Map(BoardViewCreatedDomainEvent domainEvent)
    {
        return new BoardViewCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ViewId: domainEvent.ViewId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            ViewName: domainEvent.Name,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public BoardViewDeletedIntegrationEvent? Map(BoardViewDeletedDomainEvent domainEvent)
    {
        return new BoardViewDeletedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ViewId: domainEvent.ViewId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.DeletedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public LabelCreatedIntegrationEvent? Map(LabelCreatedDomainEvent domainEvent)
    {
        return new LabelCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            LabelId: domainEvent.LabelId,
            BoardId: domainEvent.BoardId,
            WorkspaceId: domainEvent.WorkspaceId,
            LabelName: domainEvent.Name,
            CorrelationId: domainEvent.EventId,
            ActorUserId: null,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public LabelUpdatedIntegrationEvent? Map(LabelUpdatedDomainEvent domainEvent)
    {
        return new LabelUpdatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            LabelId: domainEvent.LabelId,
            BoardId: Guid.Empty,
            WorkspaceId: domainEvent.WorkspaceId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.UpdatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public ChecklistCreatedIntegrationEvent? Map(ChecklistCreatedDomainEvent domainEvent)
    {
        return new ChecklistCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ChecklistId: domainEvent.ChecklistId,
            ItemId: domainEvent.ItemId,
            BoardId: Guid.Empty,
            WorkspaceId: domainEvent.WorkspaceId,
            ChecklistTitle: domainEvent.Title,
            CorrelationId: domainEvent.EventId,
            ActorUserId: null,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public ChecklistItemToggledIntegrationEvent? Map(ChecklistItemToggledDomainEvent domainEvent)
    {
        return new ChecklistItemToggledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            ChecklistId: domainEvent.ChecklistId,
            ChecklistItemId: domainEvent.ItemId,
            ItemId: Guid.Empty,
            BoardId: Guid.Empty,
            WorkspaceId: domainEvent.WorkspaceId,
            IsCompleted: domainEvent.IsDone,
            CorrelationId: domainEvent.EventId,
            ActorUserId: null,
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
        if (domainEvent is BoardRenamedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardArchivedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardUnarchivedDomainEvent e4)
        {
            var mapped = Map(e4);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemCreatedDomainEvent e5)
        {
            var mapped = Map(e5);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemFieldValueChangedDomainEvent e6)
        {
            var mapped = Map(e6);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemRenamedDomainEvent e7)
        {
            var mapped = Map(e7);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemMovedDomainEvent e8)
        {
            var mapped = Map(e8);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardItemArchivedDomainEvent e9)
        {
            var mapped = Map(e9);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardFieldCreatedDomainEvent e10)
        {
            var mapped = Map(e10);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardFieldUpdatedDomainEvent e11)
        {
            var mapped = Map(e11);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardFieldDeletedDomainEvent e12)
        {
            var mapped = Map(e12);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardViewCreatedDomainEvent e13)
        {
            var mapped = Map(e13);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is BoardViewDeletedDomainEvent e14)
        {
            var mapped = Map(e14);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is LabelCreatedDomainEvent e15)
        {
            var mapped = Map(e15);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is LabelUpdatedDomainEvent e16)
        {
            var mapped = Map(e16);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is ChecklistCreatedDomainEvent e17)
        {
            var mapped = Map(e17);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is ChecklistItemToggledDomainEvent e18)
        {
            var mapped = Map(e18);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
