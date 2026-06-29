namespace Notrelix.Domain.Billing.Entitlements.Events;

public sealed record EntitlementSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid EntitlementId,
    string FeatureCode,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
