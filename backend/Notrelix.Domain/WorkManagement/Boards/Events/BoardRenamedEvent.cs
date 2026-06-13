using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardRenamedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
