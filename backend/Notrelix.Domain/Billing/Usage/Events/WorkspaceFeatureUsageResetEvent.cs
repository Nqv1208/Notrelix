using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageResetEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
