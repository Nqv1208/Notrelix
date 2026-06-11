using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Scheduled;

public class ScheduledJob : AggregateRoot
{
    public Guid RuleId { get; private set; }
    public ScheduleDefinition Schedule { get; private set; } = null!;
    public ScheduledJobStatus Status { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }

    private ScheduledJob() : base() { }

    public static ScheduledJob Create(Guid ruleId, ScheduleDefinition schedule)
    {
        Guard.NotEmpty(ruleId);
        Guard.NotNull(schedule);

        return new ScheduledJob
        {
            RuleId = ruleId,
            Schedule = schedule,
            Status = ScheduledJobStatus.Active
        };
    }

    public void Pause()
    {
        Status = ScheduledJobStatus.Paused;
    }

    public void Resume()
    {
        Status = ScheduledJobStatus.Active;
    }
}
