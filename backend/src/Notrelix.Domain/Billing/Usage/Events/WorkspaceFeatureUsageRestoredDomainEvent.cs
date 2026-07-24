using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.workspace-feature-usage-restored")]
public sealed record WorkspaceFeatureUsageRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
