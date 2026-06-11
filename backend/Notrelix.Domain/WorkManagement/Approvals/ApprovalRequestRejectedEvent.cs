using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Approvals;

public sealed record ApprovalRequestRejectedEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    string? Note,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
