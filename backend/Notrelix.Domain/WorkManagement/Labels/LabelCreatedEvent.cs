using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Labels;

public sealed record LabelCreatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid LabelId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
