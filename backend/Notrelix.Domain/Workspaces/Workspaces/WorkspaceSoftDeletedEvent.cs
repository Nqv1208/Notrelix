using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed record WorkspaceSoftDeletedEvent(
    Guid WorkspaceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
