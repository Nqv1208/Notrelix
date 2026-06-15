using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestSoftDeletedEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
