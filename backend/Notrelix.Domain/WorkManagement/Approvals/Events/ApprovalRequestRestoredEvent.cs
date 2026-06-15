using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestRestoredEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
