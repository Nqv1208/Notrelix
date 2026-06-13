using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Workload;

public sealed class WorkloadCapacity : ValueObject
{
    public int DailyMinutes { get; }

    private WorkloadCapacity() { }    private WorkloadCapacity(int dailyMinutes)
    {
        DailyMinutes = dailyMinutes;
    }

    public static WorkloadCapacity Create(int dailyMinutes)
    {
        Guard.Positive(dailyMinutes);
        return new WorkloadCapacity(dailyMinutes);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DailyMinutes;
    }
}
