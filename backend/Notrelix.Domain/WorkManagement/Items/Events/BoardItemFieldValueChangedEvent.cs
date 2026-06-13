using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemFieldValueChangedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid FieldId,
    FieldValue OldValue,
    FieldValue NewValue,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
