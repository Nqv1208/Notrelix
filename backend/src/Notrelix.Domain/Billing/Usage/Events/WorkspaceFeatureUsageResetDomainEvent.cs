using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage.Events;

[EventName("billing.workspace-feature-usage-reset")]
public sealed record WorkspaceFeatureUsageResetDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    FeatureCode Feature,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
