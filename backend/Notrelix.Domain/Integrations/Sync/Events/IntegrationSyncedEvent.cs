using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Sync.Events;

public sealed record IntegrationSyncedEvent(
    Guid WorkspaceId,
    Guid SyncCursorId,
    DateTimeOffset SyncedAt,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
