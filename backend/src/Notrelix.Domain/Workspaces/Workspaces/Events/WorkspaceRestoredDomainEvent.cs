namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-restored")]
public sealed record WorkspaceRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RestoredBy { get; }
    public WorkspaceStatus Status { get; }

    public WorkspaceRestoredDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid restoredBy,
        WorkspaceStatus status,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RestoredBy = restoredBy;
        Status = status;
    }
}
