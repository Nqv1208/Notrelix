using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementRestoredEvent(
    Guid WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
