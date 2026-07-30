namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-deleted")]
public sealed record WorkspaceDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid DeletedBy { get; }
    public WorkspaceStatus Status { get; }

    public WorkspaceDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid deletedBy,
        WorkspaceStatus status,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        DeletedBy = deletedBy;
        Status = status;
    }
}
