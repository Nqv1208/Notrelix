using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-deleted")]
public sealed record IntegrationConnectionDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
