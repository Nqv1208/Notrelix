using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationScopeRemovedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Scope,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RemovedBy);
