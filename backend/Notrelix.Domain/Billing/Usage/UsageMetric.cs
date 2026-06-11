using Notrelix.Domain.Common;

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

    public void Increase(int amount, DateTimeOffset occurredAt)
    {
         CurrentValue += amount;
         _history.Add(UsageMetricHistory.Create(Id, amount, occurredAt));
         AddDomainEvent(new UsageMetricIncreasedEvent(WorkspaceId, Key, amount, occurredAt));
    }
}
