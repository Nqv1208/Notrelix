namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-deleted")]
public sealed record BoardFieldDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
