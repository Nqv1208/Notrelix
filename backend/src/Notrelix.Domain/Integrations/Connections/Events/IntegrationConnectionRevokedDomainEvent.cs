namespace Notrelix.Domain.Integrations.Connections.Events;

[EventName("integrations.integration-connection-revoked")]
public sealed record IntegrationConnectionRevokedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
