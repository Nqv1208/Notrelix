namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-source-updated")]
public sealed record DashboardSourceUpdatedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid DashboardId { get; }
    public Guid SourceId { get; }

    public DashboardSourceUpdatedDomainEvent(
        Guid accountId, Guid workspaceId, Guid dashboardId, Guid sourceId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        DashboardId = dashboardId;
        SourceId = sourceId;
    }
}
