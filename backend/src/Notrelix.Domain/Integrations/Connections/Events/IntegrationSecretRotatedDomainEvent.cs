namespace Notrelix.Domain.Integrations.Connections.Events;

public sealed record IntegrationSecretRotatedDomainEvent(
    Guid WorkspaceId,
    Guid ConnectionId,
    string Version,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RotatedBy);
