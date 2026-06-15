using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardUnarchivedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UnarchivedBy);
