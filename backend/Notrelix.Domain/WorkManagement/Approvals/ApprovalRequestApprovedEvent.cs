using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Approvals;

public sealed record ApprovalRequestApprovedEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
