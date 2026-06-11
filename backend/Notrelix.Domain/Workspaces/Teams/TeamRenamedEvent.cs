using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Teams;

public sealed record TeamRenamedEvent(
    Guid WorkspaceId,
    Guid TeamId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
