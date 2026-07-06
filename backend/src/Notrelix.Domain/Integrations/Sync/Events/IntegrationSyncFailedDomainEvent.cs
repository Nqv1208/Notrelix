namespace Notrelix.Domain.Integrations.Sync.Events;

public sealed record IntegrationSyncFailedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SyncCursorId,
    string Error,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
