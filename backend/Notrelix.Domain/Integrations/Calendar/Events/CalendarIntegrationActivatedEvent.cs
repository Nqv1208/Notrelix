using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationActivatedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
