using Notrelix.Domain.Analytics.Dashboards.Events;
using Notrelix.Domain.Analytics.Rules;
using Notrelix.Domain.Analytics.Widgets;
using static Notrelix.Domain.Analytics.AnalyticsRuleCodes;

namespace Notrelix.Domain.Analytics.Dashboards;

public class Dashboard : AggregateRoot, IWorkspaceScoped
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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        if (Name == normalizedName) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = normalizedName;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardRenamedDomainEvent(AccountId, WorkspaceId, Id, Name, updatedBy, updatedAt));
    }

    public void ChangeVisibility(DashboardVisibility visibility, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);
        if (Visibility == visibility) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Visibility = visibility;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardVisibilityChangedDomainEvent(AccountId, WorkspaceId, Id, Visibility, updatedBy, updatedAt));
    }

    public void AddWidget(string title, DashboardWidgetType type, JsonValue config, WidgetPosition position, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(title);
        WidgetRules.ValidatePosition(position);

        if (_widgets.Count >= MaxWidgets)
            throw new BusinessRuleException(Analytics_Dashboard_WidgetLimitExceeded, $"Cannot add more than {MaxWidgets} widgets to a dashboard.");

        var widget = DashboardWidget.Create(AccountId, WorkspaceId, Id, title, type, config, position);
        _widgets.Add(widget);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetAddedDomainEvent(AccountId, WorkspaceId, Id, widget.Id, updatedBy, updatedAt));
    }

    public void RemoveWidget(Guid widgetId, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null) return;

        _widgets.Remove(widget);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetRemovedDomainEvent(AccountId, WorkspaceId, Id, widgetId, updatedBy, updatedAt));
    }

    public void MoveWidget(Guid widgetId, WidgetPosition newPosition, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);
        var widget = _widgets.FirstOrDefault(w => w.Id == widgetId);
        if (widget is null)
        {
            throw new BusinessRuleException(Analytics_Dashboard_WidgetNotFound, $"Widget '{widgetId}' not found on this dashboard.");
        }

        if (widget.Position == newPosition) return;

        widget.UpdatePosition(newPosition);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardWidgetMovedDomainEvent(AccountId, WorkspaceId, Id, widgetId, newPosition, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        if (Status == DashboardStatus.Archived) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = DashboardStatus.Archived;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new DashboardArchivedDomainEvent(AccountId, WorkspaceId, Id, archivedBy, archivedAt));
    }
}
