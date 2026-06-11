using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Integrations.Sync;

public sealed record IntegrationSyncFailedEvent(
    Guid WorkspaceId,
    Guid SyncCursorId,
    string Error,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
