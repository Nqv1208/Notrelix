using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationScopeAddedEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Scope,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, AddedBy);
