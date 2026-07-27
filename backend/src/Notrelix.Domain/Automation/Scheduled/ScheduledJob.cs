using Notrelix.Domain.Automation.Scheduled.Events;
namespace Notrelix.Domain.Automation.Scheduled;

public class ScheduledJob : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RuleId { get; private set; }
    public ScheduleDefinition Schedule { get; private set; } = null!;
    public ScheduledJobStatus Status { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }

    private ScheduledJob() : base() { }

    public static ScheduledJob Create(Guid accountId, Guid workspaceId, Guid ruleId, ScheduleDefinition schedule, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(ruleId);
        Guard.NotNull(schedule);

        var job = new ScheduledJob
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            RuleId = ruleId,
            Schedule = schedule,
            Status = ScheduledJobStatus.Active,
            NextRunAt = createdAt
        };

        job.SetAuditOnCreate(null, createdAt);
        job.RaiseDomainEvent(new ScheduledJobCreatedDomainEvent(accountId, workspaceId, job.Id, ruleId, createdAt));
        return job;
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Paused) return;
        Status = ScheduledJobStatus.Paused;
        SetAuditOnUpdate(null, updatedAt);
        RaiseDomainEvent(new ScheduledJobPausedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
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
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ScheduledJob_CannotCompleteFromStatus, $"Cannot complete a job in '{Status}' status.");
        Status = ScheduledJobStatus.Completed;
        SetAuditOnUpdate(null, completedAt);
        RaiseDomainEvent(new ScheduledJobCompletedDomainEvent(AccountId, WorkspaceId, Id, completedAt));
    }

    public void Fail(string reason, DateTimeOffset failedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Failed) return;
        if (Status == ScheduledJobStatus.Completed || Status == ScheduledJobStatus.Cancelled)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ScheduledJob_CannotFailFromStatus, $"Cannot fail a job in '{Status}' status.");
        Status = ScheduledJobStatus.Failed;
        SetAuditOnUpdate(null, failedAt);
        RaiseDomainEvent(new ScheduledJobFailedDomainEvent(AccountId, WorkspaceId, Id, reason, failedAt));
    }

    public void MarkRunCompleted(DateTimeOffset nextRunAt, DateTimeOffset completedAt)
    {
        EnsureNotDeleted();
        LastRunAt = completedAt;
        NextRunAt = nextRunAt;
        SetAuditOnUpdate(null, completedAt);
        RaiseDomainEvent(new ScheduledJobRunCompletedDomainEvent(AccountId, WorkspaceId, Id, completedAt, nextRunAt));
    }

    public void UpdateSchedule(ScheduleDefinition newSchedule, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newSchedule);
        Schedule = newSchedule;
        SetAuditOnUpdate(null, updatedAt);
        RaiseDomainEvent(new ScheduledJobUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        EnsureNotDeleted();
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        RaiseDomainEvent(new ScheduledJobSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        RaiseDomainEvent(new ScheduledJobRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredAt));
    }
}
