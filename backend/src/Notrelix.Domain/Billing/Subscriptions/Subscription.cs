namespace Notrelix.Domain.Billing.Subscriptions;

public class Subscription : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public DateTimeOffset CurrentPeriodStart { get; private set; }
    public DateTimeOffset CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }

    private Subscription() : base() { }

    public static Subscription Create(Guid accountId, Guid planId, SubscriptionTier tier, DateTimeOffset start, DateTimeOffset end, Guid createdBy, DateTimeOffset createdAt, Guid? workspaceId = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(planId);

        if (start >= end)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_PeriodStartMustBeBeforeEnd, "Subscription period start must be before end.");

        var subscription = new Subscription
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            PlanId = planId,
            Status = SubscriptionStatus.Active,
            Tier = tier,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = end
        };

        subscription.SetAuditOnCreate(createdBy, createdAt);
        subscription.RaiseDomainEvent(new SubscriptionStartedDomainEvent(accountId, workspaceId, subscription.Id, planId, createdAt));
        return subscription;
    }

    public void ChangePlan(Guid newPlanId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(newPlanId);

        if (Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_CannotChangePlanOfInactive, "Cannot change plan of an inactive subscription.");

        var oldPlanId = PlanId;
        PlanId = newPlanId;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionChangedDomainEvent(AccountId, WorkspaceId, Id, oldPlanId, newPlanId, updatedAt));
    }

    public void ScheduleCancellation(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (CancelAtPeriodEnd) return;
        CancelAtPeriodEnd = true;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionCancellationScheduledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void CancelImmediately(Guid updatedBy, DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status == SubscriptionStatus.Canceled) return;
        Status = SubscriptionStatus.Canceled;
        SetAuditOnUpdate(updatedBy, cancelledAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionCanceledDomainEvent(AccountId, WorkspaceId, Id, updatedBy, cancelledAt));
    }

    public void Renew(DateTimeOffset newStart, DateTimeOffset newEnd, Guid updatedBy, DateTimeOffset renewedAt)
    {
        EnsureNotDeleted();
        if (newStart >= newEnd)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Subscription_PeriodStartMustBeBeforeEnd, "Renewal period start must be before end.");

        CurrentPeriodStart = newStart;
        CurrentPeriodEnd = newEnd;
        CancelAtPeriodEnd = false;
        Status = SubscriptionStatus.Active;
        SetAuditOnUpdate(updatedBy, renewedAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionRenewedDomainEvent(AccountId, WorkspaceId, Id, newStart, newEnd, renewedAt));
    }

    public void Expire(Guid updatedBy, DateTimeOffset expiredAt)
    {
        EnsureNotDeleted();
        if (Status == SubscriptionStatus.Expired) return;
        Status = SubscriptionStatus.Expired;
        SetAuditOnUpdate(updatedBy, expiredAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionExpiredDomainEvent(AccountId, WorkspaceId, Id, expiredAt));
    }

    public void MarkPastDue(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (Status == SubscriptionStatus.PastDue) return;
        Status = SubscriptionStatus.PastDue;
        SetAuditOnUpdate(updatedBy, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionPastDueDomainEvent(AccountId, WorkspaceId, Id, occurredAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new SubscriptionRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
