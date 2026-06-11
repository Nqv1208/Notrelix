using Notrelix.Domain.Common;

namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardWidget : Entity
{
    public Guid DashboardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public JsonValue Config { get; private set; } = null!;
    public int Position { get; private set; }

    private DashboardWidget() : base() { }

    public static DashboardWidget Create(Guid dashboardId, string title, string type, JsonValue config, int position)
    {
        return new DashboardWidget
        {
            DashboardId = dashboardId,
            Title = title,
            Type = type,
            Config = config,
            Position = position
        };
    }
}

public class Dashboard : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsPublic { get; private set; }

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

    private Dashboard() : base() { }

    public static Dashboard Create(Guid workspaceId, string name)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var dashboard = new Dashboard
        {
            WorkspaceId = workspaceId,
            Name = name.Trim()
        };

        dashboard.SetAuditOnCreate(Guid.Empty); // System or Actor
        return dashboard;
    }
}
