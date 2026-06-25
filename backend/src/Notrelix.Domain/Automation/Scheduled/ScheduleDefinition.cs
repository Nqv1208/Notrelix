namespace Notrelix.Domain.Automation.Scheduled;

public sealed class ScheduleDefinition : ValueObject
{
    public string CronExpression { get; }
    public string TimeZone { get; }

    private ScheduleDefinition() { }
    private ScheduleDefinition(string cronExpression, string timeZone)
    {
        CronExpression = cronExpression;
        TimeZone = timeZone;
    }

    public static ScheduleDefinition Create(string cronExpression, string timeZone = "UTC")
    {
        Guard.NotNullOrWhiteSpace(cronExpression);
        return new ScheduleDefinition(cronExpression, timeZone);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CronExpression;
        yield return TimeZone;
    }
}
