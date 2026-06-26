namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementChangedDomainEvent(
    Guid WorkspaceId,
    string FeatureCode,
    int NewLimit,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
