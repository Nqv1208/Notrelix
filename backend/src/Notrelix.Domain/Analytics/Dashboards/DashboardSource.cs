namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardSource : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid DashboardId { get; private set; }
    public DashboardSourceType SourceType { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? BoardViewId { get; private set; }
    public JsonValue Filter { get; private set; } = null!;

    private DashboardSource() : base() { }

    public static DashboardSource Create(
        Guid workspaceId,
        Guid dashboardId,
        DashboardSourceType sourceType,
        Guid? boardId,
        Guid? boardViewId,
        JsonValue filter,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(dashboardId);
        Guard.NotNull(filter);

        if (sourceType == DashboardSourceType.Board || sourceType == DashboardSourceType.BoardView)
        {
            Guard.NotEmpty(boardId ?? Guid.Empty, nameof(boardId));
        }
        if (sourceType == DashboardSourceType.BoardView)
        {
            Guard.NotEmpty(boardViewId ?? Guid.Empty, nameof(boardViewId));
        }

        var source = new DashboardSource
        {
            WorkspaceId = workspaceId,
            DashboardId = dashboardId,
            SourceType = sourceType,
            BoardId = boardId,
            BoardViewId = boardViewId,
            Filter = filter
        };

        source.SetAuditOnCreate(createdBy, createdAt);
        source.AddDomainEvent(new DashboardSourceAddedDomainEvent(workspaceId, dashboardId, source.Id, createdBy, createdAt));
        return source;
    }

    public void UpdateFilter(JsonValue newFilter, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newFilter);

        Filter = newFilter;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardSourceUpdatedDomainEvent(WorkspaceId, DashboardId, Id, updatedBy, updatedAt));
    }
}
