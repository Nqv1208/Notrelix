using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions;

public record SubscriptionStartedEvent(Guid WorkspaceId, Guid PlanId) : DomainRecordEvent;
public record SubscriptionChangedEvent(Guid WorkspaceId, Guid OldPlanId, Guid NewPlanId) : DomainRecordEvent;
public record SubscriptionCancelledEvent(Guid WorkspaceId) : DomainRecordEvent;
public record SubscriptionExpiredEvent(Guid WorkspaceId) : DomainRecordEvent;
