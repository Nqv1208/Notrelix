using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Scheduled;

public sealed record ScheduledJobPausedEvent(
    Guid WorkspaceId,
    Guid JobId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
