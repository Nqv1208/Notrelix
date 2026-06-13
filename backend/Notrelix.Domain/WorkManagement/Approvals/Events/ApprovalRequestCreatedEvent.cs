using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestCreatedEvent(
    Guid RequestId,
    Guid WorkspaceId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
