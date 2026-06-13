using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamMemberAddedEvent(
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    TeamMemberRole Role,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
