namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.board-field-reordered")]
public sealed record BoardFieldReorderedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    double NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
