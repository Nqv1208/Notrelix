using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed record BoardViewConfigUpdatedEvent(
    Guid WorkspaceId,
    Guid ViewId,
    Guid BoardId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
