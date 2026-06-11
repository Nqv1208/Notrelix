using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed record CalendarEventLinkedEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid CalendarEventId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
