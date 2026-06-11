using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Teams;

public sealed record TeamMemberRemovedEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
