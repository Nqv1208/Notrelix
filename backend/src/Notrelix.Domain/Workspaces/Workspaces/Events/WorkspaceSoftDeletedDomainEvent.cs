namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-soft-deleted")]
public sealed record WorkspaceSoftDeletedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid DeletedBy { get; }

    public WorkspaceSoftDeletedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid deletedBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        DeletedBy = deletedBy;
    }
}
