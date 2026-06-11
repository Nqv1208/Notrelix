using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Boards;

public sealed record BoardVisibilityChangedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    BoardVisibility OldVisibility,
    BoardVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
