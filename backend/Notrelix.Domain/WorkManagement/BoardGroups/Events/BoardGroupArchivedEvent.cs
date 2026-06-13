using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupArchivedEvent(
    Guid WorkspaceId,
    Guid GroupId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
