using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed record CalendarIntegrationDeactivatedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid DeactivatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
