using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.workspace-feature-usage-initialized")]
public sealed record WorkspaceFeatureUsageInitializedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    decimal CurrentUsage,
    decimal? HardLimit,
    decimal? SoftLimit,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
