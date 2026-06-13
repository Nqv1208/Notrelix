using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarConflictDetectedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
