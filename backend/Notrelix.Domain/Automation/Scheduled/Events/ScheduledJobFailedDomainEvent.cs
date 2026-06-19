using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Scheduled.Events;

public sealed record ScheduledJobFailedDomainEvent(
    Guid WorkspaceId,
    Guid JobId,
    string Reason,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
