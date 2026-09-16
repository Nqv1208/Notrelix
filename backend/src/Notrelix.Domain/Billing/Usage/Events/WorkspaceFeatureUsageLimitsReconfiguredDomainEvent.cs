using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.workspace-feature-usage-limits-reconfigured")]
public sealed record WorkspaceFeatureUsageLimitsReconfiguredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    decimal? HardLimit,
    decimal? SoftLimit,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);