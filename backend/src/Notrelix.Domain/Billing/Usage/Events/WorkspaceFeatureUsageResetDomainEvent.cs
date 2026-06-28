namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageResetDomainEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
