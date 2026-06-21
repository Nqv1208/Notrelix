using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelRestoredDomainEvent(
    Guid WorkspaceId,
    Guid LabelId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
