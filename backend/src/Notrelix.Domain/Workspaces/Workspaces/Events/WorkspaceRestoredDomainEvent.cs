namespace Notrelix.Domain.Workspaces.Workspaces.Events;

public sealed record WorkspaceRestoredDomainEvent : WorkspaceRootDomainEvent
{
    public Guid RestoredBy { get; }

    public WorkspaceRestoredDomainEvent(
        Guid workspaceId,
        Guid restoredBy,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt)
    {
        RestoredBy = restoredBy;
    }
}
