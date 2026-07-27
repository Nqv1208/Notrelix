namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-restored")]
public sealed record BoardFieldRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
