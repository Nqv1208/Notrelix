using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public sealed record BoardFieldReorderedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    double NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
