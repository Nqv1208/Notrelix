namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UnarchivedBy);
