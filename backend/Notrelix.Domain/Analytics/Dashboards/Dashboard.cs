using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Analytics.Dashboards.Events;
using Notrelix.Domain.Analytics.Widgets;
using Notrelix.Domain.Analytics.Rules;

namespace Notrelix.Domain.Analytics.Dashboards;

public class DashboardWidget : Entity
{
    public Guid DashboardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public JsonValue Config { get; private set; } = null!;
    public WidgetPosition Position { get; private set; } = null!;

    private DashboardWidget() : base() { }

    public static DashboardWidget Create(Guid dashboardId, string title, string type, JsonValue config, WidgetPosition position)
    {
        Guard.NotEmpty(dashboardId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNullOrWhiteSpace(type);
        Guard.NotNull(config);
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
}

public class Dashboard : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public DashboardVisibility Visibility { get; private set; }
    public DashboardStatus Status { get; private set; }

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

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
        dashboard.AddDomainEvent(new DashboardCreatedEvent(workspaceId, dashboard.Id, createdBy, createdAt));
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
        AddDomainEvent(new DashboardRenamedEvent(WorkspaceId, Id, Name, updatedBy, updatedAt));
    }

    public void ChangeVisibility(DashboardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardVisibilityChangedEvent(WorkspaceId, Id, Visibility, updatedBy, updatedAt));
    }

    public void AddWidget(string title, string type, JsonValue config, WidgetPosition position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNullOrWhiteSpace(type);
        WidgetRules.ValidatePosition(position);

        var widget = DashboardWidget.Create(Id, title, type, config, position);
        _widgets.Add(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardWidgetAddedEvent(WorkspaceId, Id, widget.Id, updatedBy, updatedAt));
    }

    public void RemoveWidget(Guid widgetId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        _widgets.Remove(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardWidgetRemovedEvent(WorkspaceId, Id, widgetId, updatedBy, updatedAt));
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
        AddDomainEvent(new DashboardWidgetMovedEvent(WorkspaceId, Id, widgetId, newPosition, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = DashboardStatus.Archived;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new DashboardDeletedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = DashboardStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new DashboardRestoredEvent(WorkspaceId, Id, restoredBy, restoredAt));
    }
}
