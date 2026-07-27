namespace Notrelix.Domain.Workspaces.Workspaces.Events;

[EventName("workspaces.workspace-restored")]
public sealed record WorkspaceRestoredDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid RestoredBy { get; }

    public WorkspaceRestoredDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid restoredBy,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        RestoredBy = restoredBy;
    }
}
