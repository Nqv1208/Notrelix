using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Automation.Scheduled;

public sealed record ScheduledJobCreatedEvent(
    Guid WorkspaceId,
    Guid JobId,
    Guid RuleId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
