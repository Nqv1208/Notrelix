namespace Notrelix.Domain.WorkManagement.Items;

public class TimeTrackingEntry : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public TimeTrackingStatus Status { get; private set; } = TimeTrackingStatus.Running;
    public string? Note { get; private set; }

    public int? DurationSeconds
    {
        get
        {
            if (EndedAt == null) return null;
            var diff = (int)(EndedAt.Value - StartedAt).TotalSeconds;
            return diff < 0 ? 0 : diff;
        }
    }

    private TimeTrackingEntry() : base() { }

    public static TimeTrackingEntry Start(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid userId,
        DateTimeOffset startedAt,
        string? note = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);

        var entry = new TimeTrackingEntry
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            UserId = userId,
            StartedAt = startedAt,
            Status = TimeTrackingStatus.Running,
            Note = note
        };

        entry.SetAuditOnCreate(userId, startedAt);
        return entry;
    }

    public void Stop(DateTimeOffset endedAt, Guid stoppedBy)
    {
        EnsureNotDeleted();
        if (Status != TimeTrackingStatus.Running)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_TimeTracking_CannotStopNotRunning, "Cannot stop a timer that is not running.");

        if (endedAt < StartedAt)
            throw new BusinessRuleException(BusinessRuleCodes.WorkManagement_TimeTracking_EndTimeMustBeAfterStart, "End time must be after start time.");

        EndedAt = endedAt;
        Status = TimeTrackingStatus.Stopped;
        SetAuditOnUpdate(stoppedBy, endedAt);
        IncrementVersion();
    }
}
