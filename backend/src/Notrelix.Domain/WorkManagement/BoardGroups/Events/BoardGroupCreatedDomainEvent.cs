using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.BoardGroups.Events;

public sealed record BoardGroupCreatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    string Title,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CreatedBy);
