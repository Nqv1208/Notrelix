namespace Notrelix.Domain.Common;

public abstract record WorkspaceScopedDomainEvent : AccountScopedDomainEvent, IWorkspaceScoped
{
    public Guid WorkspaceId { get; }

    protected WorkspaceScopedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        DateTimeOffset occurredAt)
        : base(accountId, occurredAt)
    {
        if (workspaceId == Guid.Empty)
            throw new ArgumentException("Workspace id cannot be empty.", nameof(workspaceId));
        WorkspaceId = workspaceId;
    }
}
