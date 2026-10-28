using Notrelix.Application.Events.WorkManagement;

namespace Notrelix.Application.EventMappers.WorkManagement;

/// <summary>
/// Maps the Work-owned Item lifecycle Domain facts that had no outward contract (unarchived, deleted,
/// restored, moved to another Board, assignee removed) to their producer-owned integration events, so
/// Activity, Automation and Analytics see them through the outbox like every other Item fact.
/// </summary>
public sealed class BoardItemLifecycleEventMapper :
    IIntegrationEventMapper<BoardItemUnarchivedDomainEvent, BoardItemUnarchivedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemDeletedDomainEvent, BoardItemDeletedIntegrationEvent>,
    IIntegrationEventMapper<BoardItemRestoredDomainEvent, BoardItemRestoredIntegrationEvent>,
    IIntegrationEventMapper<BoardItemMovedToBoardDomainEvent, BoardItemMovedToBoardIntegrationEvent>,
    IIntegrationEventMapper<BoardItemMemberUnassignedDomainEvent, BoardItemMemberUnassignedIntegrationEvent>
{
    public BoardItemUnarchivedIntegrationEvent? Map(BoardItemUnarchivedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            UnarchivedBy: domainEvent.UnarchivedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public BoardItemDeletedIntegrationEvent? Map(BoardItemDeletedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            DeletedBy: domainEvent.DeletedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public BoardItemRestoredIntegrationEvent? Map(BoardItemRestoredDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            BoardId: domainEvent.BoardId,
            RestoredBy: domainEvent.RestoredBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public BoardItemMovedToBoardIntegrationEvent? Map(BoardItemMovedToBoardDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            FromBoardId: domainEvent.FromBoardId,
            ToBoardId: domainEvent.ToBoardId,
            FromGroupId: domainEvent.FromGroupId,
            ToGroupId: domainEvent.ToGroupId,
            ItemKey: domainEvent.ItemKey,
            Revision: domainEvent.Version,
            MovedBy: domainEvent.MovedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    public BoardItemMemberUnassignedIntegrationEvent? Map(BoardItemMemberUnassignedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountIdValue: domainEvent.AccountId,
            WorkspaceIdValue: domainEvent.WorkspaceId,
            ItemId: domainEvent.ItemId,
            UnassignedUserId: domainEvent.UserId,
            UnassignedBy: domainEvent.UnassignedBy,
            CorrelationId: domainEvent.EventId,
            SourceEventId: domainEvent.EventId,
            OccurredAt: domainEvent.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        IIntegrationEvent? mapped = domainEvent switch
        {
            BoardItemUnarchivedDomainEvent e => Map(e),
            BoardItemDeletedDomainEvent e => Map(e),
            BoardItemRestoredDomainEvent e => Map(e),
            BoardItemMovedToBoardDomainEvent e => Map(e),
            BoardItemMemberUnassignedDomainEvent e => Map(e),
            _ => null,
        };

        return mapped is null ? [] : [new IntegrationEventMapping(mapped)];
    }
}
