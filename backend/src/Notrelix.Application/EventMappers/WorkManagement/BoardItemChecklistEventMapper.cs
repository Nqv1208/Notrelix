using Notrelix.Application.Events.WorkManagement;
using Notrelix.Domain.WorkManagement.Checklists.Events;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the Work-owned checklist facts worth an Item history line to producer-owned integration events.
/// Not mapped: to-do renames and moves, checklist moves and restores (no history line).
/// </summary>
public sealed class BoardItemChecklistEventMapper :
    IIntegrationEventMapper<ChecklistCreatedDomainEvent, BoardItemChecklistAddedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistRenamedDomainEvent, BoardItemChecklistRenamedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistDeletedDomainEvent, BoardItemChecklistDeletedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistItemAddedDomainEvent, BoardItemChecklistItemAddedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistItemToggledDomainEvent, BoardItemChecklistItemCompletionChangedIntegrationEvent>,
    IIntegrationEventMapper<ChecklistItemRemovedDomainEvent, BoardItemChecklistItemRemovedIntegrationEvent>
{
    public BoardItemChecklistAddedIntegrationEvent? Map(ChecklistCreatedDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.ItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.Title,
            AddedBy: e.CreatedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    public BoardItemChecklistRenamedIntegrationEvent? Map(ChecklistRenamedDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.ItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.Title,
            RenamedBy: e.UpdatedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    public BoardItemChecklistDeletedIntegrationEvent? Map(ChecklistDeletedDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.ItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.Title,
            DeletedBy: e.DeletedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    public BoardItemChecklistItemAddedIntegrationEvent? Map(ChecklistItemAddedDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.BoardItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.ChecklistTitle,
            ChecklistItemTitle: e.Title,
            AddedBy: e.AddedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    public BoardItemChecklistItemCompletionChangedIntegrationEvent? Map(ChecklistItemToggledDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.BoardItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.ChecklistTitle,
            ChecklistItemTitle: e.ItemTitle,
            IsCompleted: e.IsDone,
            ChangedBy: e.UpdatedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    public BoardItemChecklistItemRemovedIntegrationEvent? Map(ChecklistItemRemovedDomainEvent e) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: e.AccountId,
            WorkspaceIdValue: e.WorkspaceId,
            ItemId: e.BoardItemId,
            ChecklistId: e.ChecklistId,
            ChecklistTitle: e.ChecklistTitle,
            ChecklistItemTitle: e.ItemTitle,
            RemovedBy: e.UpdatedBy,
            CorrelationId: e.EventId,
            SourceEventId: e.EventId,
            OccurredAt: e.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        IIntegrationEvent? mapped = domainEvent switch
        {
            ChecklistCreatedDomainEvent e0 => Map(e0),
            ChecklistRenamedDomainEvent e1 => Map(e1),
            ChecklistDeletedDomainEvent e2 => Map(e2),
            ChecklistItemAddedDomainEvent e3 => Map(e3),
            ChecklistItemToggledDomainEvent e4 => Map(e4),
            ChecklistItemRemovedDomainEvent e5 => Map(e5),
            _ => null,
        };

        return mapped is null ? [] : [new IntegrationEventMapping(mapped)];
    }
}
