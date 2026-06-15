using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSoftDeletedEvent(
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
