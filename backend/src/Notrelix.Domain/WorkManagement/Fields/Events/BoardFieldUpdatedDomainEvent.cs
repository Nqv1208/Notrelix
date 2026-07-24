namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-updated")]
public sealed record BoardFieldUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
