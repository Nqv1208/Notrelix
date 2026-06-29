namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageRestoredDomainEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
