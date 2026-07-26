using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardWidget : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid DashboardId { get; private set; }
    public string Title { get; private set; } = null!;
    public DashboardWidgetType Type { get; private set; }
    public JsonValue Config { get; private set; } = null!;
    public WidgetPosition Position { get; private set; } = null!;

    private DashboardWidget() : base() { }

    public static DashboardWidget Create(Guid accountId, Guid workspaceId, Guid dashboardId, string title, DashboardWidgetType type, JsonValue config, WidgetPosition position)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(dashboardId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNull(config);

        var (isValid, error) = WidgetConfigValidator.Validate(type, config);
        if (!isValid)
            throw new BusinessRuleException(AnalyticsRuleCodes.Analytics_Dashboard_InvalidWidgetConfig, $"Invalid widget config: {error}");

        WidgetRules.ValidatePosition(position);

        return new DashboardWidget
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            DashboardId = dashboardId,
            Title = title,
            Type = type,
            Config = config,
            Position = position
        };
    }

    public bool UpdatePosition(WidgetPosition newPosition)
    {
        WidgetRules.ValidatePosition(newPosition);
        if (Position == newPosition) return false;
        Position = newPosition;
        return true;
    }

    public bool UpdateTitle(string title)
    {
        Guard.NotNullOrWhiteSpace(title);
        var normalizedName = title.Trim();
        if (Title == normalizedName) return false;
        Title = normalizedName;
        return true;
    }
}
