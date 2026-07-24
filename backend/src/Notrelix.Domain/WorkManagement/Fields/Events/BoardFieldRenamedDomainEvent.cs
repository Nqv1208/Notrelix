namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-renamed")]
public sealed record BoardFieldRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
