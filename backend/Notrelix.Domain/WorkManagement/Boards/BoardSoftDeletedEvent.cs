using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed record BoardSoftDeletedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
