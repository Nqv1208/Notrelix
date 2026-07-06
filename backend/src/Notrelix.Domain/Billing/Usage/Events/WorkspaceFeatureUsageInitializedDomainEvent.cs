namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageInitializedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    decimal CurrentUsage,
    decimal? HardLimit,
    decimal? SoftLimit,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
