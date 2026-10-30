namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-repeated")]
public sealed record BoardItemRepeatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    DateTimeOffset CompletedAt,
    DateTimeOffset? PreviousDueAt,
    DateTimeOffset? NextDueAt,
    Guid CompletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
