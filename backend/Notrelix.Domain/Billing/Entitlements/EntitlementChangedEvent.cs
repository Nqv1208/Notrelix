using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Entitlements;

public sealed record EntitlementChangedEvent(
    Guid WorkspaceId,
    string FeatureCode,
    int NewLimit,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
