using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageInitializedDomainEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    decimal CurrentUsage,
    decimal? HardLimit,
    decimal? SoftLimit,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
