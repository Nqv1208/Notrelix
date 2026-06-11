using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Workspaces;

public sealed record WorkspaceArchivedEvent(
    Guid WorkspaceId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
