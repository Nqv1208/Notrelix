using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Sync.Events;

public sealed record IntegrationSyncFailedDomainEvent(
    Guid WorkspaceId,
    Guid SyncCursorId,
    string Error,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
