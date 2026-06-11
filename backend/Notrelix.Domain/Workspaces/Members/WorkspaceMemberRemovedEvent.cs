using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberRemovedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
