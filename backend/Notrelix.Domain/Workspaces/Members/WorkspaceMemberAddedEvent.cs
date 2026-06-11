using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberAddedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole Role,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
