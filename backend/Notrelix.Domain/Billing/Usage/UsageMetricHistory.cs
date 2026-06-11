using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Usage;

public class UsageMetricHistory : Entity
{
    public Guid MetricId { get; private set; }
    public int Increment { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    private UsageMetricHistory() : base() { }

    public static UsageMetricHistory Create(Guid metricId, int increment, DateTimeOffset timestamp)
    {
        return new UsageMetricHistory
        {
            MetricId = metricId,
            Increment = increment,
            Timestamp = timestamp
        };
    }
}
