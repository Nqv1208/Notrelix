using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestRejectedDomainEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    string? Note,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DecidedBy);
