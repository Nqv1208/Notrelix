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
            Status = ScheduledJobStatus.Active,
            NextRunAt = createdAt
        };

        job.SetAuditOnCreate(null, createdAt);
        job.AddDomainEvent(new ScheduledJobCreatedDomainEvent(workspaceId, job.Id, ruleId, createdAt));
        return job;
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Paused) return;
        Status = ScheduledJobStatus.Paused;
        SetAuditOnUpdate(null, updatedAt);
        AddDomainEvent(new ScheduledJobPausedDomainEvent(WorkspaceId, Id, updatedAt));
    }

    public void Resume(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status != ScheduledJobStatus.Paused) return;
        Status = ScheduledJobStatus.Active;
        SetAuditOnUpdate(null, updatedAt);
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Cancelled) return;
        Status = ScheduledJobStatus.Cancelled;
        SetAuditOnUpdate(null, cancelledAt);
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Completed) return;
        if (Status == ScheduledJobStatus.Cancelled || Status == ScheduledJobStatus.Failed)
            throw new BusinessRuleException($"Cannot complete a job in '{Status}' status.");
        Status = ScheduledJobStatus.Completed;
        SetAuditOnUpdate(null, completedAt);
        AddDomainEvent(new ScheduledJobCompletedDomainEvent(WorkspaceId, Id, completedAt));
    }

    public void Fail(string reason, DateTimeOffset failedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Failed) return;
        if (Status == ScheduledJobStatus.Completed || Status == ScheduledJobStatus.Cancelled)
            throw new BusinessRuleException($"Cannot fail a job in '{Status}' status.");
        Status = ScheduledJobStatus.Failed;
        SetAuditOnUpdate(null, failedAt);
        AddDomainEvent(new ScheduledJobFailedDomainEvent(WorkspaceId, Id, reason, failedAt));
    }

    public void MarkRunCompleted(DateTimeOffset nextRunAt, DateTimeOffset completedAt)
    {
        EnsureNotDeleted();
        LastRunAt = completedAt;
        NextRunAt = nextRunAt;
        SetAuditOnUpdate(null, completedAt);
        AddDomainEvent(new ScheduledJobRunCompletedDomainEvent(WorkspaceId, Id, completedAt, nextRunAt));
    }

    public void UpdateSchedule(ScheduleDefinition newSchedule, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newSchedule);
        Schedule = newSchedule;
        SetAuditOnUpdate(null, updatedAt);
        AddDomainEvent(new ScheduledJobUpdatedDomainEvent(WorkspaceId, Id, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        EnsureNotDeleted();
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new ScheduledJobSoftDeletedDomainEvent(WorkspaceId, Id, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        AddDomainEvent(new ScheduledJobRestoredDomainEvent(WorkspaceId, Id, restoredAt));
    }
}
