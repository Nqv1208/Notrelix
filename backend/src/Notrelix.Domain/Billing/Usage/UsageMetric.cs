using Notrelix.Domain.Billing.Usage.Events;
using Notrelix.Domain.Billing.Rules;
using static Notrelix.Domain.Billing.BillingRuleCodes;

namespace Notrelix.Domain.Billing.Usage;

public class UsageMetric : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public UsageMetricKey Key { get; private set; } = null!;
    public int CurrentValue { get; private set; }
    public UsagePeriod CurrentPeriod { get; private set; } = null!;

    private readonly List<UsageMetricHistory> _history = new();
    public IReadOnlyCollection<UsageMetricHistory> History => _history.AsReadOnly();

    private UsageMetric() : base() { }

    public static UsageMetric Create(Guid accountId, Guid workspaceId, UsageMetricKey key, UsagePeriod period, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(key);
        Guard.NotNull(period);

        var metric = new UsageMetric
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Key = key,
            CurrentValue = 0,
            CurrentPeriod = period
        };

        metric.RaiseDomainEvent(new UsageMetricCreatedDomainEvent(accountId, workspaceId, key, createdAt));
        return metric;
    }

    public void Increase(int amount, int limit, bool isHardLimit, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.Positive(amount);

        if (CurrentValue + amount > limit)
        {
            RaiseDomainEvent(new UsageLimitExceededDomainEvent(AccountId, WorkspaceId, Key, occurredAt));
            UsageRules.EnsureCanIncrease(CurrentValue, amount, limit, isHardLimit);
        }

        CurrentValue += amount;
        _history.Add(UsageMetricHistory.Create(Id, amount, occurredAt));
        RaiseDomainEvent(new UsageMetricIncreasedDomainEvent(AccountId, WorkspaceId, Key, amount, occurredAt));
    }

    public void Decrease(int amount, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.Positive(amount);

        if (CurrentValue - amount < 0)
        {
            throw new BusinessRuleException(Billing_Usage_ValueCannotBeNegative, "Usage value cannot be negative.");
        }

        CurrentValue -= amount;
        _history.Add(UsageMetricHistory.Create(Id, -amount, occurredAt));
        IncrementVersion();
        RaiseDomainEvent(new UsageMetricDecreasedDomainEvent(AccountId, WorkspaceId, Key, amount, occurredAt));
    }

    public void Reset(UsagePeriod newPeriod, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPeriod);

        CurrentValue = 0;
        CurrentPeriod = newPeriod;
        IncrementVersion();
        RaiseDomainEvent(new UsageMetricResetDomainEvent(AccountId, WorkspaceId, Key, occurredAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new UsageMetricSoftDeletedDomainEvent(AccountId, WorkspaceId, Key, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new UsageMetricRestoredDomainEvent(AccountId, WorkspaceId, Key, restoredBy, restoredAt));
    }
}
