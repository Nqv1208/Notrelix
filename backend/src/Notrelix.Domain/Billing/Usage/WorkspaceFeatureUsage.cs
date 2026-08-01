using Notrelix.Domain.Billing.Usage.Events;
using Notrelix.Domain.Billing.Plans;
namespace Notrelix.Domain.Billing.Usage;

public class WorkspaceFeatureUsage : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public FeatureCode Feature { get; private set; } = null!;
    public decimal CurrentUsage { get; private set; }
    public decimal? HardLimit { get; private set; }
    public decimal? SoftLimit { get; private set; }
    public bool OverageAllowed { get; private set; }
    public string ResetPeriod { get; private set; } = "None";
    public DateTimeOffset? LastResetAt { get; private set; }

    private WorkspaceFeatureUsage() : base() { }

    public static WorkspaceFeatureUsage Create(
        Guid accountId,
        Guid workspaceId,
        FeatureCode feature,
        decimal currentUsage,
        decimal? hardLimit,
        decimal? softLimit,
        DateTimeOffset createdAt,
        bool overageAllowed = false,
        string resetPeriod = "None")
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(feature);

        if (currentUsage < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_CurrentCannotBeNegative, "Current usage cannot be negative.");

        if (hardLimit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_HardLimitCannotBeNegative, "Hard limit cannot be negative.");

        if (softLimit < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_SoftLimitCannotBeNegative, "Soft limit cannot be negative.");

        if (softLimit.HasValue && hardLimit.HasValue && softLimit > hardLimit)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_SoftLimitCannotExceedHard, "Soft limit cannot exceed hard limit.");

        if (!overageAllowed && hardLimit.HasValue && currentUsage > hardLimit.Value)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_ExceedsHardLimitNoOverage, "Current usage exceeds hard limit and overage is not allowed.");

        var usage = new WorkspaceFeatureUsage
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Feature = feature,
            CurrentUsage = currentUsage,
            HardLimit = hardLimit,
            SoftLimit = softLimit,
            OverageAllowed = overageAllowed,
            ResetPeriod = resetPeriod
        };

        usage.RaiseDomainEvent(new WorkspaceFeatureUsageInitializedDomainEvent(
            accountId, workspaceId, feature, currentUsage, hardLimit, softLimit, createdAt));
        return usage;
    }

    public void Consume(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        if (amount <= 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_ConsumeAmountMustBePositive, "Amount to consume must be positive.");

        if (HardLimit.HasValue && !OverageAllowed && CurrentUsage + amount > HardLimit.Value)
        {
            RaiseDomainEvent(new QuotaExceededDomainEvent(AccountId, WorkspaceId, Feature.Code, HardLimit.Value, occurredAt));
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded, $"Feature usage limit exceeded for '{Feature.Code}'. Limit: {HardLimit.Value}, Requested total: {CurrentUsage + amount}.");
        }

        var oldUsage = CurrentUsage;
        var pending = PrepareAuditUpdate(actorUserId, occurredAt);
        CurrentUsage += amount;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new FeatureUsageConsumedDomainEvent(AccountId, WorkspaceId, Feature.Code, amount, occurredAt));
    }

    public void Release(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        if (amount <= 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_ReleaseAmountMustBePositive, "Amount to release must be positive.");

        if (CurrentUsage - amount < 0)
            throw new BusinessRuleException(BillingRuleCodes.Billing_Usage_CannotReleaseBelowZero, "Usage cannot be released below zero.");

        var pending = PrepareAuditUpdate(actorUserId, occurredAt);
        CurrentUsage -= amount;
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new FeatureUsageReleasedDomainEvent(AccountId, WorkspaceId, Feature.Code, amount, occurredAt));
    }

    public void Reset(DateTimeOffset resetAt, Guid actorUserId)
    {
        var pending = PrepareAuditUpdate(actorUserId, resetAt);
        CurrentUsage = 0;
        LastResetAt = resetAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceFeatureUsageResetDomainEvent(AccountId, WorkspaceId, Feature, resetAt));
    }
}
