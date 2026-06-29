namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementChangedDomainEvent(
    Guid WorkspaceId,
    string FeatureCode,
    int NewLimit,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
