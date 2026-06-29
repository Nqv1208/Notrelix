namespace Notrelix.Domain.Integrations.Sync.Events;

public sealed record IntegrationSyncFailedDomainEvent(
    Guid WorkspaceId,
    Guid SyncCursorId,
    string Error,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
