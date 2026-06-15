using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationConnectionExpiredEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid ExpiredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ExpiredBy);
