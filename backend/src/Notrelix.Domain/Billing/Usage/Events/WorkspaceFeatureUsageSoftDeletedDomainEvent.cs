using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.workspace-feature-usage-soft-deleted")]
public sealed record WorkspaceFeatureUsageSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
