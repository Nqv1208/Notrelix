using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardWidget : Entity
{
    public Guid DashboardId { get; private set; }
    public string Title { get; private set; } = null!;
    public WidgetType Type { get; private set; }
    public JsonValue Config { get; private set; } = null!;
    public WidgetPosition Position { get; private set; } = null!;

    private DashboardWidget() : base() { }

    public static DashboardWidget Create(Guid dashboardId, string title, WidgetType type, JsonValue config, WidgetPosition position)
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

public class Dashboard : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public DashboardVisibility Visibility { get; private set; }
    public DashboardStatus Status { get; private set; }

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

    private const int MaxWidgets = 50;

    private Dashboard() : base() { }

    public static Dashboard Create(Guid workspaceId, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);

        var dashboard = new Dashboard
        {
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Visibility = DashboardVisibility.Private,
            Status = DashboardStatus.Active
        };

        dashboard.SetAuditOnCreate(createdBy, createdAt);
        dashboard.AddDomainEvent(new DashboardCreatedDomainEvent(workspaceId, dashboard.Id, createdBy, createdAt));
        return dashboard;
    }

    public void Rename(string name, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        Name = normalizedName;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardRenamedDomainEvent(WorkspaceId, Id, Name, updatedBy, updatedAt));
    }

    public void ChangeVisibility(DashboardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardVisibilityChangedDomainEvent(WorkspaceId, Id, Visibility, updatedBy, updatedAt));
    }

    public void AddWidget(string title, WidgetType type, JsonValue config, WidgetPosition position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        WidgetRules.ValidatePosition(position);

        if (_widgets.Count >= MaxWidgets)
            throw new BusinessRuleException($"Cannot add more than {MaxWidgets} widgets to a dashboard.");

        var widget = DashboardWidget.Create(Id, title, type, config, position);
        _widgets.Add(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardWidgetAddedDomainEvent(WorkspaceId, Id, widget.Id, updatedBy, updatedAt));
    }

    public void RemoveWidget(Guid widgetId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        _widgets.Remove(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardWidgetRemovedDomainEvent(WorkspaceId, Id, widgetId, updatedBy, updatedAt));
    }

    public void MoveWidget(Guid widgetId, WidgetPosition newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null)
        {
            throw new DomainException($"Widget '{widgetId}' not found on this dashboard.");
        }

        widget.UpdatePosition(newPosition);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardWidgetMovedDomainEvent(WorkspaceId, Id, widgetId, newPosition, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = DashboardStatus.Archived;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardDeletedDomainEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = DashboardStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new DashboardRestoredDomainEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
