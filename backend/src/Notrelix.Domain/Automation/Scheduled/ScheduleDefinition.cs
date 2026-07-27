namespace Notrelix.Domain.Automation.Scheduled;

public sealed class ScheduleDefinition : ValueObject
{
    public string CronExpression { get; private set; } = null!;
    public string TimeZone { get; private set; } = null!;
    public int SchemaVersion { get; private set; }

    private ScheduleDefinition() { }
    private ScheduleDefinition(string cronExpression, string timeZone, int schemaVersion)
    {
        CronExpression = cronExpression;
        TimeZone = timeZone;
        SchemaVersion = schemaVersion;
    }

    public static ScheduleDefinition Create(string cronExpression, string timeZone = "UTC")
    {
        Guard.NotNullOrWhiteSpace(cronExpression);
        return new ScheduleDefinition(cronExpression, timeZone, 1);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CronExpression;
        yield return TimeZone;
        yield return SchemaVersion;
    }
}
