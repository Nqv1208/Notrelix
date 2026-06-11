using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed record BoardRestoredEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
