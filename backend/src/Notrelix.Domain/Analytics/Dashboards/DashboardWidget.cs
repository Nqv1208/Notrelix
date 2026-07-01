using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardWidget : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid DashboardId { get; private set; }
    public string Title { get; private set; } = null!;
    public DashboardWidgetType Type { get; private set; }
    public JsonValue Config { get; private set; } = null!;
    public WidgetPosition Position { get; private set; } = null!;

    private DashboardWidget() : base() { }

    public static DashboardWidget Create(Guid dashboardId, string title, DashboardWidgetType type, JsonValue config, WidgetPosition position)
    {
        Guard.NotEmpty(dashboardId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(config);

        var (isValid, error) = WidgetConfigValidator.Validate(type, config);
        if (!isValid)
            throw new BusinessRuleException($"Invalid widget config: {error}");

        WidgetRules.ValidatePosition(position);

        return new DashboardWidget
        {
            DashboardId = dashboardId,
            Title = title,
            Type = type,
            Config = config,
            Position = position
        };
    }

    public void UpdatePosition(WidgetPosition newPosition)
    {
        WidgetRules.ValidatePosition(newPosition);
        Position = newPosition;
    }

    public void UpdateTitle(string title, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(title);
        Title = title.Trim();
    }
}
