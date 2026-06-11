using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Calendar;

public sealed record CalendarIntegrationConnectedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
