namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationSecretRotatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    string Version,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RotatedBy);
