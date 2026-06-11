using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed record BoardArchivedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
