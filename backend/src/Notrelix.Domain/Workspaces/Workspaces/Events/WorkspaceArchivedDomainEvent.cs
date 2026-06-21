using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceArchivedDomainEvent(
    Guid WorkspaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ArchivedBy);
