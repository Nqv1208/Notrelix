namespace Notrelix.Domain.Integrations.Sync.Events;

public sealed record IntegrationSyncedDomainEvent(
    Guid WorkspaceId,
    Guid SyncCursorId,
    DateTimeOffset SyncedAt,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
