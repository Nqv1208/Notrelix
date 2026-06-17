using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageSoftDeletedDomainEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
