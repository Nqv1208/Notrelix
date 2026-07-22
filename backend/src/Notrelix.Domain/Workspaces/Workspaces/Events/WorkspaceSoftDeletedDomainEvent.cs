namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceSoftDeletedDomainEvent : WorkspaceRootDomainEvent
{
    public Guid DeletedBy { get; }

    public WorkspaceSoftDeletedDomainEvent(
        Guid workspaceId,
        Guid deletedBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        DeletedBy = deletedBy;
    }
}
