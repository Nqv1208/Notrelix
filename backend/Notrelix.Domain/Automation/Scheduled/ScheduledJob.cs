using Notrelix.Domain.Common;

namespace Notrelix.Domain.Automation.Scheduled;

public class ScheduledJob : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid RuleId { get; private set; }
    public ScheduleDefinition Schedule { get; private set; } = null!;
    public ScheduledJobStatus Status { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }

    private ScheduledJob() : base() { }

    public static ScheduledJob Create(Guid workspaceId, Guid ruleId, ScheduleDefinition schedule, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(ruleId);
        Guard.NotNull(schedule);

        var job = new ScheduledJob
        {
            WorkspaceId = workspaceId,
            RuleId = ruleId,
            Schedule = schedule,
            Status = ScheduledJobStatus.Active
        };

        job.SetAuditOnCreate(null, createdAt);
        job.AddDomainEvent(new ScheduledJobCreatedEvent(workspaceId, job.Id, ruleId, createdAt));
        return job;
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        if (Status == ScheduledJobStatus.Paused) return;
        Status = ScheduledJobStatus.Paused;
        SetAuditOnUpdate(null, updatedAt);
        AddDomainEvent(new ScheduledJobPausedEvent(WorkspaceId, Id, updatedAt));
    }

    public void Resume(DateTimeOffset updatedAt)
    {
        if (Status != ScheduledJobStatus.Paused) return;
        Status = ScheduledJobStatus.Active;
        SetAuditOnUpdate(null, updatedAt);
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status == ScheduledJobStatus.Cancelled) return;
        Status = ScheduledJobStatus.Cancelled;
        SetAuditOnUpdate(Guid.Empty, cancelledAt);
    }
}
