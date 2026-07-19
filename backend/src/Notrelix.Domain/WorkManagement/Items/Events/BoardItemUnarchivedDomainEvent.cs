namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid ItemId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UnarchivedBy);
