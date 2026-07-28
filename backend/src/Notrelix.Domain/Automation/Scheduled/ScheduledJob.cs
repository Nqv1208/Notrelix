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
        var pending = PrepareAuditUpdate(null, updatedAt);
        Status = ScheduledJobStatus.Paused;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ScheduledJobPausedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
    }

    public void Resume(DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status != ScheduledJobStatus.Paused) return;
        var pending = PrepareAuditUpdate(null, updatedAt);
        Status = ScheduledJobStatus.Active;
        ApplyAuditUpdate(pending);
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Cancelled) return;
        var pending = PrepareAuditUpdate(null, cancelledAt);
        Status = ScheduledJobStatus.Cancelled;
        ApplyAuditUpdate(pending);
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Completed) return;
        if (Status == ScheduledJobStatus.Cancelled || Status == ScheduledJobStatus.Failed)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ScheduledJob_CannotCompleteFromStatus, $"Cannot complete a job in '{Status}' status.");
        var pending = PrepareAuditUpdate(null, completedAt);
        Status = ScheduledJobStatus.Completed;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ScheduledJobCompletedDomainEvent(AccountId, WorkspaceId, Id, completedAt));
    }

    public void Fail(string reason, DateTimeOffset failedAt)
    {
        EnsureNotDeleted();
        if (Status == ScheduledJobStatus.Failed) return;
        if (Status == ScheduledJobStatus.Completed || Status == ScheduledJobStatus.Cancelled)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ScheduledJob_CannotFailFromStatus, $"Cannot fail a job in '{Status}' status.");
        var pending = PrepareAuditUpdate(null, failedAt);
        Status = ScheduledJobStatus.Failed;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ScheduledJobFailedDomainEvent(AccountId, WorkspaceId, Id, reason, failedAt));
    }

    public void MarkRunCompleted(DateTimeOffset nextRunAt, DateTimeOffset completedAt)
    {
        EnsureNotDeleted();
        var pending = PrepareAuditUpdate(null, completedAt);
        LastRunAt = completedAt;
        NextRunAt = nextRunAt;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ScheduledJobRunCompletedDomainEvent(AccountId, WorkspaceId, Id, completedAt, nextRunAt));
    }

    public void UpdateSchedule(ScheduleDefinition newSchedule, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newSchedule);
        var pending = PrepareAuditUpdate(null, updatedAt);
        Schedule = newSchedule;
        ApplyAuditUpdate(pending);
        RaiseDomainEvent(new ScheduledJobUpdatedDomainEvent(AccountId, WorkspaceId, Id, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        EnsureNotDeleted();
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        RaiseDomainEvent(new ScheduledJobSoftDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        RaiseDomainEvent(new ScheduledJobRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredAt));
    }
}
