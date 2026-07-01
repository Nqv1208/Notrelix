namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemFieldValueChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid FieldId,
    FieldValue OldValue,
    FieldValue NewValue,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
