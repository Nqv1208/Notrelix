using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed record CalendarSyncedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
