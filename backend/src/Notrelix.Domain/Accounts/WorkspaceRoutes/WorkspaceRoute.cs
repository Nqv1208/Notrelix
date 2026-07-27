using Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

namespace Notrelix.Domain.Accounts.WorkspaceRoutes;

public class WorkspaceRoute : SoftDeletableAggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public string RouteSlug { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    private WorkspaceRoute() : base() { }

    public static WorkspaceRoute Create(
        Guid accountId,
        string routeSlug,
        Guid createdBy,
        DateTimeOffset createdAt,
        Guid? workspaceId = null,
        bool isDefault = false)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(routeSlug);
        Guard.NotEmpty(createdBy);

        if (workspaceId == Guid.Empty)
            throw new BusinessRuleException(
                AccountRuleCodes.Accounts_WorkspaceRoute_InvalidWorkspaceId,
                "Workspace ID cannot be empty GUID.");

        var route = new WorkspaceRoute
        {
            AccountId = accountId,
            RouteSlug = routeSlug.Trim().ToLowerInvariant(),
            WorkspaceId = workspaceId,
            IsDefault = isDefault
        };

        route.SetAuditOnCreate(createdBy, createdAt);
        route.RaiseDomainEvent(new WorkspaceRouteCreatedDomainEvent(
            accountId, route.Id, route.RouteSlug, workspaceId, isDefault, createdBy, createdAt));

        return route;
    }

    public void SetAsDefault(Guid actorId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorId);

        if (IsDefault) return;

        IsDefault = true;
        SetAuditOnUpdate(actorId, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteSetAsDefaultDomainEvent(
            AccountId, Id, RouteSlug, actorId, occurredAt));
    }

    public void UnsetDefault(Guid actorId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorId);

        if (!IsDefault) return;

        IsDefault = false;
        SetAuditOnUpdate(actorId, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteUnsetAsDefaultDomainEvent(
            AccountId, Id, RouteSlug, actorId, occurredAt));
    }

    public void LinkWorkspace(Guid workspaceId, Guid actorId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(actorId);

        if (WorkspaceId == workspaceId) return;

        WorkspaceId = workspaceId;
        SetAuditOnUpdate(actorId, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteLinkedDomainEvent(
            AccountId, Id, RouteSlug, workspaceId, actorId, occurredAt));
    }

    public void UnlinkWorkspace(Guid actorId, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(actorId);

        if (WorkspaceId is null) return;

        WorkspaceId = null;
        SetAuditOnUpdate(actorId, occurredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteUnlinkedDomainEvent(
            AccountId, Id, RouteSlug, actorId, occurredAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteSoftDeletedDomainEvent(
            AccountId, Id, RouteSlug, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceRouteRestoredDomainEvent(
            AccountId, Id, RouteSlug, restoredBy, restoredAt));
    }
}