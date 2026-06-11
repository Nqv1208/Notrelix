using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed record BoardViewDeletedEvent(
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
