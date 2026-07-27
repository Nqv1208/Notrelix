namespace Notrelix.Domain.WorkManagement.Fields.Events;

[EventName("work-management.field-options-reordered")]
public sealed record FieldOptionsReorderedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    IReadOnlyList<Guid> OrderedOptionIds,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
