using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed record CalendarIntegrationSyncDirectionChangedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    CalendarSyncDirection NewDirection,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
