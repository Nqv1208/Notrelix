namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-reordered")]
public sealed record BoardFieldReorderedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    FractionalIndex NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
