using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Rules;

namespace Notrelix.Domain.Billing.Usage;

public class UsageMetric : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public UsageMetricKey Key { get; private set; } = null!;
    public int CurrentValue { get; private set; }
    public UsagePeriod CurrentPeriod { get; private set; } = null!;

    private readonly List<UsageMetricHistory> _history = new();
    public IReadOnlyCollection<UsageMetricHistory> History => _history.AsReadOnly();

    private UsageMetric() : base() { }

    public static UsageMetric Create(Guid workspaceId, UsageMetricKey key, UsagePeriod period)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(key);
        Guard.NotNull(period);

        return new UsageMetric
        {
            WorkspaceId = workspaceId,
            Key = key,
            CurrentValue = 0,
            CurrentPeriod = period
        };
    }

    public void Increase(int amount, int limit, bool isHardLimit, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.Positive(amount);

        if (CurrentValue + amount > limit)
        {
            AddDomainEvent(new UsageLimitExceededEvent(WorkspaceId, Key, occurredAt));
            UsageRules.EnsureCanIncrease(CurrentValue, amount, limit, isHardLimit);
        }

        CurrentValue += amount;
        _history.Add(UsageMetricHistory.Create(Id, amount, occurredAt));
        AddDomainEvent(new UsageMetricIncreasedEvent(WorkspaceId, Key, amount, occurredAt));
    }

    public void Decrease(int amount, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.Positive(amount);

        if (CurrentValue - amount < 0)
        {
            throw new DomainException("Usage value cannot be negative.");
        }

        CurrentValue -= amount;
        _history.Add(UsageMetricHistory.Create(Id, -amount, occurredAt));
    }

    public void Reset(UsagePeriod newPeriod, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newPeriod);

        CurrentValue = 0;
        CurrentPeriod = newPeriod;
    }
}
