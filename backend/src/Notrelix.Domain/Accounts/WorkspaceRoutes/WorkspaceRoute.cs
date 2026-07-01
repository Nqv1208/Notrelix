namespace Notrelix.Domain.Accounts.WorkspaceRoutes;

public class WorkspaceRoute : AuditableEntity, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public string RouteSlug { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    private WorkspaceRoute() : base() { }

    public WorkspaceRoute(Guid accountId, string routeSlug, Guid? workspaceId = null, bool isDefault = false) : base()
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(routeSlug);

        AccountId = accountId;
        RouteSlug = routeSlug.Trim().ToLowerInvariant();
        WorkspaceId = workspaceId;
        IsDefault = isDefault;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }

    public void UnsetDefault()
    {
        IsDefault = false;
    }

    public void LinkWorkspace(Guid workspaceId)
    {
        Guard.NotEmpty(workspaceId);
        WorkspaceId = workspaceId;
    }
}
