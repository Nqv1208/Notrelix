using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Workspaces.Members;

public sealed record WorkspaceMemberActivatedEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid ActivatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
