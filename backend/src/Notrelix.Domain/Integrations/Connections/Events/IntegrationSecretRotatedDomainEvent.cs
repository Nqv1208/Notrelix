namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-secret-rotated")]
public sealed record IntegrationSecretRotatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    string Version,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
