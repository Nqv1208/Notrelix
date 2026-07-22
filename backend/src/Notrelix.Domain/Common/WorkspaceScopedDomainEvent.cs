namespace Notrelix.Domain.Common;

public abstract record WorkspaceScopedDomainEvent : DomainEvent, IWorkspaceScoped
{
    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }

    protected WorkspaceScopedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
    }
}
