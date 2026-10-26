namespace Notrelix.Domain.WorkManagement.Items.Events;

/// <summary>
/// A member who waits on an Item asked the people in charge of it for an update. The fact other contexts react to:
/// Collaboration tells each recipient in their Inbox.
/// </summary>
[EventName("work-management.board-item-owners-reminded")]
public sealed record BoardItemOwnersRemindedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid SenderUserId,
    IReadOnlyList<Guid> RecipientUserIds,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
