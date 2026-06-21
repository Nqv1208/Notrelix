using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarEventLinkedDomainEvent(
    Guid WorkspaceId,
    Guid IntegrationId,
    Guid CalendarEventId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
