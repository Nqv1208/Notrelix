using Notrelix.Domain.Analytics.Dashboards.Events;
using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;
using static Notrelix.Domain.Analytics.AnalyticsRuleCodes;

namespace Notrelix.Domain.Analytics.Dashboards;

public class Dashboard : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public DashboardVisibility Visibility { get; private set; }
    public DashboardStatus Status { get; private set; }

    private readonly List<DashboardWidget> _widgets = new();
    public IReadOnlyCollection<DashboardWidget> Widgets => _widgets.AsReadOnly();

    private const int MaxWidgets = 50;

    private Dashboard() : base() { }

    public static Dashboard Create(Guid accountId, Guid workspaceId, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotEmpty(accountId);

        var dashboard = new Dashboard
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Visibility = DashboardVisibility.Private,
            Status = DashboardStatus.Active
        };

        dashboard.SetAuditOnCreate(createdBy, createdAt);
        dashboard.RaiseDomainEvent(new DashboardCreatedDomainEvent(accountId, workspaceId, dashboard.Id, createdBy, createdAt));
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
        RaiseDomainEvent(new DashboardRenamedDomainEvent(AccountId, WorkspaceId, Id, Name, updatedBy, updatedAt));
    }

    public void ChangeVisibility(DashboardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Visibility == visibility) return;

        Visibility = visibility;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardVisibilityChangedDomainEvent(AccountId, WorkspaceId, Id, Visibility, updatedBy, updatedAt));
    }

    public void AddWidget(string title, DashboardWidgetType type, JsonValue config, WidgetPosition position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(title);
        WidgetRules.ValidatePosition(position);

        if (_widgets.Count >= MaxWidgets)
            throw new BusinessRuleException(Analytics_Dashboard_WidgetLimitExceeded, $"Cannot add more than {MaxWidgets} widgets to a dashboard.");

        var widget = DashboardWidget.Create(AccountId, WorkspaceId, Id, title, type, config, position);
        _widgets.Add(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetAddedDomainEvent(AccountId, WorkspaceId, Id, widget.Id, updatedBy, updatedAt));
    }

    public void RemoveWidget(Guid widgetId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        _widgets.Remove(widget);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetRemovedDomainEvent(AccountId, WorkspaceId, Id, widgetId, updatedBy, updatedAt));
    }

    public void MoveWidget(Guid widgetId, WidgetPosition newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null)
        {
            throw new BusinessRuleException(Analytics_Dashboard_WidgetNotFound, $"Widget '{widgetId}' not found on this dashboard.");
        }

        if (widget.Position == newPosition) return;

        widget.UpdatePosition(newPosition);
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetMovedDomainEvent(AccountId, WorkspaceId, Id, widgetId, newPosition, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = DashboardStatus.Archived;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = DashboardStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new DashboardRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
