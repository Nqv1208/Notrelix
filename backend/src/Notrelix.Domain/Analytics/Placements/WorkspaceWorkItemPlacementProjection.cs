namespace Notrelix.Domain.Analytics.Placements;

/// <summary>
/// Analytics-owned derived state: the latest known current placement of one
/// Work Management item, keyed by WorkspaceId + ItemId. This is an
/// Analytics read model for placement queries — never Work source truth, and
/// never a Billing/security authority.
/// </summary>
public class WorkspaceWorkItemPlacementProjection : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid GroupId { get; private set; }
    public bool IsArchived { get; private set; }
    public long SourceRevision { get; private set; }
    public DateTimeOffset LastOccurredAt { get; private set; }

    private WorkspaceWorkItemPlacementProjection() { }

    /// <summary>
    /// Creates or re-derives the projection from a producer-owned snapshot
    /// fact. Last-write-wins by producer revision: a snapshot older than the
    /// currently known revision is ignored so out-of-order delivery cannot
    /// regress the projection.
    /// </summary>
    public static WorkspaceWorkItemPlacementProjection Upsert(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        Guid groupId,
        bool isArchived,
        long sourceRevision,
        DateTimeOffset lastOccurredAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(itemId);

        return new WorkspaceWorkItemPlacementProjection
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ItemId = itemId,
            BoardId = boardId,
            GroupId = groupId,
            IsArchived = isArchived,
            SourceRevision = sourceRevision,
            LastOccurredAt = lastOccurredAt,
        };
    }

    /// <summary>
    /// Applies a newer producer fact. Returns false and changes nothing when
    /// the incoming revision is not newer (duplicate or stale delivery).
    /// </summary>
    public bool ApplyNewer(
        Guid boardId,
        Guid groupId,
        bool isArchived,
        long sourceRevision,
        DateTimeOffset lastOccurredAt)
    {
        if (sourceRevision <= SourceRevision)
            return false;

        BoardId = boardId;
        GroupId = groupId;
        IsArchived = isArchived;
        SourceRevision = sourceRevision;
        LastOccurredAt = lastOccurredAt;
        return true;
    }

    /// <summary>
    /// Reconciliation path: replaces local state from a producer-owned
    /// snapshot regardless of revision (rebuild after drift).
    /// </summary>
    public void Reconcile(
        Guid boardId,
        Guid groupId,
        bool isArchived,
        long sourceRevision,
        DateTimeOffset lastOccurredAt)
    {
        BoardId = boardId;
        GroupId = groupId;
        IsArchived = isArchived;
        SourceRevision = sourceRevision;
        LastOccurredAt = lastOccurredAt;
    }
}
