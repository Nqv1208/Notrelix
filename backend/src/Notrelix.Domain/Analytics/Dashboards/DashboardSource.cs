using Notrelix.Domain.Analytics.Dashboards.Events;
namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardSource : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid DashboardId { get; private set; }
    public DashboardSourceType SourceType { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? BoardViewId { get; private set; }
    public JsonValue Filter { get; private set; } = null!;

    private DashboardSource() : base() { }

    public static DashboardSource Create(
        Guid accountId,
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
        Guard.NotEmpty(accountId);

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
            AccountId = accountId,
            WorkspaceId = workspaceId,
            DashboardId = dashboardId,
            SourceType = sourceType,
            BoardId = boardId,
            BoardViewId = boardViewId,
            Filter = filter
        };

        source.SetAuditOnCreate(createdBy, createdAt);
        source.RaiseDomainEvent(new DashboardSourceAddedDomainEvent(accountId, workspaceId, dashboardId, source.Id, createdAt));
        return source;
    }

    public void UpdateFilter(JsonValue newFilter, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNull(newFilter);
        Guard.NotEmpty(updatedBy);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Filter = newFilter;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardSourceUpdatedDomainEvent(AccountId, WorkspaceId, DashboardId, Id, updatedAt));
    }
}
