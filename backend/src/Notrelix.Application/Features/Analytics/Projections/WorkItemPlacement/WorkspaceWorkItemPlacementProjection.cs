namespace Notrelix.Application.Features.Analytics.Projections.WorkItemPlacement;

/// <summary>
/// Analytics-owned derived state: the latest known current placement of one
/// Work Management item, keyed by WorkspaceId + ItemId. This is an
/// Analytics read model for placement queries — never Work source truth, and
/// never a Billing/security authority.
/// </summary>
public class WorkspaceWorkItemPlacementProjection
{
    public Guid Id { get; private set; }
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
    /// Canonical ordering watermark: the producer timestamp of the projected
    /// fact. Live delivery and rebuild snapshots share this single scale —
    /// aggregate versions are never mixed into it.
    /// </summary>
    public static long WatermarkOf(DateTimeOffset lastOccurredAt) => lastOccurredAt.UtcTicks;

    /// <summary>
    /// Creates or re-derives the projection from a producer-owned snapshot
    /// fact at the producer-timestamp watermark.
    /// </summary>
    public static WorkspaceWorkItemPlacementProjection Upsert(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        Guid groupId,
        bool isArchived,
        DateTimeOffset lastOccurredAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(itemId);

        return new WorkspaceWorkItemPlacementProjection
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ItemId = itemId,
            BoardId = boardId,
            GroupId = groupId,
            IsArchived = isArchived,
            SourceRevision = WatermarkOf(lastOccurredAt),
            LastOccurredAt = lastOccurredAt,
        };
    }

    /// <summary>
    /// Applies a newer producer fact. Returns false and changes nothing when
    /// the incoming watermark is not strictly newer (duplicate or stale
    /// delivery), so out-of-order live facts cannot regress the projection.
    /// </summary>
    public bool ApplyNewer(
        Guid boardId,
        Guid groupId,
        bool isArchived,
        DateTimeOffset lastOccurredAt)
    {
        var watermark = WatermarkOf(lastOccurredAt);
        if (watermark <= SourceRevision)
            return false;

        BoardId = boardId;
        GroupId = groupId;
        IsArchived = isArchived;
        SourceRevision = watermark;
        LastOccurredAt = lastOccurredAt;
        return true;
    }

    /// <summary>
    /// Reconciliation path for rebuild: applies a producer snapshot whose
    /// watermark is at or ahead of the local state (equal watermarks are the
    /// authorized drift-repair window). A strictly older snapshot is refused —
    /// a live fact that arrived after the snapshot was taken must not be
    /// overwritten. Returns true when local state was replaced.
    /// </summary>
    public bool Reconcile(
        Guid boardId,
        Guid groupId,
        bool isArchived,
        DateTimeOffset lastOccurredAt)
    {
        var watermark = WatermarkOf(lastOccurredAt);
        if (watermark < SourceRevision)
            return false;

        BoardId = boardId;
        GroupId = groupId;
        IsArchived = isArchived;
        SourceRevision = watermark;
        LastOccurredAt = lastOccurredAt;
        return true;
    }
}