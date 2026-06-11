using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberSuspendedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid SuspendedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
