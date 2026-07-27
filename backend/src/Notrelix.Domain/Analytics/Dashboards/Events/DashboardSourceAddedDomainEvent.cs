namespace Notrelix.Domain.Analytics.Dashboards.Events;

[EventName("analytics.dashboard-source-added")]
public sealed record DashboardSourceAddedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid DashboardId { get; }
    public Guid SourceId { get; }

    public DashboardSourceAddedDomainEvent(
        Guid accountId, Guid workspaceId, Guid dashboardId, Guid sourceId,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        DashboardId = dashboardId;
        SourceId = sourceId;
    }
}
