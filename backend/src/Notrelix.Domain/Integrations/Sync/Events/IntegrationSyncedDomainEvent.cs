namespace Notrelix.Domain.Integrations.Sync.Events;

[EventName("integrations.integration-synced")]
public sealed record IntegrationSyncedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SyncCursorId,
    DateTimeOffset SyncedAt,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
