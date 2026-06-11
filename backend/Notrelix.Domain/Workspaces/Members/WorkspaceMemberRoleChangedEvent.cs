using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberRoleChangedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    WorkspaceRole OldRole,
    WorkspaceRole NewRole,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
