namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-created")]
public sealed record BoardFieldCreatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    string Name,
    FieldType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
