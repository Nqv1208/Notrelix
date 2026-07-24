using Notrelix.Domain.WorkManagement.Fields;
namespace Notrelix.Domain.WorkManagement.Items.Events;

[EventName("work-management.board-item-field-value-changed")]
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
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
