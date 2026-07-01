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
            throw new BusinessRuleException("Current usage cannot be negative.");

        if (hardLimit < 0)
            throw new BusinessRuleException("Hard limit cannot be negative.");

        if (softLimit < 0)
            throw new BusinessRuleException("Soft limit cannot be negative.");

        if (softLimit.HasValue && hardLimit.HasValue && softLimit > hardLimit)
            throw new BusinessRuleException("Soft limit cannot exceed hard limit.");

        if (!overageAllowed && hardLimit.HasValue && currentUsage > hardLimit.Value)
            throw new BusinessRuleException("Current usage exceeds hard limit and overage is not allowed.");

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

        usage.AddDomainEvent(new WorkspaceFeatureUsageInitializedDomainEvent(
            accountId, workspaceId, feature, currentUsage, hardLimit, softLimit, createdAt));
        return usage;
    }

    public void Consume(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (amount <= 0)
            throw new BusinessRuleException("Amount to consume must be positive.");

        if (HardLimit.HasValue && !OverageAllowed && CurrentUsage + amount > HardLimit.Value)
        {
            AddDomainEvent(new QuotaExceededDomainEvent(AccountId, WorkspaceId, Feature.Code, HardLimit.Value, occurredAt));
            throw new BusinessRuleException($"Feature usage limit exceeded for '{Feature.Code}'. Limit: {HardLimit.Value}, Requested total: {CurrentUsage + amount}.");
        }

        var oldUsage = CurrentUsage;
        CurrentUsage += amount;
        SetAuditOnUpdate(actorUserId, occurredAt);
        IncrementVersion();

        AddDomainEvent(new FeatureUsageConsumedDomainEvent(AccountId, WorkspaceId, Feature.Code, amount, actorUserId, occurredAt));
    }

    public void Release(decimal amount, Guid actorUserId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (amount <= 0)
            throw new BusinessRuleException("Amount to release must be positive.");

        if (CurrentUsage - amount < 0)
            throw new BusinessRuleException("Usage cannot be released below zero.");

        CurrentUsage -= amount;
        SetAuditOnUpdate(actorUserId, occurredAt);
        IncrementVersion();

        AddDomainEvent(new FeatureUsageReleasedDomainEvent(AccountId, WorkspaceId, Feature.Code, amount, actorUserId, occurredAt));
    }

    public void Reset(DateTimeOffset resetAt, Guid actorUserId)
    {
        EnsureNotDeleted();
        CurrentUsage = 0;
        LastResetAt = resetAt;
        SetAuditOnUpdate(actorUserId, resetAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceFeatureUsageResetDomainEvent(AccountId, WorkspaceId, Feature, resetAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        IncrementVersion();
        AddDomainEvent(new WorkspaceFeatureUsageSoftDeletedDomainEvent(AccountId, WorkspaceId, Feature, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceFeatureUsageRestoredDomainEvent(AccountId, WorkspaceId, Feature, restoredBy, restoredAt));
    }
}
