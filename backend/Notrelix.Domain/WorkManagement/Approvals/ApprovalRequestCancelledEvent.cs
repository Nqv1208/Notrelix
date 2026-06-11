using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Approvals;

public sealed record ApprovalRequestCancelledEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
